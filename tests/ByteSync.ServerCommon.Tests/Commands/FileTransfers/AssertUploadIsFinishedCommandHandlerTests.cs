using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Interfaces.Hub;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.FileTransfers;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Tests.Commands.FileTransfers;

[TestFixture]
public class AssertUploadIsFinishedCommandHandlerTests
{
    private ICloudSessionsRepository _mockCloudSessionsRepository;
    private ISharedFilesService _mockSharedFilesService;
    private ITrackingActionRepository _mockTrackingActionRepository;
    private ISynchronizationProgressService _mockSynchronizationProgressService;
    private ISynchronizationStatusCheckerService _mockSynchronizationStatusCheckerService;
    private IInvokeClientsService _mockInvokeClientsService;
    private ILogger<AssertUploadIsFinishedCommandHandler> _mockLogger;
    private AssertUploadIsFinishedCommandHandler _assertUploadIsFinishedCommandHandler;
    private ITransferLocationService _mockTransferLocationService = A.Fake<ITransferLocationService>();
    
    [SetUp]
    public void Setup()
    {
        _mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        _mockSharedFilesService = A.Fake<ISharedFilesService>();
        _mockTrackingActionRepository = A.Fake<ITrackingActionRepository>(x => x.Strict());
        _mockSynchronizationProgressService = A.Fake<ISynchronizationProgressService>(x => x.Strict());
        _mockSynchronizationStatusCheckerService = A.Fake<ISynchronizationStatusCheckerService>(x => x.Strict());
        _mockInvokeClientsService = A.Fake<IInvokeClientsService>();
        _mockLogger = A.Fake<ILogger<AssertUploadIsFinishedCommandHandler>>();
        _mockTransferLocationService = A.Fake<ITransferLocationService>();
        
        _assertUploadIsFinishedCommandHandler = new AssertUploadIsFinishedCommandHandler(
            _mockCloudSessionsRepository,
            _mockSharedFilesService,
            _mockTrackingActionRepository,
            _mockSynchronizationProgressService,
            _mockSynchronizationStatusCheckerService,
            _mockInvokeClientsService,
            _mockTransferLocationService, _mockLogger);
    }

