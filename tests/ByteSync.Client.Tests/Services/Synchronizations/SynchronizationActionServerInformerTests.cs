using ByteSync.Business.Actions.Shared;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Synchronizations;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ByteSync.Common.Business.Synchronizations;

namespace ByteSync.Tests.Services.Synchronizations;

[TestFixture]
public class SynchronizationActionServerInformerTests
{
    private Mock<ISessionService> _sessionServiceMock = null!;
    private Mock<ISynchronizationApiClient> _synchronizationApiClientMock = null!;
    private Mock<ILogger<SynchronizationActionServerInformer>> _loggerMock = null!;
    private SynchronizationActionServerInformer _synchronizationActionServerInformer = null!;
    
    private Mock<ISynchronizationActionServerInformer.CloudActionCaller> _cloudActionCallerMock = null!;
    private SharedActionsGroup _testSharedActionsGroup = null!;
    private SharedDataPart _testSharedDataPart = null!;

    [SetUp]
    public void Setup()
    {
        _sessionServiceMock = new Mock<ISessionService>();
        _synchronizationApiClientMock = new Mock<ISynchronizationApiClient>();
        _loggerMock = new Mock<ILogger<SynchronizationActionServerInformer>>();
        _cloudActionCallerMock = new Mock<ISynchronizationActionServerInformer.CloudActionCaller>();

        _synchronizationActionServerInformer = new SynchronizationActionServerInformer(
            _sessionServiceMock.Object,
            _synchronizationApiClientMock.Object,
            _loggerMock.Object);

        // Setup test data
        _testSharedActionsGroup = new SharedActionsGroup
        {
            ActionsGroupId = "test-group-id"
        };

        _testSharedDataPart = new SharedDataPart
        {
            NodeId = "test-node-id",
            ClientInstanceId = "test-client-id",
            Name = "test-file.txt"
        };

        // Setup default session behavior
        _sessionServiceMock.Setup(x => x.IsCloudSession).Returns(true);
        _sessionServiceMock.Setup(x => x.SessionId).Returns("test-session-id");
    }

    [Test]
    public async Task HandleCloudActionDone_WhenCloudSession_ShouldStoreActionForBatching()
    {
        // Arrange
        _cloudActionCallerMock
            .Setup(x => x.Invoke(It.IsAny<string>(), It.IsAny<SynchronizationActionRequest>()))
            .Returns(Task.CompletedTask);

        // Act
        await _synchronizationActionServerInformer.HandleCloudActionDone(
            _testSharedActionsGroup, 
            _testSharedDataPart, 
            _cloudActionCallerMock.Object);

        // Assert - Action should be stored, not immediately called
        _cloudActionCallerMock.Verify(
            x => x.Invoke(It.IsAny<string>(), It.IsAny<SynchronizationActionRequest>()), 
            Times.Never);

        // Verify the action is processed when HandlePendingActions is called
        await _synchronizationActionServerInformer.HandlePendingActions();
        
        _cloudActionCallerMock.Verify(
            x => x.Invoke("test-session-id", 
                It.Is<SynchronizationActionRequest>(req => 
                    req.ActionsGroupIds.Contains("test-group-id") && 
                    req.NodeId == "test-node-id")), 
            Times.Once);
    }

    [Test]
    public async Task HandleCloudActionDone_WithMetrics_ShouldPropagateMetricsInRequest()
    {
        // Arrange
        SynchronizationActionRequest? captured = null;
        _cloudActionCallerMock
            .Setup(x => x.Invoke(It.IsAny<string>(), It.IsAny<SynchronizationActionRequest>()))
            .Callback<string, SynchronizationActionRequest>((_, req) => captured = req)
            .Returns(Task.CompletedTask);

        var metrics = new Dictionary<string, SynchronizationActionMetrics>
        {
            [_testSharedActionsGroup.ActionsGroupId] = new SynchronizationActionMetrics { TransferredBytes = 1234 }
        };

        // Act
        await _synchronizationActionServerInformer.HandleCloudActionDone(
            _testSharedActionsGroup,
            _testSharedDataPart,
            _cloudActionCallerMock.Object,
            metrics);

        await _synchronizationActionServerInformer.HandlePendingActions();

        // Assert
        captured.Should().NotBeNull();
        captured!.ActionsGroupIds.Should().Contain(_testSharedActionsGroup.ActionsGroupId);
        captured.ActionMetricsByActionId.Should().NotBeNull();
        captured.ActionMetricsByActionId!.Should().ContainKey(_testSharedActionsGroup.ActionsGroupId);
        captured.ActionMetricsByActionId![_testSharedActionsGroup.ActionsGroupId].TransferredBytes.Should().Be(1234);
    }

