using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TelegramDigest.Backend.Core;

namespace TelegramDigest.Application.Tests.UnitTests;

[TestFixture]
[SuppressMessage("ReSharper", "MethodSupportsCancellation")]
public class TaskManagerTests
{
    private TaskManager<string> _taskManager;

    [SetUp]
    public void SetUp()
    {
        _taskManager = new();
    }

    [Test]
    public void AddTaskToWaitQueue_ShouldAddTask()
    {
        var task = new Func<CancellationToken, IServiceScope, Task>((_, _) => Task.CompletedTask);
        var key = "task1";

        _taskManager.AddTaskToWaitQueue(task, key);

        var waitingTasks = _taskManager.GetWaitingTasks();
        Assert.That(waitingTasks, Does.Contain(key));
    }

    [Test]
    public void AddTaskToWaitQueue_ShouldThrowException_WhenTaskAlreadyInProgress()
    {
        var task = new Func<CancellationToken, IServiceScope, Task>((_, _) => Task.CompletedTask);
        var key = "task1";

        _taskManager.AddTaskToWaitQueue(task, key);
        _taskManager.MoveTaskToInProgress(key);

        Assert.Throws<InvalidOperationException>(() => _taskManager.AddTaskToWaitQueue(task, key));
    }

    [Test]
    public async Task DequeueWaitingTask_ShouldReturnTask()
    {
        var task = new Func<CancellationToken, IServiceScope, Task>((_, _) => Task.CompletedTask);
        var key = "task1";

        _taskManager.AddTaskToWaitQueue(task, key);

        var (dequeuedTask, _, dequeuedKey) = await _taskManager.DequeueWaitingTask();

        Assert.That(task, Is.EqualTo(dequeuedTask));
        Assert.That(key, Is.EqualTo(dequeuedKey));
    }

    [Test]
    public void MoveTaskToInProgress_ShouldMoveTask()
    {
        var task = new Func<CancellationToken, IServiceScope, Task>((_, _) => Task.CompletedTask);
        var key = "task1";

        _taskManager.AddTaskToWaitQueue(task, key);
        _taskManager.MoveTaskToInProgress(key);

        var inProgressTasks = _taskManager.GetInProgressTasks();
        var waitingTasks = _taskManager.GetWaitingTasks();
        Assert.That(waitingTasks, Does.Not.Contain(key));
        Assert.That(inProgressTasks, Does.Contain(key));
    }

    [Test]
    public void CompleteTaskInProgress_ShouldRemoveTask()
    {
        var task = new Func<CancellationToken, IServiceScope, Task>((_, _) => Task.CompletedTask);
        var key = "task1";

        _taskManager.AddTaskToWaitQueue(task, key);
        _taskManager.MoveTaskToInProgress(key);
        _taskManager.CompleteTaskInProgress(key);

        var inProgressTasks = _taskManager.GetInProgressTasks();
        Assert.That(inProgressTasks, Does.Not.Contain(key));
    }

    [Test]
    public void CancelTaskInProgress_ShouldCancelTask()
    {
        var task = new Func<CancellationToken, IServiceScope, Task>(
            (ct, _) => Task.Delay(1000, ct)
        );
        var key = "task1";

        _taskManager.AddTaskToWaitQueue(task, key);
        var cancellationToken = _taskManager.MoveTaskToInProgress(key);

        _taskManager.CancelTaskInProgress(key);

        Assert.That(cancellationToken.IsCancellationRequested);
    }

    [Test]
    public void RemoveWaitingTask_ShouldRemoveTask()
    {
        var task = new Func<CancellationToken, IServiceScope, Task>((_, _) => Task.CompletedTask);
        var key = "task1";

        _taskManager.AddTaskToWaitQueue(task, key);
        _taskManager.RemoveWaitingTask(key);

        var waitingTasks = _taskManager.GetWaitingTasks();
        Assert.That(waitingTasks, Does.Not.Contain(key));
    }

    [Test]
    public void RemoveWaitingTask_ShouldThrowException_WhenTaskNotFound()
    {
        Assert.Throws<KeyNotFoundException>(() => _taskManager.RemoveWaitingTask("nonexistent"));
    }