    [Test]
    public async Task Handle_ValidRequest_AssertsUploadIsFinished()
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };
        var sharedFileDefinition = new SharedFileDefinition
        {
            Id = "file1",
            SharedFileType = SharedFileTypes.BaseInventory
        };
        var transferParameters = new TransferParameters
        {
            SessionId = sessionId,
            SharedFileDefinition = sharedFileDefinition,
            PartNumber = 1,
            TotalParts = 3
        };

        var request = new AssertUploadIsFinishedRequest(sessionId, client, transferParameters);

        // Mock the session repository to return a valid session
        var mockSession = new CloudSessionData();
        var mockSessionMember = new SessionMemberData { ClientInstanceId = client.ClientInstanceId };
        mockSession.SessionMembers.Add(mockSessionMember);
        
        // Add another member so we have a target audience
        var otherMember = new SessionMemberData { ClientInstanceId = "other-client" };
        mockSession.SessionMembers.Add(otherMember);
        
        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId)).Returns(mockSession);
        
        // Mock the transfer location service to return true for IsSharedFileDefinitionAllowed
        A.CallTo(() => _mockTransferLocationService.IsSharedFileDefinitionAllowed(mockSessionMember, transferParameters.SharedFileDefinition))
            .Returns(true);

        // Inventory branch: mock shared files service and hub push
        A.CallTo(() => _mockSharedFilesService.AssertUploadIsFinished(A<TransferParameters>._, A<List<string>>.Ignored))
            .Returns(Task.CompletedTask);
        var hubPush = A.Fake<IHubByteSyncPush>();
        A.CallTo(() => _mockInvokeClientsService.Clients(A<ICollection<SessionMemberData>>.Ignored))
            .Returns(hubPush);
        A.CallTo(() => hubPush.UploadFinished(A<FileTransferPush>._))
            .Returns(Task.CompletedTask);

        // Act
        await _assertUploadIsFinishedCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockTransferLocationService.IsSharedFileDefinitionAllowed(mockSessionMember, transferParameters.SharedFileDefinition))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_WhenServiceThrowsException_PropagatesException()
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };
        var transferParameters = new TransferParameters
        {
            SessionId = sessionId,
            SharedFileDefinition = new SharedFileDefinition { Id = "file1" },
            PartNumber = 1,
            TotalParts = 3
        };
        var expectedException = new InvalidOperationException("Test exception");

        var request = new AssertUploadIsFinishedRequest(sessionId, client, transferParameters);

        // Mock the session repository to throw an exception
        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId))
            .Throws(expectedException);

        // Act & Assert
        var exception = await FluentActions.Awaiting(() => 
            _assertUploadIsFinishedCommandHandler.Handle(request, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();

        exception.Which.Should().Be(expectedException);
    }

    [Test]
    public async Task Handle_SynchronizationFile_WhenCheckSynchronizationSuccess_RunsNormally()
    {
        // Arrange
        var sessionId = "sessionId";
        var client = new Client { ClientInstanceId = "client1" };
        var sharedFileDefinition = new SharedFileDefinition 
        { 
            SessionId = sessionId,
            ActionsGroupIds = new List<string> { "actionGroupId1" }
        };
        var transferParameters = new TransferParameters
        {
            SessionId = sessionId,
            SharedFileDefinition = sharedFileDefinition,
            TotalParts = 3
        };
        var request = new AssertUploadIsFinishedRequest(sessionId, client, transferParameters);

        // Mock the session repository to return a valid session
        var mockSession = new CloudSessionData();
        var mockSessionMember = new SessionMemberData { ClientInstanceId = client.ClientInstanceId };
        mockSession.SessionMembers.Add(mockSessionMember);
        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId)).Returns(mockSession);
        
        // Mock the transfer location service to return true for IsSharedFileDefinitionAllowed
        A.CallTo(() => _mockTransferLocationService.IsSharedFileDefinitionAllowed(mockSessionMember, sharedFileDefinition))
            .Returns(true);

        // Mock tracking action repository
        var trackingActionEntity = new TrackingActionEntity();
        trackingActionEntity.TargetClientInstanceAndNodeIds.Add("targetClientInstanceId_nodeId");
        var synchronizationEntity = new SynchronizationEntity();

        A.CallTo(() => _mockTrackingActionRepository.AddOrUpdate(sessionId, A<List<string>>.Ignored, A<Func<TrackingActionEntity, SynchronizationEntity, bool>>.Ignored))
            .Invokes((string _, List<string> _, Func<TrackingActionEntity, SynchronizationEntity, bool> func) => func(trackingActionEntity, synchronizationEntity))
            .Returns(new TrackingActionResult(true, new List<TrackingActionEntity>(), synchronizationEntity));
            
        A.CallTo(() => _mockSynchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronizationEntity))
            .Returns(true);

        A.CallTo(() => _mockSharedFilesService.AssertUploadIsFinished(A<TransferParameters>._, A<ICollection<string>>.Ignored))
            .Returns(Task.CompletedTask);
        var hubPush = A.Fake<IHubByteSyncPush>();
        A.CallTo(() => _mockInvokeClientsService.Clients(A<ICollection<string>>.Ignored))
            .Returns(hubPush);
        A.CallTo(() => hubPush.UploadFinished(A<FileTransferPush>._))
            .Returns(Task.CompletedTask);

        // Act
        await _assertUploadIsFinishedCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockTrackingActionRepository.AddOrUpdate(sessionId, A<List<string>>.Ignored, A<Func<TrackingActionEntity, SynchronizationEntity, bool>>.Ignored))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockSynchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronizationEntity))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockSharedFilesService.AssertUploadIsFinished(A<TransferParameters>._, A<ICollection<string>>.Ignored))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_SynchronizationFile_WhenCheckSynchronizationFails_Aborts()
    {
        // Arrange
        var sessionId = "sessionId";
        var client = new Client { ClientInstanceId = "client1" };
        var sharedFileDefinition = new SharedFileDefinition 
        { 
            SessionId = sessionId,
            ActionsGroupIds = new List<string> { "actionGroupId1" }
        };
        var transferParameters = new TransferParameters
        {
            SessionId = sessionId,
            SharedFileDefinition = sharedFileDefinition,
            TotalParts = 3
        };
        var request = new AssertUploadIsFinishedRequest(sessionId, client, transferParameters);

        // Mock the session repository to return a valid session
        var mockSession = new CloudSessionData();
        var mockSessionMember = new SessionMemberData { ClientInstanceId = client.ClientInstanceId };
        mockSession.SessionMembers.Add(mockSessionMember);
        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId)).Returns(mockSession);
        
        // Mock the transfer location service to return true for IsSharedFileDefinitionAllowed
        A.CallTo(() => _mockTransferLocationService.IsSharedFileDefinitionAllowed(mockSessionMember, sharedFileDefinition))
            .Returns(true);

        // Mock tracking action repository to fail synchronization check
        var trackingActionEntity = new TrackingActionEntity();
        trackingActionEntity.TargetClientInstanceAndNodeIds.Add("targetClientInstanceId_nodeId");
        var synchronizationEntity = new SynchronizationEntity();

        A.CallTo(() => _mockTrackingActionRepository.AddOrUpdate(sessionId, A<List<string>>.Ignored, A<Func<TrackingActionEntity, SynchronizationEntity, bool>>.Ignored))
            .Invokes((string _, List<string> _, Func<TrackingActionEntity, SynchronizationEntity, bool> func) => func(trackingActionEntity, synchronizationEntity))
            .Returns(new TrackingActionResult(false, new List<TrackingActionEntity>(), synchronizationEntity));
            
        A.CallTo(() => _mockSynchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronizationEntity))
            .Returns(false);

        // Act
        await _assertUploadIsFinishedCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockTrackingActionRepository.AddOrUpdate(sessionId, A<List<string>>.Ignored, A<Func<TrackingActionEntity, SynchronizationEntity, bool>>.Ignored))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockSynchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronizationEntity))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockSharedFilesService.AssertUploadIsFinished(A<TransferParameters>._, A<ICollection<string>>.Ignored))
            .MustNotHaveHappened();
    }

} 