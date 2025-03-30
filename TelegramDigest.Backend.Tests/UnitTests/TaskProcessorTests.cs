using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TelegramDigest.Backend.Core;
using TelegramDigest.Backend.DeploymentOptions;

namespace TelegramDigest.Application.Tests.UnitTests;

[TestFixture]
[SuppressMessage("ReSharper", "MethodSupportsCancellation")]
[SuppressMessage("Reliability", "CA2016:Forward the \'CancellationToken\' parameter to methods")]
public class TaskProcessorTests
{
    private Mock<ITaskTracker<DigestId>> _mockTaskTracker;
    private Mock<ILogger<TaskProcessor>> _mockLogger;
    private IOptions<BackendDeploymentOptions> _deploymentOptions;
    private TaskProcessor _service;

    [TearDown]
    public void TearDown()
    {
        _service.Dispose();
    }

    [SetUp]
    public void SetUp()
    {
        _mockTaskTracker = new();
        _mockLogger = new();
        _deploymentOptions = Mock.Of<IOptions<BackendDeploymentOptions>>(x =>
            x.Value.MaxConcurrentAiTasks == 2
        );

        _service = new(_mockTaskTracker.Object, _mockLogger.Object, _deploymentOptions);
    }

    [Test]
    public async Task ExecuteAsync_ShouldProcessTasks()
    {
        // Arrange
        var digestId = new DigestId();
        var taskCompletionSource = new TaskCompletionSource();
        var invocationCount = 0;

        _mockTaskTracker
            .Setup(t => t.DequeueWaitingTask())
            .ReturnsAsync(() =>
            {
                if (invocationCount == 0)
                {
                    invocationCount++;
                    return (async _ => await taskCompletionSource.Task, null, digestId);
                }

                return (async _ => await Task.Delay(Timeout.Infinite), null, digestId);
            });

        _mockTaskTracker
            .Setup(t => t.MoveTaskToInProgress(digestId))
            .Returns(CancellationToken.None);

        // Act
        var cts = new CancellationTokenSource();
        _ = _service.StartAsync(cts.Token);

        // Simulate task completion
        taskCompletionSource.SetResult();

        // Wait for the service to process the task
        await Task.Delay(100);

        // Assert
        _mockTaskTracker.Verify(t => t.TryCompleteTaskInProgress(digestId), Times.Once);
    }

    [Test]
    public async Task ProcessesTask_CompletesSuccessfully()
    {
        // Arrange
        var digestId = new DigestId();
        var tcs = new TaskCompletionSource();
        var invocationCount = 0;
        _mockTaskTracker
            .Setup(t => t.DequeueWaitingTask())
            .ReturnsAsync(() =>
            {
                if (invocationCount == 0)
                {
                    invocationCount++;
                    return (_ => tcs.Task, null, digestId);
                }

                return (async _ => await Task.Delay(Timeout.Infinite), null, digestId);
            });
        _mockTaskTracker
            .Setup(t => t.MoveTaskToInProgress(digestId))
            .Returns(CancellationToken.None);

        // Act
        var cts = new CancellationTokenSource();
        _ = _service.StartAsync(cts.Token);
        tcs.SetResult(); // Complete the task
        await Task.Delay(1000); // Allow processing time

        // Assert
        _mockTaskTracker.Verify(t => t.TryCompleteTaskInProgress(digestId), Times.Once);
        await cts.CancelAsync();
    }