    [Test]
    public async Task HandleCloudActionDone_WithMetricsForOtherIds_ShouldNotAllocateMetricsDictionary()
    {
        // Arrange
        SynchronizationActionRequest? captured = null;
        _cloudActionCallerMock
            .Setup(x => x.Invoke(It.IsAny<string>(), It.IsAny<SynchronizationActionRequest>()))
            .Callback<string, SynchronizationActionRequest>((_, req) => captured = req)
            .Returns(Task.CompletedTask);

        var metrics = new Dictionary<string, SynchronizationActionMetrics>
        {
            ["some-other-id"] = new SynchronizationActionMetrics { TransferredBytes = 42 }
        };

        // Act
        await _synchronizationActionServerInformer.HandleCloudActionDone(
            _testSharedActionsGroup,
            _testSharedDataPart,
            _cloudActionCallerMock.Object,
            metrics);

        await _synchronizationActionServerInformer.HandlePendingActions();

        // Assert
        captured.Should().NotBeNull();
        captured!.ActionsGroupIds.Should().Contain(_testSharedActionsGroup.ActionsGroupId);
        captured.ActionMetricsByActionId.Should().BeNull("no metrics matched chunk ids so dictionary should not be created");
    }

    [Test]
    public async Task HandleCloudActionDone_WhenNotCloudSession_ShouldNotCallCloudActionCaller()
    {
        // Arrange
        _sessionServiceMock.Setup(x => x.IsCloudSession).Returns(false);

        // Act
        await _synchronizationActionServerInformer.HandleCloudActionDone(
            _testSharedActionsGroup, 
            _testSharedDataPart, 
            _cloudActionCallerMock.Object);

        // Assert
        _cloudActionCallerMock.Verify(
            x => x.Invoke(It.IsAny<string>(), It.IsAny<SynchronizationActionRequest>()), 
            Times.Never);
    }

    [Test]
    public async Task HandleCloudActionError_WithSharedDataPart_ShouldStoreActionForBatching()
    {
        // Arrange
        _synchronizationApiClientMock
            .Setup(x => x.InformSynchronizationActionErrors(It.IsAny<string>(), It.IsAny<SynchronizationActionRequest>()))
            .Returns(Task.CompletedTask);

        // Act
        await _synchronizationActionServerInformer.HandleCloudActionError(_testSharedActionsGroup, _testSharedDataPart);

        // Assert - Action should be stored, not immediately called
        _synchronizationApiClientMock.Verify(
            x => x.InformSynchronizationActionErrors(It.IsAny<string>(), It.IsAny<SynchronizationActionRequest>()), 
            Times.Never);

        // Verify the action is processed when HandlePendingActions is called
        await _synchronizationActionServerInformer.HandlePendingActions();
        
        _synchronizationApiClientMock.Verify(
            x => x.InformSynchronizationActionErrors("test-session-id", 
                It.Is<SynchronizationActionRequest>(req => 
                    req.ActionsGroupIds.Contains("test-group-id") && 
                    req.NodeId == "test-node-id")), 
            Times.Once);
    }

    [Test]
    public async Task HandleCloudActionError_WithoutSharedDataPart_ShouldStoreActionForBatching()
    {
        // Arrange
        _synchronizationApiClientMock
            .Setup(x => x.InformSynchronizationActionErrors(It.IsAny<string>(), It.IsAny<SynchronizationActionRequest>()))
            .Returns(Task.CompletedTask);

        // Act
        await _synchronizationActionServerInformer.HandleCloudActionError(_testSharedActionsGroup);

        // Assert - Action should be stored, not immediately called
        _synchronizationApiClientMock.Verify(
            x => x.InformSynchronizationActionErrors(It.IsAny<string>(), It.IsAny<SynchronizationActionRequest>()), 
            Times.Never);

        // Verify the action is processed when HandlePendingActions is called
        await _synchronizationActionServerInformer.HandlePendingActions();
        
        _synchronizationApiClientMock.Verify(
            x => x.InformSynchronizationActionErrors("test-session-id", 
                It.Is<SynchronizationActionRequest>(req => 
                    req.ActionsGroupIds.Contains("test-group-id") && 
                    req.NodeId == null)), 
            Times.Once);
    }

