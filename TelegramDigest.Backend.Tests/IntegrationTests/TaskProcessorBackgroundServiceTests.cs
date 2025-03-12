using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Moq;
using TelegramDigest.Backend.Core;
using TelegramDigest.Backend.DeploymentOptions;

namespace TelegramDigest.Application.Tests.IntegrationTests;

[TestFixture]
[SuppressMessage("ReSharper", "MethodSupportsCancellation")]
[SuppressMessage("Reliability", "CA2016:Forward the \'CancellationToken\' parameter to methods")]
public class TaskProcessorBackgroundServiceTests
{
    private readonly Mock<ILogger<TaskProcessorBackgroundService>> _mockLogger = new();

    [Test]
    public async Task RealTracker_ProcessesTask_ThroughFullLifecycle()
    {
        // Arrange
        var tracker = new TaskTracker<DigestId>();
        var service = new TaskProcessorBackgroundService(
            tracker,
            _mockLogger.Object,
            new() { MaxConcurrentAiTasks = 2 }
        );

        var digestId = new DigestId();
        var tcs = new TaskCompletionSource();

        // Add a task to queue
        tracker.AddTaskToWaitQueue(_ => tcs.Task, digestId);

        // Act
        var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);

        // Verify a task moved to in-progress
        await Task.Delay(100);
        Assert.That(tracker.GetInProgressTasks(), Has.Exactly(1).EqualTo(digestId));

        // Complete task
        tcs.SetResult();
        await Task.Delay(100);

        // Assert
        Assert.That(tracker.GetInProgressTasks(), Is.Empty);

        // Cleanup
        cts.Dispose();
        service.Dispose();
    }

    [Test]
    public async Task RealTracker_RespectsConcurrencyLimit()
    {
        // Arrange
        var tracker = new TaskTracker<DigestId>();
        var options = new BackendDeploymentOptions { MaxConcurrentAiTasks = 2 };
        var service = new TaskProcessorBackgroundService(tracker, _mockLogger.Object, options);

        var tcs1 = new TaskCompletionSource();
        var tcs2 = new TaskCompletionSource();
        var tcs3 = new TaskCompletionSource();
        var digestId1 = new DigestId();
        var digestId2 = new DigestId();
        var digestId3 = new DigestId();

        // Add tasks
        tracker.AddTaskToWaitQueue(_ => tcs1.Task, digestId1);
        tracker.AddTaskToWaitQueue(_ => tcs2.Task, digestId2);
        tracker.AddTaskToWaitQueue(_ => tcs3.Task, digestId3);

        // Act
        var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);

        // Verify both tasks start immediately
        await Task.Delay(100);
        Assert.That(tracker.GetInProgressTasks(), Has.Exactly(2).Items);
        Assert.That(tracker.GetWaitingTasks(), Has.Exactly(1).Items);

        // Cleanup
        tcs1.SetResult();
        tcs2.SetResult();
        tcs3.SetResult();
        cts.Dispose();
        service.Dispose();
    }

    [Test]
    public async Task RealTracker_ExecuteAsync_HandlesCancellation_DuringProcessing()
    {
        // Arrange
        var tracker = new TaskTracker<DigestId>();
        var service = new TaskProcessorBackgroundService(
            tracker,
            _mockLogger.Object,
            new() { MaxConcurrentAiTasks = 1 }
        );
        var cacnelationCalled = false;

        var digestId = new DigestId();

        tracker.AddTaskToWaitQueue(
            ct =>
            {
                ct.Register(() => cacnelationCalled = true);
                return Task.CompletedTask;
            },
            digestId
        );

        // Act
        var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);
        await cts.CancelAsync(); // Cancel service

        await Task.Delay(100); // Let the task complete

        // Assert
        Assert.That(cacnelationCalled);

        // Cleanup
        service.Dispose();
    }
}
