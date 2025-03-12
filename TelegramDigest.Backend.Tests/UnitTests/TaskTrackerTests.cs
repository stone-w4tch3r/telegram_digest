using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using TelegramDigest.Backend.Core;

namespace TelegramDigest.Application.Tests.UnitTests;

[TestFixture]
[SuppressMessage("ReSharper", "MethodSupportsCancellation")]
public class TaskTrackerTests
{
    private TaskTracker<string> _taskTracker;

    [SetUp]
    public void SetUp()
    {
        _taskTracker = new();
    }

    [Test]
    public void AddTaskToWaitQueue_ShouldAddTask()
    {
        var task = new Func<CancellationToken, Task>(_ => Task.CompletedTask);
        var key = "task1";

        _taskTracker.AddTaskToWaitQueue(task, key);

        var waitingTasks = _taskTracker.GetWaitingTasks();
        Assert.That(waitingTasks, Does.Contain(key));
    }

    [Test]
    public void AddTaskToWaitQueue_ShouldThrowException_WhenTaskAlreadyInProgress()
    {
        var task = new Func<CancellationToken, Task>(_ => Task.CompletedTask);
        var key = "task1";

        _taskTracker.AddTaskToWaitQueue(task, key);
        _taskTracker.MoveTaskToInProgress(key);

        Assert.Throws<InvalidOperationException>(() => _taskTracker.AddTaskToWaitQueue(task, key));
    }

    [Test]
    public async Task DequeueWaitingTask_ShouldReturnTask()
    {
        var task = new Func<CancellationToken, Task>(_ => Task.CompletedTask);
        var key = "task1";

        _taskTracker.AddTaskToWaitQueue(task, key);

        var (dequeuedTask, dequeuedKey) = await _taskTracker.DequeueWaitingTask();

        Assert.That(task, Is.EqualTo(dequeuedTask));
        Assert.That(key, Is.EqualTo(dequeuedKey));
    }

    [Test]
    public void MoveTaskToInProgress_ShouldMoveTask()
    {
        var task = new Func<CancellationToken, Task>(_ => Task.CompletedTask);
        var key = "task1";

        _taskTracker.AddTaskToWaitQueue(task, key);
        _taskTracker.MoveTaskToInProgress(key);

        var inProgressTasks = _taskTracker.GetInProgressTasks();
        var waitingTasks = _taskTracker.GetWaitingTasks();
        Assert.That(waitingTasks, Does.Not.Contain(key));
        Assert.That(inProgressTasks, Does.Contain(key));
    }

    [Test]
    public void CompleteTaskInProgress_ShouldRemoveTask()
    {
        var task = new Func<CancellationToken, Task>(_ => Task.CompletedTask);
        var key = "task1";

        _taskTracker.AddTaskToWaitQueue(task, key);
        _taskTracker.MoveTaskToInProgress(key);
        _taskTracker.CompleteTaskInProgress(key);

        var inProgressTasks = _taskTracker.GetInProgressTasks();
        Assert.That(inProgressTasks, Does.Not.Contain(key));
    }

    [Test]
    public void CancelTaskInProgress_ShouldCancelTask()
    {
        var task = new Func<CancellationToken, Task>(ct => Task.Delay(1000, ct));
        var key = "task1";

        _taskTracker.AddTaskToWaitQueue(task, key);
        var cancellationToken = _taskTracker.MoveTaskToInProgress(key);

        _taskTracker.CancelTaskInProgress(key);

        Assert.That(cancellationToken.IsCancellationRequested);
    }

    [Test]
    public void RemoveWaitingTask_ShouldRemoveTask()
    {
        var task = new Func<CancellationToken, Task>(_ => Task.CompletedTask);
        var key = "task1";

        _taskTracker.AddTaskToWaitQueue(task, key);
        _taskTracker.RemoveWaitingTask(key);

        var waitingTasks = _taskTracker.GetWaitingTasks();
        Assert.That(waitingTasks, Does.Not.Contain(key));
    }

    [Test]
    public void RemoveWaitingTask_ShouldThrowException_WhenTaskNotFound()
    {
        Assert.Throws<KeyNotFoundException>(() => _taskTracker.RemoveWaitingTask("nonexistent"));
    }