    [Test]
    public async Task ExecuteAsync_LogsErrors_WhenTaskFails()
    {
        // Arrange
        var digestId = new DigestId();
        var exception = new InvalidOperationException("Test error");
        _mockTaskTracker
            .Setup(t => t.DequeueWaitingTask())
            .ReturnsAsync((_ => throw exception, null, digestId));

        // Act
        var cts = new CancellationTokenSource();
        await _service.StartAsync(cts.Token);
        await Task.Delay(100);

        // Assert
        _mockLogger.Verify(log =>
            log.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>(
                    (v, _) =>
                        v.ToString()!.Contains("Error occurred during digest queue processing")
                ),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()!
            )
        );
        await cts.CancelAsync();
    }

    [Test]
    public async Task ExecuteAsync_ShouldReleaseSemaphore_WhenTaskFails()
    {
        // Arrange
        var digestId = new DigestId();
        var exception = new Exception("Test exception");
        var taskExecuted = false;

        _mockTaskTracker
            .Setup(t => t.DequeueWaitingTask())
            .ReturnsAsync(() =>
            {
                if (!taskExecuted)
                {
                    taskExecuted = true;
                    return (_ => throw exception, null, digestId);
                }
                return (async _ => await Task.Delay(100), null, new());
            });

        // Act
        var cts = new CancellationTokenSource();
        await _service.StartAsync(cts.Token);

        // Wait for first task to fail and second to start
        await Task.Delay(200);

        // Assert
        _mockTaskTracker.Verify(t => t.TryCompleteTaskInProgress(digestId), Times.Once);
        _mockTaskTracker.Verify(t => t.DequeueWaitingTask(), Times.AtLeast(2));

        await cts.CancelAsync();
    }

    [Test]
    public async Task ExecuteAsync_ShouldPropagateCancellation_ToRunningTasks()
    {
        // Arrange
        var digestId = new DigestId();
        var taskStarted = new TaskCompletionSource();
        var taskCancelled = new TaskCompletionSource<bool>();

        _mockTaskTracker
            .Setup(t => t.DequeueWaitingTask())
            .ReturnsAsync(
                () =>
                    (
                        async ct =>
                        {
                            taskStarted.SetResult();
                            try
                            {
                                await Task.Delay(Timeout.Infinite, ct);
                            }
                            catch (OperationCanceledException)
                            {
                                taskCancelled.SetResult(true);
                                throw;
                            }
                        },
                        null,
                        digestId
                    )
            );

        _mockTaskTracker
            .Setup(t => t.MoveTaskToInProgress(digestId))
            .Returns(CancellationToken.None);

        // Act
        var cts = new CancellationTokenSource();
        await _service.StartAsync(cts.Token);

        // Wait for task to start
        await taskStarted.Task;

        // Cancel the service
        await cts.CancelAsync();

        // Assert
        Assert.That(
            async () => await taskCancelled.Task.WaitAsync(TimeSpan.FromSeconds(1)),
            Throws.Nothing
        );
    }

    [Test]
    public async Task StopAsync_CancelsWaitingTasks()
    {
        // Arrange
        var digestId = new DigestId();
        var taskStarted = new TaskCompletionSource();
        var taskCancelled = new TaskCompletionSource<bool>();

        _mockTaskTracker
            .Setup(t => t.DequeueWaitingTask())
            .ReturnsAsync(
                () =>
                    (
                        async ct =>
                        {
                            taskStarted.SetResult();
                            try
                            {
                                await Task.Delay(5000, ct);
                            }
                            catch (OperationCanceledException)
                            {
                                taskCancelled.SetResult(true);
                                throw;
                            }
                        },
                        null,
                        digestId
                    )
            );

        // Act
        await _service.StartAsync(CancellationToken.None);
        await taskStarted.Task;

        // Stop the service
        await _service.StopAsync(CancellationToken.None);

        // Assert
        _mockTaskTracker.Verify(x => x.CancelAllTasksInProgress(), Times.Once);
        Assert.That(
            async () => await taskCancelled.Task.WaitAsync(TimeSpan.FromSeconds(1)),
            Throws.Nothing
        );
    }

    [Test]
    public async Task HandleException_CalledWhenHandlerProvided()
    {
        // Arrange
        var digestId = new DigestId();
        var exception = new Exception("Test exception");
        var exceptionHandled = new TaskCompletionSource<Exception>();

        _mockTaskTracker
            .Setup(t => t.DequeueWaitingTask())
            .ReturnsAsync(
                () =>
                    (
                        _ => throw exception,
                        (Exception ex) =>
                        {
                            exceptionHandled.SetResult(ex);
                            return Task.CompletedTask;
                        },
                        digestId
                    )
            );

        // Act
        var cts = new CancellationTokenSource();
        await _service.StartAsync(cts.Token);

        // Wait for exception handler
        var handledException = await exceptionHandled.Task.WaitAsync(TimeSpan.FromSeconds(1));

        // Assert
        Assert.That(handledException, Is.SameAs(exception));
        await cts.CancelAsync();
    }
}
