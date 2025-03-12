using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Moq;
using TelegramDigest.Backend.Core;
using TelegramDigest.Backend.DeploymentOptions;

namespace TelegramDigest.Application.Tests.UnitTests;

[TestFixture]
[SuppressMessage("ReSharper", "MethodSupportsCancellation")]
[SuppressMessage("Reliability", "CA2016:Forward the \'CancellationToken\' parameter to methods")]
public class TaskProcessorBackgroundServiceTests
{
    private Mock<ITaskProgressHandler<DigestId>> _mockTaskTracker;
    private Mock<ILogger<TaskProcessorBackgroundService>> _mockLogger;
    private BackendDeploymentOptions _deploymentOptions;
    private TaskProcessorBackgroundService _service;

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
        _deploymentOptions = new() { MaxConcurrentAiTasks = 1 };

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
                    return (async _ => await taskCompletionSource.Task, digestId);
                }

                return (async _ => await Task.Delay(Timeout.Infinite), digestId);
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
        _mockTaskTracker.Verify(t => t.CompleteTaskInProgress(digestId), Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_ProcessesTask_CompletesSuccessfully()
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
                    return (_ => tcs.Task, digestId);
                }

                return (async _ => await Task.Delay(Timeout.Infinite), digestId);
            });
        _mockTaskTracker
            .Setup(t => t.MoveTaskToInProgress(digestId))
            .Returns(CancellationToken.None);

        // Act
        var cts = new CancellationTokenSource();
        await _service.StartAsync(cts.Token);
        tcs.SetResult(); // Complete the task
        await Task.Delay(100); // Allow processing time

        // Assert
        _mockTaskTracker.Verify(t => t.CompleteTaskInProgress(digestId), Times.Once);
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
            .ReturnsAsync((_ => throw exception, digestId));

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
}