    [Test]
    public void AddTaskToWaitQueue_ShouldThrow_WhenTaskAlreadyInWaitingQueue()
    {
        var key = "task1";
        var task = new Func<CancellationToken, Task>(_ => Task.CompletedTask);

        _taskTracker.AddTaskToWaitQueue(task, key);

        Assert.Throws<InvalidOperationException>(() => _taskTracker.AddTaskToWaitQueue(task, key));
    }

    [Test]
    public async Task ConcurrentAddToWaitQueue_ThrowsForDuplicateKey()
    {
        var key = "task1";
        var task = new Func<CancellationToken, Task>(_ => Task.CompletedTask);
        var exceptions = new ConcurrentQueue<Exception>();

        var tasks = Enumerable
            .Range(0, 4)
            .Select(_ =>
                Task.Run(() =>
                {
                    try
                    {
                        _taskTracker.AddTaskToWaitQueue(task, key);
                    }
                    catch (InvalidOperationException ex)
                    {
                        exceptions.Enqueue(ex);
                    }
                })
            );

        await Task.WhenAll(tasks);

        Assert.That(exceptions, Has.Count.EqualTo(3)); // Only 1 success, 3 failures
        Assert.That(_taskTracker.GetWaitingTasks(), Has.Exactly(1).Items);
    }

    [Test]
    public async Task ConcurrentDequeue_ProcessesAllTasks()
    {
        var numThreads = 4;
        var numTasks = numThreads * 25;
        var addedKeys = new List<string>();

        for (var i = 0; i < numTasks; i++)
        {
            var key = $"task{i}";
            addedKeys.Add(key);
            _taskTracker.AddTaskToWaitQueue(_ => Task.CompletedTask, key);
        }

        var processedKeys = new ConcurrentBag<string>();
        var dequeueTasks = Enumerable
            .Range(0, numThreads)
            .Select(_ =>
                Task.Run(async () =>
                {
                    foreach (var __ in Enumerable.Range(0, numTasks / numThreads))
                    {
                        var (_, key) = await _taskTracker.DequeueWaitingTask();
                        _taskTracker.MoveTaskToInProgress(key);
                        _taskTracker.CompleteTaskInProgress(key);
                        processedKeys.Add(key);
                    }
                })
            )
            .ToList();

        // Allow time to process all tasks
        await Task.WhenAll(dequeueTasks);
        await Task.Delay(100); // Small delay for final processing

        Assert.That(processedKeys, Is.EquivalentTo(addedKeys));
    }

    [Test]
    public void StressTest_MultipleOperationsConcurrently()
    {
        var numTasks = 1000;
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount * 2,
        };

        Parallel.For(
            0,
            numTasks,
            options,
            i =>
            {
                var key = $"task{i}";
                _taskTracker.AddTaskToWaitQueue(_ => Task.CompletedTask, key);
                _taskTracker.MoveTaskToInProgress(key);
                _taskTracker.CompleteTaskInProgress(key);
            }
        );

