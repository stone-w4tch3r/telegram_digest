using System.Threading.Channels;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;

namespace TelegramDigest.Backend.Core;

internal interface ITaskQueue
{
    public void QueueTask(Func<CancellationToken, Task> workItem);
    public Task<Func<CancellationToken, Task>> WaitForDequeue(CancellationToken ct);
}

internal sealed class TaskQueue : ITaskQueue
{
    private readonly Channel<Func<CancellationToken, Task>> _queue = Channel.CreateUnbounded<
        Func<CancellationToken, Task>
    >();

    /// <summary>
    /// Queues a work item to be processed later.
    /// </summary>
    public void QueueTask(Func<CancellationToken, Task> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);

        var result = _queue.Writer.TryWrite(workItem);
        if (!result)
        {
            throw new InvalidOperationException("Failed to write to the background task queue.");
        }
    }

    /// <summary>
    /// Waits asynchronously for a work item to be present in the queue, and then returns it.
    /// </summary>
    public async Task<Func<CancellationToken, Task>> WaitForDequeue(CancellationToken ct)
    {
        var workItem = await _queue.Reader.ReadAsync(ct);
        return workItem;
    }
}

[UsedImplicitly]
internal sealed class QueueProcessorBackgroundService(
    ITaskQueue taskQueue,
    IServiceProvider serviceProvider,
    ILogger<QueueProcessorBackgroundService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // wait for a new task
            var workItem = await taskQueue.WaitForDequeue(ct);
            try
            {
                // execute task
                await workItem(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during digest queue processing");
            }
        }
    }
}