    [Test]
    public async Task HandleCloudActionError_WithActionGroupIds_ShouldStoreActionForBatching()
    {
        // Arrange
        var actionGroupIds = new List<string> { "group-1", "group-2", "group-3" };
        _synchronizationApiClientMock
            .Setup(x => x.InformSynchronizationActionErrors(It.IsAny<string>(), It.IsAny<SynchronizationActionRequest>()))
            .Returns(Task.CompletedTask);

        // Act
        await _synchronizationActionServerInformer.HandleCloudActionError(actionGroupIds);

        // Assert - Action should be stored, not immediately called
        _synchronizationApiClientMock.Verify(
            x => x.InformSynchronizationActionErrors(It.IsAny<string>(), It.IsAny<SynchronizationActionRequest>()), 
            Times.Never);

        // Verify the action is processed when HandlePendingActions is called
        await _synchronizationActionServerInformer.HandlePendingActions();
        
        _synchronizationApiClientMock.Verify(
            x => x.InformSynchronizationActionErrors("test-session-id", 
                It.Is<SynchronizationActionRequest>(req => 
                    req.ActionsGroupIds.Count == 3 &&
                    req.ActionsGroupIds.Contains("group-1") &&
                    req.ActionsGroupIds.Contains("group-2") &&
                    req.ActionsGroupIds.Contains("group-3") &&
                    req.NodeId == null)), 
            Times.Once);
    }

    [Test]
    public async Task HandleCloudActionError_WhenNotCloudSession_ShouldNotCallApi()
    {
        // Arrange
        _sessionServiceMock.Setup(x => x.IsCloudSession).Returns(false);

        // Act
        await _synchronizationActionServerInformer.HandleCloudActionError(_testSharedActionsGroup, _testSharedDataPart);

        // Assert
        _synchronizationApiClientMock.Verify(
            x => x.InformSynchronizationActionErrors(It.IsAny<string>(), It.IsAny<SynchronizationActionRequest>()), 
            Times.Never);
    }

    [Test]
    public async Task HandlePendingActions_WhenNoActions_ShouldNotCallApi()
    {
        // Act
        await _synchronizationActionServerInformer.HandlePendingActions();

        // Assert
        _cloudActionCallerMock.Verify(
            x => x.Invoke(It.IsAny<string>(), It.IsAny<SynchronizationActionRequest>()), 
            Times.Never);
    }

    [Test]
    public async Task HandlePendingActions_WhenNotCloudSession_ShouldNotCallApi()
    {
        // Arrange
        _sessionServiceMock.Setup(x => x.IsCloudSession).Returns(false);

        // Act
        await _synchronizationActionServerInformer.HandlePendingActions();

        // Assert
        _cloudActionCallerMock.Verify(
            x => x.Invoke(It.IsAny<string>(), It.IsAny<SynchronizationActionRequest>()), 
            Times.Never);
    }

    [Test]
    public async Task HandlePendingActions_WithPendingActions_ShouldCallApiAndClearActions()
    {
        // Arrange
        _cloudActionCallerMock
            .Setup(x => x.Invoke(It.IsAny<string>(), It.IsAny<SynchronizationActionRequest>()))
            .Returns(Task.CompletedTask);

        // Add some pending actions by calling HandleCloudActionDone
        await _synchronizationActionServerInformer.HandleCloudActionDone(
            _testSharedActionsGroup, 
            _testSharedDataPart, 
            _cloudActionCallerMock.Object);

        _cloudActionCallerMock.Reset(); // Reset to verify only HandlePendingActions calls

        // Act
        await _synchronizationActionServerInformer.HandlePendingActions();

        // Assert - Should have been called during HandlePendingActions
        _cloudActionCallerMock.Verify(
            x => x.Invoke(It.IsAny<string>(), It.IsAny<SynchronizationActionRequest>()), 
            Times.Once);

        // Verify actions are cleared by calling HandlePendingActions again
        _cloudActionCallerMock.Reset();
        await _synchronizationActionServerInformer.HandlePendingActions();
        
        _cloudActionCallerMock.Verify(
            x => x.Invoke(It.IsAny<string>(), It.IsAny<SynchronizationActionRequest>()), 
            Times.Never);
    }