        Assert.That(_taskTracker.GetInProgressTasks(), Is.Empty);
        Assert.That(_taskTracker.GetWaitingTasks(), Is.Empty);
    }

    [Test]
    public async Task CancelTaskInProgress_ShouldTerminateLongRunningTask()
    {
        var key = "task1";
        var taskCompleted = false;
        var cancellationConfirmed = false;

        var task = new Func<CancellationToken, Task>(async ct =>
        {
            try
            {
                await Task.Delay(Timeout.Infinite, ct);
            }
            catch (OperationCanceledException)
            {
                cancellationConfirmed = true;
                throw;
            }
            finally
            {
                taskCompleted = true;
            }
        });

        _taskTracker.AddTaskToWaitQueue(task, key);
        var (taskToRun, _) = await _taskTracker.DequeueWaitingTask();
        var ct = _taskTracker.MoveTaskToInProgress(key);
        _ = taskToRun(ct);
        _taskTracker.CancelTaskInProgress(key);

        await Task.Delay(200); // Allow cancellation to propagate

        Assert.Multiple(() =>
        {
            Assert.That(taskCompleted, Is.True);
            Assert.That(cancellationConfirmed, Is.True);
        });
    }

    [Test]
    public void CompleteTask_ShouldThrowForNonExistentKey()
    {
        Assert.Throws<KeyNotFoundException>(
            () => _taskTracker.CompleteTaskInProgress("nonexistent")
        );
    }

    [Test]
    public void ConcurrentMoveToInProgress_ThrowsForDuplicateKey()
    {
        var key = "task1";
        _taskTracker.AddTaskToWaitQueue(_ => Task.CompletedTask, key);

        var exceptions = new ConcurrentQueue<Exception>();

        var tasks = Enumerable
            .Range(0, 4)
            .Select(_ =>
                Task.Run(() =>
                {
                    try
                    {
                        _taskTracker.MoveTaskToInProgress(key);
                    }
                    catch (InvalidOperationException ex)
                    {
                        exceptions.Enqueue(ex);
                    }
                })
            );

        Task.WaitAll(tasks.ToArray());

        Assert.That(exceptions, Has.Count.EqualTo(3)); // Only 1 success
        Assert.That(_taskTracker.GetInProgressTasks(), Has.Exactly(1).Items);
    }

    [Test]
    public void AddTaskToWaitQueue_ShouldAllowReuseOfKey_AfterCompletion()
    {
        var task = new Func<CancellationToken, Task>(_ => Task.CompletedTask);
        var key = "task1";

        // Add, move to in-progress, complete.
        _taskTracker.AddTaskToWaitQueue(task, key);
        _taskTracker.MoveTaskToInProgress(key);
        _taskTracker.CompleteTaskInProgress(key);

        // Now the same key can be reused.
        Assert.DoesNotThrow(() => _taskTracker.AddTaskToWaitQueue(task, key));
        var waitingTasks = _taskTracker.GetWaitingTasks();
        Assert.That(waitingTasks, Contains.Item(key));
    }

    [Test]
    public void MoveTaskToInProgress_ShouldThrow_WhenTaskNotInWaitingQueue()
    {
        var key = "nonexistent";
        var ex = Assert.Throws<InvalidOperationException>(
            () => _taskTracker.MoveTaskToInProgress(key)
        );
        Assert.That(ex.Message, Does.Contain("is not in waiting queue"));
    }

    [Test]
    public void CancelTaskInProgress_ShouldThrow_WhenTaskNotFound()
    {
        var key = "nonexistent";
        var ex = Assert.Throws<KeyNotFoundException>(() => _taskTracker.CancelTaskInProgress(key));
        Assert.That(ex.Message, Does.Contain("was not found in progress tasks list"));
    }

    [Test]
    public void CancelTaskInProgress_ShouldNotThrow_WhenAlreadyCancelled()
    {
        var key = "task1";
        var task = new Func<CancellationToken, Task>(ct => Task.Delay(1000, ct));

        _taskTracker.AddTaskToWaitQueue(task, key);
        var token = _taskTracker.MoveTaskToInProgress(key);

        // First cancellation should cancel the token.
        Assert.DoesNotThrow(() => _taskTracker.CancelTaskInProgress(key));
        Assert.That(token.IsCancellationRequested, Is.True);

        // Calling cancellation again should not throw.
        Assert.DoesNotThrow(() => _taskTracker.CancelTaskInProgress(key));
    }

    [Test]
    public async Task DequeueWaitingTask_ShouldFollowFIFOOrder()
    {
        var keys = new[] { "first", "second", "third" };
        foreach (var key in keys)
        {
            _taskTracker.AddTaskToWaitQueue(_ => Task.CompletedTask, key);
        }

        var dequeuedKeys = new List<string>();
        for (var i = 0; i < keys.Length; i++)
        {
            var (_, key) = await _taskTracker.DequeueWaitingTask();
            dequeuedKeys.Add(key);
        }

        Assert.That(dequeuedKeys, Is.EqualTo(keys).AsCollection);
    }

    [Test]
    public void CompleteTaskInProgress_ShouldThrow_WhenCalledTwice()
    {
        var key = "task1";
        var task = new Func<CancellationToken, Task>(_ => Task.CompletedTask);

        _taskTracker.AddTaskToWaitQueue(task, key);
        _taskTracker.MoveTaskToInProgress(key);
        _taskTracker.CompleteTaskInProgress(key);

        var ex = Assert.Throws<KeyNotFoundException>(
            () => _taskTracker.CompleteTaskInProgress(key)
        );
        Assert.That(ex.Message, Does.Contain("is not in progress"));
    }
}
