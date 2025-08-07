using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.FileTransfers;
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
    private ISynchronizationService _mockSynchronizationService;
    private IInvokeClientsService _mockInvokeClientsService;
    private ILogger<AssertUploadIsFinishedCommandHandler> _mockLogger;
    private AssertUploadIsFinishedCommandHandler _assertUploadIsFinishedCommandHandler;
    private ITransferLocationService _mockTransferLocationService = A.Fake<ITransferLocationService>();
    
    [SetUp]
    public void Setup()
    {
        _mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        _mockSharedFilesService = A.Fake<ISharedFilesService>();
        _mockSynchronizationService = A.Fake<ISynchronizationService>();
        _mockInvokeClientsService = A.Fake<IInvokeClientsService>();
        _mockLogger = A.Fake<ILogger<AssertUploadIsFinishedCommandHandler>>();
        _mockTransferLocationService = A.Fake<ITransferLocationService>();
        
        _assertUploadIsFinishedCommandHandler = new AssertUploadIsFinishedCommandHandler(
            _mockCloudSessionsRepository,
            _mockSharedFilesService,
            _mockSynchronizationService,
            _mockInvokeClientsService,
            _mockLogger,
            _mockTransferLocationService);
    }

    [Test]
    public async Task Handle_ValidRequest_AssertsUploadIsFinished()
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

        var request = new AssertUploadIsFinishedRequest(sessionId, client, transferParameters);

        // Mock the session repository to return a valid session
        var mockSession = new CloudSessionData();
        var mockSessionMember = new SessionMemberData { ClientInstanceId = client.ClientInstanceId };
        mockSession.SessionMembers.Add(mockSessionMember);
        
        // Add the shared file definition's client to the session members so IsSharedFileDefinitionAllowed returns true
        var fileOwnerMember = new SessionMemberData { ClientInstanceId = transferParameters.SharedFileDefinition.ClientInstanceId };
        mockSession.SessionMembers.Add(fileOwnerMember);
        
        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId)).Returns(mockSession);
        
        // Mock the transfer location service to return true for IsSharedFileDefinitionAllowed
        A.CallTo(() => _mockTransferLocationService.IsSharedFileDefinitionAllowed(mockSessionMember, transferParameters.SharedFileDefinition))
            .Returns(true);

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

} 