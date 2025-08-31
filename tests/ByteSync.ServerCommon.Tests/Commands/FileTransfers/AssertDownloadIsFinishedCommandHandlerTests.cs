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
    
} 