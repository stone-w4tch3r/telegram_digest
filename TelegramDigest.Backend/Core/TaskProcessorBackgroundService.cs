using System.Collections.Concurrent;
using System.Threading.Channels;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TelegramDigest.Backend.DeploymentOptions;

namespace TelegramDigest.Backend.Core;

internal interface ITaskScheduler<TKey>
    where TKey : IEquatable<TKey>
{
    /// <exception cref="InvalidOperationException">
    /// Thrown if the task is already in progress or in the waiting queue.
    /// </exception>
    public void AddTaskToWaitQueue(Func<CancellationToken, Task> task, TKey key);

    /// <exception cref="KeyNotFoundException">
    /// Thrown if the key is not found in the waiting tasks list.
    /// </exception>
    public void RemoveWaitingTask(TKey key);

    /// <returns>A read-only collection of keys.</returns>
    public TKey[] GetWaitingTasks();

    /// <returns>A read-only collection of keys.</returns>
    public TKey[] GetInProgressTasks();

    /// <param name="key">The key associated with the task to be canceled.</param>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if the key is not found in the in-progress tasks list.
    /// </exception>
    public void CancelTaskInProgress(TKey key);
}

internal interface ITaskProgressHandler<TKey>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Waits for a task to be available in the queue and returns it.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, containing the task and its key.</returns>
    public Task<(Func<CancellationToken, Task> task, TKey key)> DequeueWaitingTask();

    /// <summary>
    /// Moves a task from the waiting queue to in-progress state.
    /// </summary>
    /// <returns>A cancellation token that can be used to cancel the task in progress.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the task is not in the waiting queue or is already in progress.
    /// </exception>
    public CancellationToken MoveTaskToInProgress(TKey key);

    /// <summary>
    /// Marks a task as complete and removes it from the in-progress state.
    /// </summary>
    /// <param name="key">The key associated with the task to be completed.</param>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if the key is not found in the in-progress tasks list.
    /// </exception>
    public void CompleteTaskInProgress(TKey key);

    /// <summary>
    /// Tries to mark a task as complete and removes it from the in-progress state.
    /// </summary>
    /// <param name="key">The key associated with the task to be completed.</param>
    /// <returns>True if the task was successfully completed, false otherwise</returns>
    public bool TryCompleteTaskInProgress(TKey key);
}

internal sealed class TaskTracker<TKey> : ITaskScheduler<TKey>, ITaskProgressHandler<TKey>
    where TKey : IEquatable<TKey>
{
    private readonly Channel<(
        Func<CancellationToken, Task> taskFactory,
        TKey key
    )> _waitingTasksQueue = Channel.CreateUnbounded<(
        Func<CancellationToken, Task> taskFactory,
        TKey key
    )>();
    private readonly ConcurrentDictionary<TKey, Func<CancellationToken, Task>> _waitingTasksList =
        new();
    private readonly ConcurrentDictionary<TKey, CancellationTokenSource> _inProgressTasksCts =
        new();

    public void AddTaskToWaitQueue(Func<CancellationToken, Task> task, TKey key)
    {
        if (_inProgressTasksCts.ContainsKey(key))
        {
            throw new InvalidOperationException($"Task {key} is already in progress");
        }

        if (!_waitingTasksList.TryAdd(key, task))
        {
            throw new InvalidOperationException($"Task {key} is already in waiting queue");
        }

        if (!_waitingTasksQueue.Writer.TryWrite((task, key)))
        {
            _waitingTasksList.TryRemove(key, out _);
            throw new InvalidOperationException("Failed to write to the background task queue.");
        }
    }

    public async Task<(Func<CancellationToken, Task> task, TKey key)> DequeueWaitingTask()
    {
        var item = await _waitingTasksQueue.Reader.ReadAsync();
        return (item.taskFactory, item.key);
    }

    public CancellationToken MoveTaskToInProgress(TKey key)
    {
        if (!_waitingTasksList.TryRemove(key, out _))
        {
            throw new InvalidOperationException($"Task {key} is not in waiting queue");
        }

        var cts = new CancellationTokenSource();
        if (!_inProgressTasksCts.TryAdd(key, cts))
        {
            cts.Dispose();
            throw new InvalidOperationException($"Task {key} is already in progress");
        }

        return cts.Token;
    }

    public TKey[] GetInProgressTasks()
    {
        return _inProgressTasksCts.Keys.ToArray();
    }

    public void CompleteTaskInProgress(TKey key)
    {
        if (!_inProgressTasksCts.TryRemove(key, out var cts))
        {
            throw new KeyNotFoundException($"Task {key} is not in progress");
        }

        cts.Dispose();
    }

    public bool TryCompleteTaskInProgress(TKey key)
    {
        try
        {
            CompleteTaskInProgress(key);
            return true;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    }

    public TKey[] GetWaitingTasks()
    {
        return _waitingTasksList.Keys.ToArray();
    }

    public void RemoveWaitingTask(TKey key)
    {
        if (!_waitingTasksList.TryRemove(key, out _))
        {
            throw new KeyNotFoundException($"The key {key} was not found in waiting tasks list.");
        }
    }

    public void CancelTaskInProgress(TKey key)
    {
        if (!_inProgressTasksCts.TryGetValue(key, out var cts))
        {
            throw new KeyNotFoundException($"The key {key} was not found in progress tasks list");
        }

        if (!cts.IsCancellationRequested)
        {
            cts.Cancel();
        }
    }
}

[UsedImplicitly]
internal sealed class TaskProcessorBackgroundService(
    ITaskProgressHandler<DigestId> taskTracker,
    ILogger<TaskProcessorBackgroundService> logger,
    IOptions<BackendDeploymentOptions> deploymentOptions
) : BackgroundService
{
    private readonly SemaphoreSlim _semaphore = new(deploymentOptions.Value.MaxConcurrentAiTasks);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProcessSingleTask(ct);
            }
            catch (Exception ex)
            {
                logger.LogCritical(
                    ex,
                    $"Unhandled exception in {nameof(TaskProcessorBackgroundService)}, but crash prevented"
                );
            }
        }
    }

    private async Task ProcessSingleTask(CancellationToken lifecycleCt)
    {
        // Wait for a new task to be available and acquire a semaphore slot
        await _semaphore.WaitAsync(lifecycleCt);
        var (workItem, digestId) = await taskTracker.DequeueWaitingTask();

        // Run the task in the background without awaiting, allowing concurrency
        _ = Task.Run(
            async () =>
            {
                try
                {
                    // Move a task to in-progress and execute it
                    var progressControlCt = taskTracker.MoveTaskToInProgress(digestId);
                    await workItem(
                        CancellationTokenSource
                            .CreateLinkedTokenSource(progressControlCt, lifecycleCt)
                            .Token
                    );
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Error occurred during digest queue processing for DigestId: {DigestId}",
                        digestId
                    );
                }
                finally
                {
                    taskTracker.TryCompleteTaskInProgress(digestId);
                    _semaphore.Release();
                }
            },
            lifecycleCt
        );
    }

    public override void Dispose()
    {
        base.Dispose();

        _semaphore.Dispose();
    }
}