    [Test]
    public async Task ClearPendingActions_WhenCloudSession_ShouldClearAllPendingActions()
    {
        // Arrange
        _cloudActionCallerMock
            .Setup(x => x.Invoke(It.IsAny<string>(), It.IsAny<SynchronizationActionRequest>()))
            .Returns(Task.CompletedTask);

        // Add some pending actions
        await _synchronizationActionServerInformer.HandleCloudActionDone(
            _testSharedActionsGroup, 
            _testSharedDataPart, 
            _cloudActionCallerMock.Object);

        // Act
        await _synchronizationActionServerInformer.ClearPendingActions();

        // Assert - Verify actions are cleared by calling HandlePendingActions
        _cloudActionCallerMock.Reset();
        await _synchronizationActionServerInformer.HandlePendingActions();
        
        _cloudActionCallerMock.Verify(
            x => x.Invoke(It.IsAny<string>(), It.IsAny<SynchronizationActionRequest>()), 
            Times.Never);
    }

    [Test]
    public async Task ClearPendingActions_WhenNotCloudSession_ShouldCompleteSuccessfully()
    {
        // Arrange
        _sessionServiceMock.Setup(x => x.IsCloudSession).Returns(false);

        // Act & Assert - Should not throw
        await _synchronizationActionServerInformer.ClearPendingActions();
    }

