using ByteSync.Common.Business.Synchronizations;
using ByteSync.Common.Interfaces.Hub;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Mappers;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using ByteSync.ServerCommon.Services;
using FakeItEasy;
using FluentAssertions;

namespace ByteSync.ServerCommon.Tests.Services;

[TestFixture]
public class SynchronizationProgressServiceTests
{
    private IInvokeClientsService _invokeClientsService;
    private ITrackingActionMapper _trackingActionMapper;
    private ISynchronizationMapper _synchronizationMapper;
    private SynchronizationProgressService _service;

    [SetUp]
    public void SetUp()
    {
        _invokeClientsService = A.Fake<IInvokeClientsService>(o => o.Strict());
        _trackingActionMapper = A.Fake<ITrackingActionMapper>(o => o.Strict());
        _synchronizationMapper = A.Fake<ISynchronizationMapper>(o => o.Strict());
        _service = new SynchronizationProgressService(_invokeClientsService, _trackingActionMapper, _synchronizationMapper);
    }

    [Test]
    public async Task UpdateSynchronizationProgress_WithTrackingActionResult_SendsProgressAndUpdated()
    {
        // Arrange
        var syncEntity = new SynchronizationEntity
        {
            SessionId = "session-1",
            Progress = new SynchronizationProgressEntity
            {
                Members = ["c1", "c2"],
                ActualUploadedVolume = 100,
                ActualDownloadedVolume = 200,
                LocalCopyTransferredVolume = 50,
                SynchronizedVolume = 300,
                ProcessedVolume = 10,
                ExchangedVolume = 20,
                FinishedAtomicActionsCount = 4,
                ErrorsCount = 1
            }
        };

        var ta1 = new TrackingActionEntity { ActionsGroupId = "ag1" };
        var ta2 = new TrackingActionEntity { ActionsGroupId = "ag2" };
        var trackingResult = new TrackingActionResult(true, [ta1, ta2], syncEntity);

        A.CallTo(() => _trackingActionMapper.MapToTrackingActionSummary(A<TrackingActionEntity>._))
            .ReturnsLazily(call =>
            {
                var e = call.GetArgument<TrackingActionEntity>(0);
                return new TrackingActionSummary { ActionsGroupId = e!.ActionsGroupId, IsSuccess = true };
            });

        var hub = A.Fake<IHubByteSyncPush>(o => o.Strict());
        A.CallTo(() => _invokeClientsService.Clients(A<ICollection<string>>._)).Returns(hub);

        A.CallTo(() => _synchronizationMapper.MapToSynchronization(syncEntity))
            .Returns(new Synchronization { SessionId = syncEntity.SessionId });

        A.CallTo(() => hub.SynchronizationProgressUpdated(A<SynchronizationProgressPush>._))
            .Invokes(call =>
            {
                var push = call.GetArgument<SynchronizationProgressPush>(0);
                push!.SessionId.Should().Be(syncEntity.SessionId);
                push.ActualUploadedVolume.Should().Be(100);
                push.ActualDownloadedVolume.Should().Be(200);
                push.LocalCopyTransferredVolume.Should().Be(50);
                push.SynchronizedVolume.Should().Be(300);
                push.ProcessedVolume.Should().Be(10);
                push.ExchangedVolume.Should().Be(20);
                push.FinishedActionsCount.Should().Be(4);
                push.ErrorActionsCount.Should().Be(1);
                push.Version.Should().BeGreaterThan(0);
                push.TrackingActionSummaries.Should().NotBeNull();
                push.TrackingActionSummaries!.Select(s => s.ActionsGroupId)
                    .Should().BeEquivalentTo(new[] { "ag1", "ag2" });
            })
            .Returns(Task.CompletedTask);

        A.CallTo(() => hub.SynchronizationUpdated(A<Synchronization>._))
            .Invokes(call =>
            {
                var s = call.GetArgument<Synchronization>(0);
                s!.SessionId.Should().Be(syncEntity.SessionId);
            })
            .Returns(Task.CompletedTask);

        // Act
        await _service.UpdateSynchronizationProgress(trackingResult, needSendSynchronizationUpdated: true);

        // Assert
        A.CallTo(() => _invokeClientsService.Clients(A<ICollection<string>>._)).MustHaveHappened();
        A.CallTo(() => hub.SynchronizationProgressUpdated(A<SynchronizationProgressPush>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => hub.SynchronizationUpdated(A<Synchronization>._)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task UpdateSynchronizationProgress_WithEntity_SendsProgressOnly_WhenFlagFalse()
    {
        // Arrange
        var syncEntity = new SynchronizationEntity
        {
            SessionId = "session-2",
            Progress = new SynchronizationProgressEntity
            {
                Members = ["m1"],
                ActualUploadedVolume = 1,
                ActualDownloadedVolume = 2,
                LocalCopyTransferredVolume = 3,
                SynchronizedVolume = 4,
                ProcessedVolume = 5,
                ExchangedVolume = 6,
                FinishedAtomicActionsCount = 7,
                ErrorsCount = 0
            }
        };

        var hub = A.Fake<IHubByteSyncPush>(o => o.Strict());
        A.CallTo(() => _invokeClientsService.Clients(A<ICollection<string>>._)).Returns(hub);

        A.CallTo(() => hub.SynchronizationProgressUpdated(A<SynchronizationProgressPush>._))
            .Invokes(call =>
            {
                var push = call.GetArgument<SynchronizationProgressPush>(0);
                push!.SessionId.Should().Be("session-2");
                push.TrackingActionSummaries.Should().BeNull();
                push.FinishedActionsCount.Should().Be(7);
                push.ErrorActionsCount.Should().Be(0);
            })
            .Returns(Task.CompletedTask);

        // Act
        await _service.UpdateSynchronizationProgress(syncEntity, needSendSynchronizationUpdated: false);

        // Assert
        A.CallTo(() => hub.SynchronizationProgressUpdated(A<SynchronizationProgressPush>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => hub.SynchronizationUpdated(A<Synchronization>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task InformSynchronizationStarted_SendsStartedToSessionGroup()
    {
        // Arrange
        var syncEntity = new SynchronizationEntity { SessionId = "session-3" };
        var client = new Client { ClientInstanceId = "c42" };

        var hub = A.Fake<IHubByteSyncPush>(o => o.Strict());
        A.CallTo(() => _invokeClientsService.SessionGroup("session-3")).Returns(hub);

        A.CallTo(() => _synchronizationMapper.MapToSynchronization(syncEntity))
            .Returns(new Synchronization { SessionId = "session-3" });

        A.CallTo(() => hub.SynchronizationStarted(A<Synchronization>._))
            .Invokes(call =>
            {
                var s = call.GetArgument<Synchronization>(0);
                s!.SessionId.Should().Be("session-3");
            })
            .Returns(Task.CompletedTask);

        // Act
        await _service.InformSynchronizationStarted(syncEntity, client);

        // Assert
        A.CallTo(() => _invokeClientsService.SessionGroup("session-3")).MustHaveHappenedOnceExactly();
        A.CallTo(() => hub.SynchronizationStarted(A<Synchronization>._)).MustHaveHappenedOnceExactly();
    }
}

