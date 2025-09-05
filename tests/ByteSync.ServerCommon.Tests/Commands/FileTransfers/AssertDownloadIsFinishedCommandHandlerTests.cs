using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.FileTransfers;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Tests.Commands.FileTransfers;

[TestFixture]
public class AssertDownloadIsFinishedCommandHandlerTests
{
    private ICloudSessionsRepository _mockCloudSessionsRepository;
    private ITrackingActionRepository _mockTrackingActionRepository;
    private ISynchronizationProgressService _mockSynchronizationProgressService;
    private ISynchronizationStatusCheckerService _mockSynchronizationStatusCheckerService;
    private ISynchronizationService _mockSynchronizationService;
    private ILogger<AssertDownloadIsFinishedCommandHandler> _mockLogger;
    private AssertDownloadIsFinishedCommandHandler _assertDownloadIsFinishedCommandHandler;
    private ITransferLocationService _mockTransferLocationService;
    
    [SetUp]
    public void Setup()
    {
        _mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        _mockTrackingActionRepository = A.Fake<ITrackingActionRepository>();
        _mockSynchronizationProgressService = A.Fake<ISynchronizationProgressService>();
        _mockSynchronizationStatusCheckerService = A.Fake<ISynchronizationStatusCheckerService>();
        _mockSynchronizationService = A.Fake<ISynchronizationService>();
        _mockLogger = A.Fake<ILogger<AssertDownloadIsFinishedCommandHandler>>();
        _mockTransferLocationService = A.Fake<ITransferLocationService>();

        _assertDownloadIsFinishedCommandHandler = new AssertDownloadIsFinishedCommandHandler(
            _mockCloudSessionsRepository,
            _mockTrackingActionRepository,
            _mockSynchronizationProgressService,
            _mockSynchronizationStatusCheckerService,
            _mockSynchronizationService,
            _mockTransferLocationService, _mockLogger);
    }