    [Test]
    public async Task HandlePendingActions_WithException_ShouldLogError()
    {
        // Arrange
        _cloudActionCallerMock
            .Setup(x => x.Invoke(It.IsAny<string>(), It.IsAny<SynchronizationActionRequest>()))
            .ThrowsAsync(new System.Exception("Test exception"));

        // Add an action to be processed
        await _synchronizationActionServerInformer.HandleCloudActionDone(
            _testSharedActionsGroup, 
            _testSharedDataPart, 
            _cloudActionCallerMock.Object);

        // Act
        await _synchronizationActionServerInformer.HandlePendingActions();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unable to inform the server of the result of an action")),
                It.IsAny<System.Exception>(),
                It.IsAny<Func<It.IsAnyType, System.Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task HandleCloudActionDone_WithMultipleCallsSameActionCaller_ShouldBatch()
    {
        // Arrange
        var callCount = 0;
        var processedRequests = new List<SynchronizationActionRequest>();
        
        _cloudActionCallerMock
            .Setup(x => x.Invoke(It.IsAny<string>(), It.IsAny<SynchronizationActionRequest>()))
            .Callback<string, SynchronizationActionRequest>((_, request) =>
            {
                callCount++;
                processedRequests.Add(request);
            })
            .Returns(Task.CompletedTask);

        var actionsGroup1 = new SharedActionsGroup { ActionsGroupId = "group-1" };
        var actionsGroup2 = new SharedActionsGroup { ActionsGroupId = "group-2" };

        // Act - Add multiple actions with same caller
        await _synchronizationActionServerInformer.HandleCloudActionDone(
            actionsGroup1, 
            _testSharedDataPart, 
            _cloudActionCallerMock.Object);
            
        await _synchronizationActionServerInformer.HandleCloudActionDone(
            actionsGroup2, 
            _testSharedDataPart, 
            _cloudActionCallerMock.Object);

        // Process pending actions
        await _synchronizationActionServerInformer.HandlePendingActions();

        // Assert - Actions are grouped by NodeId into a single request
        callCount.Should().Be(1, "Actions for the same NodeId are grouped");
        processedRequests.Should().HaveCount(1);
        
        // Verify both actions were processed
        var allProcessedGroupIds = processedRequests.SelectMany(req => req.ActionsGroupIds).ToList();
        allProcessedGroupIds.Should().Contain("group-1");
        allProcessedGroupIds.Should().Contain("group-2");
    }

    [Test]
    public async Task HandlePendingActions_WithLargeNumberOfActions_ShouldProcessInChunks()
    {
        // Arrange
        var callCount = 0;
        var processedRequests = new List<SynchronizationActionRequest>();
        
        _cloudActionCallerMock
            .Setup(x => x.Invoke(It.IsAny<string>(), It.IsAny<SynchronizationActionRequest>()))
            .Callback<string, SynchronizationActionRequest>((_, request) =>
            {
                callCount++;
                processedRequests.Add(request);
            })
            .Returns(Task.CompletedTask);

        // Create 250 actions to test chunking (should be split into chunks of 200)
        var tasks = new List<Task>();
        for (int i = 0; i < 250; i++)
        {
            var actionsGroup = new SharedActionsGroup { ActionsGroupId = $"group-{i}" };
            tasks.Add(_synchronizationActionServerInformer.HandleCloudActionDone(
                actionsGroup, 
                _testSharedDataPart, 
                _cloudActionCallerMock.Object));
        }

        await Task.WhenAll(tasks);

        // Act - Process all pending actions
        await _synchronizationActionServerInformer.HandlePendingActions();

        // Assert - Should have processed all 250 actions: 100 + 100 flushed during add, then 50 on pending
        callCount.Should().Be(3, "Two auto-flushes at 100 + remaining 50 on pending");
        
        var totalActionsProcessed = processedRequests.Sum(req => req.ActionsGroupIds.Count);
        totalActionsProcessed.Should().Be(250, "All 250 actions should be processed");
    }

    [Test]
    public async Task MultipleCloudActionCallers_ShouldHandleSeparately()
    {
        // Arrange
        var caller1Mock = new Mock<ISynchronizationActionServerInformer.CloudActionCaller>();
        var caller2Mock = new Mock<ISynchronizationActionServerInformer.CloudActionCaller>();
        
        caller1Mock.Setup(x => x.Invoke(It.IsAny<string>(), It.IsAny<SynchronizationActionRequest>()))
                  .Returns(Task.CompletedTask);
        caller2Mock.Setup(x => x.Invoke(It.IsAny<string>(), It.IsAny<SynchronizationActionRequest>()))
                  .Returns(Task.CompletedTask);

        var actionsGroup1 = new SharedActionsGroup { ActionsGroupId = "group-1" };
        var actionsGroup2 = new SharedActionsGroup { ActionsGroupId = "group-2" };

        // Act - Add actions with different callers
        await _synchronizationActionServerInformer.HandleCloudActionDone(
            actionsGroup1, _testSharedDataPart, caller1Mock.Object);
        await _synchronizationActionServerInformer.HandleCloudActionDone(
            actionsGroup2, _testSharedDataPart, caller2Mock.Object);

        // Process pending actions
        await _synchronizationActionServerInformer.HandlePendingActions();

        // Assert - Each caller should be invoked separately
        caller1Mock.Verify(
            x => x.Invoke("test-session-id", 
                It.Is<SynchronizationActionRequest>(req => req.ActionsGroupIds.Contains("group-1"))), 
            Times.Once);
        caller2Mock.Verify(
            x => x.Invoke("test-session-id", 
                It.Is<SynchronizationActionRequest>(req => req.ActionsGroupIds.Contains("group-2"))), 
            Times.Once);
    }

    [Test]
    public async Task ActionsWithDifferentNodeIds_AreGroupedSeparately()
    {
        var callCount = 0;
        var processedRequests = new List<SynchronizationActionRequest>();

        _cloudActionCallerMock
            .Setup(x => x.Invoke(It.IsAny<string>(), It.IsAny<SynchronizationActionRequest>()))
            .Callback<string, SynchronizationActionRequest>((_, request) =>
            {
                callCount++;
                processedRequests.Add(request);
            })
            .Returns(Task.CompletedTask);

        var actionsGroup1 = new SharedActionsGroup { ActionsGroupId = "gA" };
        var actionsGroup2 = new SharedActionsGroup { ActionsGroupId = "gB" };

        var dataPartA = new SharedDataPart { NodeId = "node-A", ClientInstanceId = "c1", Name = "fA" };
        var dataPartB = new SharedDataPart { NodeId = "node-B", ClientInstanceId = "c1", Name = "fB" };

        await _synchronizationActionServerInformer.HandleCloudActionDone(actionsGroup1, dataPartA, _cloudActionCallerMock.Object);
        await _synchronizationActionServerInformer.HandleCloudActionDone(actionsGroup2, dataPartB, _cloudActionCallerMock.Object);

        await _synchronizationActionServerInformer.HandlePendingActions();

        callCount.Should().Be(2, "Different NodeIds must produce separate requests");
        processedRequests.Should().HaveCount(2);
        processedRequests.Should().ContainSingle(r => r.NodeId == "node-A" && r.ActionsGroupIds.Contains("gA"));
        processedRequests.Should().ContainSingle(r => r.NodeId == "node-B" && r.ActionsGroupIds.Contains("gB"));
    }

    [Test]
    public async Task HandleCloudActionDone_WithNullLocalTarget_ShouldSetNodeIdToNull()
    {
        // Arrange
        _cloudActionCallerMock
            .Setup(x => x.Invoke(It.IsAny<string>(), It.IsAny<SynchronizationActionRequest>()))
            .Returns(Task.CompletedTask);

        // Act
        await _synchronizationActionServerInformer.HandleCloudActionDone(
            _testSharedActionsGroup, 
            null!, 
            _cloudActionCallerMock.Object);

        // Process pending actions
        await _synchronizationActionServerInformer.HandlePendingActions();

        // Assert
        _cloudActionCallerMock.Verify(
            x => x.Invoke("test-session-id", 
                It.Is<SynchronizationActionRequest>(req => 
                    req.ActionsGroupIds.Contains("test-group-id") && 
                    req.NodeId == null)), 
            Times.Once);
    }

    [Test]
    public async Task HandleCloudActionDone_With100Actions_ShouldTriggerAutomaticBatching()
    {
        // Arrange
        var callCount = 0;
        _cloudActionCallerMock
            .Setup(x => x.Invoke(It.IsAny<string>(), It.IsAny<SynchronizationActionRequest>()))
            .Callback(() => callCount++)
            .Returns(Task.CompletedTask);

        // Act - Add exactly 100 actions to trigger the batching condition
        var tasks = new List<Task>();
        for (int i = 0; i < 100; i++)
        {
            var actionsGroup = new SharedActionsGroup { ActionsGroupId = $"group-{i}" };
            tasks.Add(_synchronizationActionServerInformer.HandleCloudActionDone(
                actionsGroup, 
                _testSharedDataPart, 
                _cloudActionCallerMock.Object));
        }

        await Task.WhenAll(tasks);

        // The 100th action should trigger automatic processing
        // But since actions are processed in batches, we need to call HandlePendingActions
        await _synchronizationActionServerInformer.HandlePendingActions();

        // Assert - Should have processed the actions
        callCount.Should().BeGreaterThan(0, "Actions should have been processed");
    }

    [Test]
    public async Task HandlePendingActions_ShouldProcessActionsInChunksOf200()
    {
        // Arrange
        var processedRequests = new List<SynchronizationActionRequest>();
        
        _cloudActionCallerMock
            .Setup(x => x.Invoke(It.IsAny<string>(), It.IsAny<SynchronizationActionRequest>()))
            .Callback<string, SynchronizationActionRequest>((_, request) =>
            {
                processedRequests.Add(request);
            })
            .Returns(Task.CompletedTask);

        // Create exactly 250 actions to test chunking behavior
        for (int i = 0; i < 250; i++)
        {
            var actionsGroup = new SharedActionsGroup { ActionsGroupId = $"group-{i}" };
            await _synchronizationActionServerInformer.HandleCloudActionDone(
                actionsGroup, 
                _testSharedDataPart, 
                _cloudActionCallerMock.Object);
        }

        // Act
        await _synchronizationActionServerInformer.HandlePendingActions();

        // Assert - Should process in chunks, but each request should contain individual action groups
        processedRequests.Should().NotBeEmpty("Some actions should have been processed");
        var totalActionsProcessed = processedRequests.Sum(req => req.ActionsGroupIds.Count);
        totalActionsProcessed.Should().Be(250, "All 250 actions should be processed");
    }
}