    [Test]
    public void AddTaskToWaitQueue_ShouldThrow_WhenTaskAlreadyInWaitingQueue()
    {
        var key = "task1";
        var task = new Func<CancellationToken, IServiceScope, Task>((_, _) => Task.CompletedTask);

        _taskManager.AddTaskToWaitQueue(task, key);

        Assert.Throws<InvalidOperationException>(() => _taskManager.AddTaskToWaitQueue(task, key));
    }

    [Test]
    public async Task ConcurrentAddToWaitQueue_ThrowsForDuplicateKey()
    {
        var key = "task1";
        var task = new Func<CancellationToken, IServiceScope, Task>((_, _) => Task.CompletedTask);
        var exceptions = new ConcurrentQueue<Exception>();

        var tasks = Enumerable
            .Range(0, 4)
            .Select(
                (_, _) =>
                    Task.Run(() =>
                    {
                        try
                        {
                            _taskManager.AddTaskToWaitQueue(task, key);
                        }
                        catch (InvalidOperationException ex)
                        {
                            exceptions.Enqueue(ex);
                        }
                    })
            );

        await Task.WhenAll(tasks);

        Assert.That(exceptions, Has.Count.EqualTo(3)); // Only 1 success, 3 failures
        Assert.That(_taskManager.GetWaitingTasks(), Has.Exactly(1).Items);
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
            _taskManager.AddTaskToWaitQueue((_, _) => Task.CompletedTask, key);
        }

