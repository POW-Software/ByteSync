using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.FileTransfers;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using FakeItEasy;
using ByteSync.Common.Interfaces.Hub;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Tests.Commands.FileTransfers;

[TestFixture]
public class AssertFilePartIsUploadedCommandHandlerTests
{
    private ICloudSessionsRepository _mockCloudSessionsRepository;
    private ISharedFilesService _mockSharedFilesService;
    private ISynchronizationRepository _mockSynchronizationRepository;
    private ITrackingActionRepository _mockTrackingActionRepository;
    private ISynchronizationStatusCheckerService _mockSynchronizationStatusCheckerService;
    private ISynchronizationProgressService _mockSynchronizationProgressService;
    private IInvokeClientsService _mockInvokeClientsService;
    private IUsageStatisticsService _mockUsageStatisticsService;
    private ITransferLocationService _mockTransferLocationService = A.Fake<ITransferLocationService>();
    private ILogger<AssertFilePartIsUploadedCommandHandler> _mockLogger;
    private AssertFilePartIsUploadedCommandHandler _assertFilePartIsUploadedCommandHandler;

    
    [SetUp]
    public void Setup()
    {
        _mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>(options => options.Strict());
        _mockSharedFilesService = A.Fake<ISharedFilesService>(options => options.Strict());
        _mockSynchronizationRepository = A.Fake<ISynchronizationRepository>(options => options.Strict());
        _mockTrackingActionRepository = A.Fake<ITrackingActionRepository>(options => options.Strict());
        _mockSynchronizationStatusCheckerService = A.Fake<ISynchronizationStatusCheckerService>(options => options.Strict());
        _mockSynchronizationProgressService = A.Fake<ISynchronizationProgressService>(options => options.Strict());
        _mockInvokeClientsService = A.Fake<IInvokeClientsService>(options => options.Strict());
        _mockUsageStatisticsService = A.Fake<IUsageStatisticsService>(options => options.Strict());
        _mockTransferLocationService = A.Fake<ITransferLocationService>(options => options.Strict());
        _mockLogger = A.Fake<ILogger<AssertFilePartIsUploadedCommandHandler>>();
        
        _assertFilePartIsUploadedCommandHandler = new AssertFilePartIsUploadedCommandHandler(
            _mockCloudSessionsRepository,
            _mockSharedFilesService,
            _mockSynchronizationRepository,
            _mockTrackingActionRepository,
            _mockSynchronizationStatusCheckerService,
            _mockSynchronizationProgressService,
            _mockInvokeClientsService,
            _mockUsageStatisticsService,
            _mockTransferLocationService,
            _mockLogger);
    }

    [Test]
    public async Task Handle_ValidRequest_AssertsFilePartIsUploaded()
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };
        var transferParameters = new TransferParameters
        {
            SessionId = sessionId,
            SharedFileDefinition = new SharedFileDefinition { Id = "file1", SessionId = sessionId, ActionsGroupIds = new List<string> { "ActionGroupId" } },
            PartNumber = 1
        };

        var request = new AssertFilePartIsUploadedRequest(sessionId, client, transferParameters);
        
        
        // Mock the session repository to return a valid session and session member
        var mockSession = new CloudSessionData();
        var mockSessionMember = new SessionMemberData { ClientInstanceId = client.ClientInstanceId };
        mockSession.SessionMembers.Add(mockSessionMember);
        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId)).Returns(mockSession);
        
        // Mock the transfer location service to return true for IsSharedFileDefinitionAllowed
        A.CallTo(() => _mockTransferLocationService.IsSharedFileDefinitionAllowed(mockSessionMember, transferParameters.SharedFileDefinition))
            .Returns(true);

        // Mock the usage statistics service
        A.CallTo(() => _mockUsageStatisticsService.RegisterUploadUsage(client, transferParameters.SharedFileDefinition, transferParameters.PartNumber!.Value))
            .Returns(Task.CompletedTask);

        // Arrange synchronization branch expectations
        var synchronizationEntity = new SynchronizationEntity();
        A.CallTo(() => _mockSynchronizationRepository.Get(sessionId)).Returns(synchronizationEntity);
        A.CallTo(() => _mockSynchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronizationEntity))
            .Returns(true);
        var trackingAction = new TrackingActionEntity();
        trackingAction.TargetClientInstanceAndNodeIds.Add("clientA_node");
        A.CallTo(() => _mockTrackingActionRepository.GetOrThrow(sessionId, A<string>._)).Returns(trackingAction);
        A.CallTo(() => _mockSharedFilesService.AssertFilePartIsUploaded(A<TransferParameters>._, A<ICollection<string>>.Ignored))
            .Returns(Task.CompletedTask);
        var hubPush = A.Fake<IHubByteSyncPush>();
        A.CallTo(() => _mockInvokeClientsService.Clients(A<ICollection<string>>.Ignored)).Returns(hubPush);
        A.CallTo(() => hubPush.FilePartUploaded(A<FileTransferPush>._)).Returns(Task.CompletedTask);

        // Act
        await _assertFilePartIsUploadedCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockUsageStatisticsService.RegisterUploadUsage(client, transferParameters.SharedFileDefinition, transferParameters.PartNumber!.Value))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockSharedFilesService.AssertFilePartIsUploaded(A<TransferParameters>._, A<ICollection<string>>.Ignored))
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
            PartNumber = 1
        };
        var expectedException = new InvalidOperationException("Test exception");

        var request = new AssertFilePartIsUploadedRequest(sessionId, client, transferParameters);

        // Mock the session repository to throw an exception
        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId))
            .Throws(expectedException);

        // Act & Assert
        var exception = await FluentActions.Awaiting(() => 
            _assertFilePartIsUploadedCommandHandler.Handle(request, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();

        exception.Which.Should().Be(expectedException);
    }
} 