    [Test]
    public async Task Handle_ValidRequest_AssertsDownloadIsFinished()
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };
        var sharedFileDefinition = new SharedFileDefinition { Id = "file1" };

        var transferParameters = new TransferParameters
        {
            SessionId = sessionId,
            SharedFileDefinition = sharedFileDefinition
        };
        var request = new AssertDownloadIsFinishedRequest(sessionId, client, transferParameters);

        // Mock the session repository to return a valid session member
        var mockSessionMember = A.Fake<SessionMemberData>();
        A.CallTo(() => _mockCloudSessionsRepository.GetSessionMember(sessionId, client)).Returns(mockSessionMember);

        // Act
        await _assertDownloadIsFinishedCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockCloudSessionsRepository.GetSessionMember(sessionId, client))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_WhenServiceThrowsException_PropagatesException()
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };
        var sharedFileDefinition = new SharedFileDefinition { Id = "file1" };
        var expectedException = new InvalidOperationException("Test exception");

        var transferParameters = new TransferParameters
        {
            SessionId = sessionId,
            SharedFileDefinition = sharedFileDefinition
        };
        var request = new AssertDownloadIsFinishedRequest(sessionId, client, transferParameters);

        // Mock the session repository to throw an exception
        A.CallTo(() => _mockCloudSessionsRepository.GetSessionMember(sessionId, client))
            .Throws(expectedException);

        // Act & Assert
        var exception = await FluentActions.Awaiting(() => 
            _assertDownloadIsFinishedCommandHandler.Handle(request, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();

        exception.Which.Should().Be(expectedException);
    }
    
    [Test]
    public async Task Handle_WhenSharedFileDefinitionNotAllowed_DoesNotUpdateTracking()
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };
        var sharedFileDefinition = new SharedFileDefinition { Id = "file1", SessionId = sessionId, SharedFileType = SharedFileTypes.FullSynchronization };
        var transferParameters = new TransferParameters { SessionId = sessionId, SharedFileDefinition = sharedFileDefinition };
        var request = new AssertDownloadIsFinishedRequest(sessionId, client, transferParameters);

        var mockSessionMember = new SessionMemberData { CloudSessionData = new CloudSessionData { SessionId = sessionId } };
        A.CallTo(() => _mockCloudSessionsRepository.GetSessionMember(sessionId, client)).Returns(mockSessionMember);
        A.CallTo(() => _mockTransferLocationService.IsSharedFileDefinitionAllowed(mockSessionMember, sharedFileDefinition)).Returns(false);

        // Act
        await _assertDownloadIsFinishedCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockTrackingActionRepository.AddOrUpdate(A<string>._, A<List<string>>._, A<Func<ByteSync.ServerCommon.Entities.TrackingActionEntity, ByteSync.ServerCommon.Entities.SynchronizationEntity, bool>>._))
            .MustNotHaveHappened();
        A.CallTo(() => _mockSynchronizationProgressService.UpdateSynchronizationProgress(A<ByteSync.ServerCommon.Business.Repositories.TrackingActionResult>._, A<bool>._))
            .MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_WhenNotSynchronization_DoesNotUpdateTracking()
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };
        var sharedFileDefinition = new SharedFileDefinition { Id = "file1", SessionId = sessionId, SharedFileType = SharedFileTypes.ProfileDetails };
        var transferParameters = new TransferParameters { SessionId = sessionId, SharedFileDefinition = sharedFileDefinition };
        var request = new AssertDownloadIsFinishedRequest(sessionId, client, transferParameters);

        var mockSessionMember = new SessionMemberData { CloudSessionData = new CloudSessionData { SessionId = sessionId } };
        A.CallTo(() => _mockCloudSessionsRepository.GetSessionMember(sessionId, client)).Returns(mockSessionMember);
        A.CallTo(() => _mockTransferLocationService.IsSharedFileDefinitionAllowed(mockSessionMember, sharedFileDefinition)).Returns(true);

        // Act
        await _assertDownloadIsFinishedCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockTrackingActionRepository.AddOrUpdate(A<string>._, A<List<string>>._, A<Func<ByteSync.ServerCommon.Entities.TrackingActionEntity, ByteSync.ServerCommon.Entities.SynchronizationEntity, bool>>._))
            .MustNotHaveHappened();
        A.CallTo(() => _mockSynchronizationProgressService.UpdateSynchronizationProgress(A<ByteSync.ServerCommon.Business.Repositories.TrackingActionResult>._, A<bool>._))
            .MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_Synchronization_MultiFileZip_IncrementsProcessedAndExchangedBySize_AndSendsUpdate()
    {
        // Arrange
        var sessionId = "session-sync";
        var client = new Client { ClientInstanceId = "client1" };
        var sharedFileDefinition = new SharedFileDefinition
        {
            Id = "file-sync",
            SessionId = sessionId,
            SharedFileType = SharedFileTypes.FullSynchronization,
            IsMultiFileZip = true,
            UploadedFileLength = 999, // ignored in this branch
            ActionsGroupIds = ["ag1"]
        };
        var transferParameters = new TransferParameters { SessionId = sessionId, SharedFileDefinition = sharedFileDefinition };
        var request = new AssertDownloadIsFinishedRequest(sessionId, client, transferParameters);

        var mockSessionMember = new SessionMemberData { CloudSessionData = new CloudSessionData { SessionId = sessionId } };
        A.CallTo(() => _mockCloudSessionsRepository.GetSessionMember(sessionId, client)).Returns(mockSessionMember);
        A.CallTo(() => _mockTransferLocationService.IsSharedFileDefinitionAllowed(mockSessionMember, sharedFileDefinition)).Returns(true);

        var trackingAction = new ByteSync.ServerCommon.Entities.TrackingActionEntity { Size = 123 };
        // Add one target for current client so the action becomes finished
        trackingAction.TargetClientInstanceAndNodeIds.Add(new ByteSync.Common.Business.Actions.ClientInstanceIdAndNodeId { ClientInstanceId = client.ClientInstanceId, NodeId = "n1" });
        var synchronization = new ByteSync.ServerCommon.Entities.SynchronizationEntity();

        // Status can be updated, and finalization check returns true to propagate 'needSendSynchronizationUpdated=true'
        A.CallTo(() => _mockSynchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronization)).Returns(true);
        A.CallTo(() => _mockSynchronizationService.CheckSynchronizationIsFinished(synchronization)).Returns(true);

        A.CallTo(() => _mockTrackingActionRepository.AddOrUpdate(sessionId, sharedFileDefinition.ActionsGroupIds, A<Func<ByteSync.ServerCommon.Entities.TrackingActionEntity, ByteSync.ServerCommon.Entities.SynchronizationEntity, bool>>._))
            .Invokes((string _, List<string> _, Func<ByteSync.ServerCommon.Entities.TrackingActionEntity, ByteSync.ServerCommon.Entities.SynchronizationEntity, bool> updater) => updater(trackingAction, synchronization))
            .Returns(new ByteSync.ServerCommon.Business.Repositories.TrackingActionResult(true, [trackingAction], synchronization));

        // Act
        await _assertDownloadIsFinishedCommandHandler.Handle(request, CancellationToken.None);

        // Assert the repository is called
        A.CallTo(() => _mockTrackingActionRepository.AddOrUpdate(sessionId, sharedFileDefinition.ActionsGroupIds, A<Func<ByteSync.ServerCommon.Entities.TrackingActionEntity, ByteSync.ServerCommon.Entities.SynchronizationEntity, bool>>._))
            .MustHaveHappenedOnceExactly();

        // Assert progress update called with correct increments
        A.CallTo(() => _mockSynchronizationProgressService.UpdateSynchronizationProgress(
                A<ByteSync.ServerCommon.Business.Repositories.TrackingActionResult>.That.Matches(r =>
                    r.IsSuccess &&
                    r.SynchronizationEntity.Progress.FinishedAtomicActionsCount == 1 && // only current client target counted
                    r.SynchronizationEntity.Progress.ProcessedVolume == 123 &&
                    r.SynchronizationEntity.Progress.ExchangedVolume == 123
                ),
                A<bool>.That.IsEqualTo(true)))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_Synchronization_NotMultiFileZip_AlreadyFinished_NoProcessedIncrement_ExchangedByUploadedLength()
    {
        // Arrange
        var sessionId = "session-sync2";
        var client = new Client { ClientInstanceId = "client1" };
        var sharedFileDefinition = new SharedFileDefinition
        {
            Id = "file-sync2",
            SessionId = sessionId,
            SharedFileType = SharedFileTypes.DeltaSynchronization,
            IsMultiFileZip = false,
            UploadedFileLength = 777,
            ActionsGroupIds = ["ag2"]
        };
        var transferParameters = new TransferParameters { SessionId = sessionId, SharedFileDefinition = sharedFileDefinition };
        var request = new AssertDownloadIsFinishedRequest(sessionId, client, transferParameters);

        var mockSessionMember = new SessionMemberData { CloudSessionData = new CloudSessionData { SessionId = sessionId } };
        A.CallTo(() => _mockCloudSessionsRepository.GetSessionMember(sessionId, client)).Returns(mockSessionMember);
        A.CallTo(() => _mockTransferLocationService.IsSharedFileDefinitionAllowed(mockSessionMember, sharedFileDefinition)).Returns(true);

        var trackingAction = new ByteSync.ServerCommon.Entities.TrackingActionEntity { Size = 456 };
        var target = new ByteSync.Common.Business.Actions.ClientInstanceIdAndNodeId { ClientInstanceId = client.ClientInstanceId, NodeId = "n1" };
        trackingAction.TargetClientInstanceAndNodeIds.Add(target);
        // Already finished before update: mark as success already
        trackingAction.SuccessTargetClientInstanceAndNodeIds.Add(target);
        var synchronization = new ByteSync.ServerCommon.Entities.SynchronizationEntity();

        A.CallTo(() => _mockSynchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronization)).Returns(true);
        A.CallTo(() => _mockSynchronizationService.CheckSynchronizationIsFinished(synchronization)).Returns(false);

        A.CallTo(() => _mockTrackingActionRepository.AddOrUpdate(sessionId, sharedFileDefinition.ActionsGroupIds, A<Func<ByteSync.ServerCommon.Entities.TrackingActionEntity, ByteSync.ServerCommon.Entities.SynchronizationEntity, bool>>._))
            .Invokes((string _, List<string> _, Func<ByteSync.ServerCommon.Entities.TrackingActionEntity, ByteSync.ServerCommon.Entities.SynchronizationEntity, bool> updater) => updater(trackingAction, synchronization))
            .Returns(new ByteSync.ServerCommon.Business.Repositories.TrackingActionResult(true, [trackingAction], synchronization));

        // Act
        await _assertDownloadIsFinishedCommandHandler.Handle(request, CancellationToken.None);

        // Assert: ProcessedVolume should not increment (already finished), ExchangedVolume from UploadedFileLength, and needSendSynchronizationUpdated = false
        A.CallTo(() => _mockSynchronizationProgressService.UpdateSynchronizationProgress(
                A<ByteSync.ServerCommon.Business.Repositories.TrackingActionResult>.That.Matches(r =>
                    r.SynchronizationEntity.Progress.FinishedAtomicActionsCount == 1 &&
                    r.SynchronizationEntity.Progress.ProcessedVolume == 0 &&
                    r.SynchronizationEntity.Progress.ExchangedVolume == 777
                ),
                A<bool>.That.IsEqualTo(false)))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_Synchronization_WhenCannotBeUpdated_DoesNotSendProgress()
    {
        // Arrange
        var sessionId = "session-sync3";
        var client = new Client { ClientInstanceId = "client1" };
        var sharedFileDefinition = new SharedFileDefinition
        {
            Id = "file-sync3",
            SessionId = sessionId,
            SharedFileType = SharedFileTypes.FullSynchronization,
            ActionsGroupIds = ["ag3"]
        };
        var transferParameters = new TransferParameters { SessionId = sessionId, SharedFileDefinition = sharedFileDefinition };
        var request = new AssertDownloadIsFinishedRequest(sessionId, client, transferParameters);

        var mockSessionMember = new SessionMemberData { CloudSessionData = new CloudSessionData { SessionId = sessionId } };
        A.CallTo(() => _mockCloudSessionsRepository.GetSessionMember(sessionId, client)).Returns(mockSessionMember);
        A.CallTo(() => _mockTransferLocationService.IsSharedFileDefinitionAllowed(mockSessionMember, sharedFileDefinition)).Returns(true);

        var trackingAction = new ByteSync.ServerCommon.Entities.TrackingActionEntity();
        var synchronization = new ByteSync.ServerCommon.Entities.SynchronizationEntity();

        A.CallTo(() => _mockSynchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronization)).Returns(false);

        A.CallTo(() => _mockTrackingActionRepository.AddOrUpdate(sessionId, sharedFileDefinition.ActionsGroupIds, A<Func<ByteSync.ServerCommon.Entities.TrackingActionEntity, ByteSync.ServerCommon.Entities.SynchronizationEntity, bool>>._))
            .Invokes((string _, List<string> _, Func<ByteSync.ServerCommon.Entities.TrackingActionEntity, ByteSync.ServerCommon.Entities.SynchronizationEntity, bool> updater) => updater(trackingAction, synchronization))
            .Returns(new ByteSync.ServerCommon.Business.Repositories.TrackingActionResult(false, [trackingAction], synchronization));

        // Act
        await _assertDownloadIsFinishedCommandHandler.Handle(request, CancellationToken.None);

        // Assert: no progress update when repository signals failure
        A.CallTo(() => _mockSynchronizationProgressService.UpdateSynchronizationProgress(A<ByteSync.ServerCommon.Business.Repositories.TrackingActionResult>._, A<bool>._))
            .MustNotHaveHappened();
    }
}