        var processedKeys = new ConcurrentBag<string>();
        var dequeueTasks = Enumerable
            .Range(0, numThreads)
            .Select(
                (_, _) =>
                    Task.Run(async () =>
                    {
                        foreach (var __ in Enumerable.Range(0, numTasks / numThreads))
                        {
                            var (_, _, key) = await _taskManager.DequeueWaitingTask();
                            _taskManager.MoveTaskToInProgress(key);
                            _taskManager.CompleteTaskInProgress(key);
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
                _taskManager.AddTaskToWaitQueue((_, _) => Task.CompletedTask, key);
                _taskManager.MoveTaskToInProgress(key);
                _taskManager.CompleteTaskInProgress(key);
            }
        );

        Assert.That(_taskManager.GetInProgressTasks(), Is.Empty);
        Assert.That(_taskManager.GetWaitingTasks(), Is.Empty);
    }

    [Test]
    public async Task CancelTaskInProgress_ShouldTerminateLongRunningTask()
    {
        var key = "task1";
        var taskCompleted = false;
        var cancellationConfirmed = false;

        var task = new Func<CancellationToken, IServiceScope, Task>(
            async (ct, _) =>
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
            }
        );

        _taskManager.AddTaskToWaitQueue(task, key);
        var (taskToRun, _, _) = await _taskManager.DequeueWaitingTask();
        var ct = _taskManager.MoveTaskToInProgress(key);
        _ = taskToRun(ct, Mock.Of<IServiceScope>());
        _taskManager.CancelTaskInProgress(key);

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
            () => _taskManager.CompleteTaskInProgress("nonexistent")
        );
    }

    [Test]
    public void ConcurrentMoveToInProgress_ThrowsForDuplicateKey()
    {
        var key = "task1";
        _taskManager.AddTaskToWaitQueue((_, _) => Task.CompletedTask, key);

        var exceptions = new ConcurrentQueue<Exception>();

        var tasks = Enumerable
            .Range(0, 4)
            .Select(
                (_, _) =>
                    Task.Run(() =>
                    {
                        try
                        {
                            _taskManager.MoveTaskToInProgress(key);
                        }
                        catch (InvalidOperationException ex)
                        {
                            exceptions.Enqueue(ex);
                        }
                    })
            );

        Task.WaitAll(tasks.ToArray());

        Assert.That(exceptions, Has.Count.EqualTo(3)); // Only 1 success
        Assert.That(_taskManager.GetInProgressTasks(), Has.Exactly(1).Items);
    }

    [Test]
    public void AddTaskToWaitQueue_ShouldAllowReuseOfKey_AfterCompletion()
    {
        var task = new Func<CancellationToken, IServiceScope, Task>((_, _) => Task.CompletedTask);
        var key = "task1";

        // Add, move to in-progress, complete.
        _taskManager.AddTaskToWaitQueue(task, key);
        _taskManager.MoveTaskToInProgress(key);
        _taskManager.CompleteTaskInProgress(key);

        // Now the same key can be reused.
        Assert.DoesNotThrow(() => _taskManager.AddTaskToWaitQueue(task, key));
        var waitingTasks = _taskManager.GetWaitingTasks();
        Assert.That(waitingTasks, Contains.Item(key));
    }

    [Test]
    public void MoveTaskToInProgress_ShouldThrow_WhenTaskNotInWaitingQueue()
    {
        var key = "nonexistent";
        var ex = Assert.Throws<InvalidOperationException>(
            () => _taskManager.MoveTaskToInProgress(key)
        );
        Assert.That(ex.Message, Does.Contain("is not in waiting queue"));
    }

    [Test]
    public void CancelTaskInProgress_ShouldThrow_WhenTaskNotFound()
    {
        var key = "nonexistent";
        var ex = Assert.Throws<KeyNotFoundException>(() => _taskManager.CancelTaskInProgress(key));
        Assert.That(ex.Message, Does.Contain("was not found in progress tasks list"));
    }

    [Test]
    public void CancelTaskInProgress_ShouldNotThrow_WhenAlreadyCancelled()
    {
        var key = "task1";
        var task = new Func<CancellationToken, IServiceScope, Task>(
            (ct, _) => Task.Delay(1000, ct)
        );

        _taskManager.AddTaskToWaitQueue(task, key);
        var token = _taskManager.MoveTaskToInProgress(key);

        // First cancellation should cancel the token.
        Assert.DoesNotThrow(() => _taskManager.CancelTaskInProgress(key));
        Assert.That(token.IsCancellationRequested, Is.True);

        // Calling cancellation again should not throw.
        Assert.DoesNotThrow(() => _taskManager.CancelTaskInProgress(key));
    }

    [Test]
    public async Task DequeueWaitingTask_ShouldFollowFIFOOrder()
    {
        var keys = new[] { "first", "second", "third" };
        foreach (var key in keys)
        {
            _taskManager.AddTaskToWaitQueue((_, _) => Task.CompletedTask, key);
        }

        var dequeuedKeys = new List<string>();
        for (var i = 0; i < keys.Length; i++)
        {
            var (_, _, key) = await _taskManager.DequeueWaitingTask();
            dequeuedKeys.Add(key);
        }

        Assert.That(dequeuedKeys, Is.EqualTo(keys).AsCollection);
    }

    [Test]
    public void CompleteTaskInProgress_ShouldThrow_WhenCalledTwice()
    {
        var key = "task1";
        var task = new Func<CancellationToken, IServiceScope, Task>((_, _) => Task.CompletedTask);

        _taskManager.AddTaskToWaitQueue(task, key);
        _taskManager.MoveTaskToInProgress(key);
        _taskManager.CompleteTaskInProgress(key);

        var ex = Assert.Throws<KeyNotFoundException>(
            () => _taskManager.CompleteTaskInProgress(key)
        );
        Assert.That(ex.Message, Does.Contain("is not in progress"));
    }

    [Test]
    public void GetCancellationRequestedTasks_ShouldReturnOnlyCancelledTasks()
    {
        // Arrange
        var runningKey = "running";
        var cancelledKey = "cancelled";
        var longRunningTask = new Func<CancellationToken, IServiceScope, Task>(
            (ct, _) => Task.Delay(TimeSpan.FromSeconds(10), ct)
        );

        _taskManager.AddTaskToWaitQueue(longRunningTask, runningKey);
        _taskManager.AddTaskToWaitQueue(longRunningTask, cancelledKey);

        _taskManager.MoveTaskToInProgress(runningKey);
        _taskManager.MoveTaskToInProgress(cancelledKey);

        // Act
        _taskManager.CancelTaskInProgress(cancelledKey);
        var cancelledTasks = _taskManager.GetCancellationRequestedTasks();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(cancelledTasks, Has.Length.EqualTo(1));
            Assert.That(cancelledTasks, Does.Contain(cancelledKey));
            Assert.That(cancelledTasks, Does.Not.Contain(runningKey));
        });
    }

    [Test]
    public void GetCancellationRequestedTasks_ShouldReturnEmptyArray_WhenNoTasksCancelled()
    {
        // Arrange
        var key1 = "task1";
        var key2 = "task2";
        var task = new Func<CancellationToken, IServiceScope, Task>((_, _) => Task.CompletedTask);

        _taskManager.AddTaskToWaitQueue(task, key1);
        _taskManager.AddTaskToWaitQueue(task, key2);

        _taskManager.MoveTaskToInProgress(key1);
        _taskManager.MoveTaskToInProgress(key2);

        // Act
        var cancelledTasks = _taskManager.GetCancellationRequestedTasks();

        // Assert
        Assert.That(cancelledTasks, Is.Empty);
    }

    [Test]
    public async Task CancelTask_ShouldExecuteCleanupCode()
    {
        // Arrange
        var key = "task1";
        var cleanupExecuted = false;
        var taskStarted = false;

        var task = new Func<CancellationToken, IServiceScope, Task>(
            async (ct, _) =>
            {
                try
                {
                    taskStarted = true;
                    await Task.Delay(Timeout.Infinite, ct);
                }
                finally
                {
                    cleanupExecuted = true;
                }
            }
        );

        // Act
        _taskManager.AddTaskToWaitQueue(task, key);
        var (dequeued, _, _) = await _taskManager.DequeueWaitingTask();
        var ct = _taskManager.MoveTaskToInProgress(key);

        // Start task without awaiting
        _ = dequeued(ct, Mock.Of<IServiceScope>());

        // Wait for a task to start
        await Task.Delay(100);
        Assert.That(taskStarted, Is.True, "Task should have started");

        // Cancel and wait for completion
        _taskManager.CancelTaskInProgress(key);
        await Task.Delay(100);

        // Assert
        Assert.That(cleanupExecuted, Is.True, "Cleanup code should have executed");
    }

    [Test]
    public async Task CancellationToken_ShouldPropagateToNestedOperations()
    {
        // Arrange
        var key = "task1";
        var innerTaskCancelled = false;
        var outerTaskCancelled = false;

        var task = new Func<CancellationToken, IServiceScope, Task>(
            async (outerCt, _) =>
            {
                try
                {
                    await Task.Run(
                        async () =>
                        {
                            try
                            {
                                await Task.Delay(Timeout.Infinite, outerCt);
                            }
                            catch (OperationCanceledException)
                            {
                                innerTaskCancelled = true;
                                throw;
                            }
                        },
                        outerCt
                    );
                }
                catch (OperationCanceledException)
                {
                    outerTaskCancelled = true;
                    throw;
                }
            }
        );

        // Act
        _taskManager.AddTaskToWaitQueue(task, key);
        var (dequeued, _, _) = await _taskManager.DequeueWaitingTask();
        var ct = _taskManager.MoveTaskToInProgress(key);

        _ = dequeued(ct, Mock.Of<IServiceScope>());
        await Task.Delay(100); // Give time for task to start

        _taskManager.CancelTaskInProgress(key);
        await Task.Delay(100); // Wait for cancellation to propagate

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(innerTaskCancelled, Is.True, "Inner task should be cancelled");
            Assert.That(outerTaskCancelled, Is.True, "Outer task should be cancelled");
        });
    }

    [Test]
    public async Task CancelledTask_ShouldAllowKeyReuse()
    {
        // Arrange
        var key = "task1";
        var task = new Func<CancellationToken, IServiceScope, Task>(
            async (ct, _) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
            }
        );

        // Act - First use and cancellation
        _taskManager.AddTaskToWaitQueue(task, key);
        var (dequeued, _, _) = await _taskManager.DequeueWaitingTask();
        var ct = _taskManager.MoveTaskToInProgress(key);

        var runningTask = dequeued(ct, Mock.Of<IServiceScope>());
        _taskManager.CancelTaskInProgress(key);

        try
        {
            await runningTask;
        }
        catch (OperationCanceledException) { }

        _taskManager.CompleteTaskInProgress(key);

        // Act - Reuse key
        var newTask = new Func<CancellationToken, IServiceScope, Task>(
            (_, _) => Task.CompletedTask
        );

        // Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            _taskManager.AddTaskToWaitQueue(newTask, key);
            await _taskManager.DequeueWaitingTask();
            _taskManager.MoveTaskToInProgress(key);
        });

        Assert.That(_taskManager.GetInProgressTasks(), Does.Contain(key));
    }

    [Test]
    public async Task CancelledTask_ShouldReceiveCancellation()
    {
        // Arrange
        var key = "task1";
        var cancellationReceived = false;

        var task = new Func<CancellationToken, IServiceScope, Task>(
            async (ct, _) =>
            {
                try
                {
                    await Task.Delay(Timeout.Infinite, ct);
                }
                catch (OperationCanceledException)
                {
                    cancellationReceived = true;
                    throw;
                }
            }
        );

        // Act
        _taskManager.AddTaskToWaitQueue(task, key);
        var (dequeued, _, _) = await _taskManager.DequeueWaitingTask();
        var ct = _taskManager.MoveTaskToInProgress(key);

        var runningTask = dequeued(ct, Mock.Of<IServiceScope>());
        await Task.Delay(100); // Give task time to start

        _taskManager.CancelTaskInProgress(key);

        try
        {
            await runningTask;
        }
        catch (OperationCanceledException) { }

        // Assert
        Assert.That(cancellationReceived, Is.True, "Task should have received cancellation");
    }

    [Test]
    public async Task CancelAllTasksInProgress_ShouldCancelAllRunningTasks()
    {
        // Arrange
        var taskCount = 3;
        var cancelledTasks = 0;

        var task = new Func<CancellationToken, IServiceScope, Task>(
            async (ct, _) =>
            {
                try
                {
                    await Task.Delay(Timeout.Infinite, ct);
                }
                catch (OperationCanceledException)
                {
                    Interlocked.Increment(ref cancelledTasks);
                    throw;
                }
            }
        );

        // Add multiple tasks
        for (var i = 0; i < taskCount; i++)
        {
            var key = $"task{i}";
            _taskManager.AddTaskToWaitQueue(task, key);
            var (dequeued, _, _) = await _taskManager.DequeueWaitingTask();
            var ct = _taskManager.MoveTaskToInProgress(key);
            _ = dequeued(ct, Mock.Of<IServiceScope>());
        }

        await Task.Delay(100); // Allow tasks to start

        // Act
        _taskManager.CancelAllTasksInProgress();
        await Task.Delay(100); // Allow cancellation to propagate

        // Assert
        Assert.That(cancelledTasks, Is.EqualTo(taskCount), "All tasks should have been cancelled");
        Assert.That(_taskManager.GetCancellationRequestedTasks(), Has.Length.EqualTo(taskCount));
    }

    [Test]
    public async Task CancelledTask_ShouldBeRemovedFromInProgressList()
    {
        // Arrange
        var key = "task1";
        var task = new Func<CancellationToken, IServiceScope, Task>(
            async (ct, _) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
            }
        );

        // Act
        _taskManager.AddTaskToWaitQueue(task, key);
        var (dequeued, _, _) = await _taskManager.DequeueWaitingTask();
        var ct = _taskManager.MoveTaskToInProgress(key);

        var runningTask = dequeued(ct, Mock.Of<IServiceScope>());
        await Task.Delay(100); // Give task time to start

        _taskManager.CancelTaskInProgress(key);

        try
        {
            await runningTask;
        }
        catch (OperationCanceledException) { }

        _taskManager.CompleteTaskInProgress(key);

        // Assert
        Assert.That(_taskManager.GetInProgressTasks(), Does.Not.Contain(key));
        Assert.That(_taskManager.GetCancellationRequestedTasks(), Is.Empty);
    }
}
