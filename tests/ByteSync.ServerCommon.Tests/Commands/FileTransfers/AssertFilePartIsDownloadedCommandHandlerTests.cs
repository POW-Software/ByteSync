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
public class AssertFilePartIsDownloadedCommandHandlerTests
{
    private ICloudSessionsRepository _mockCloudSessionsRepository;
    private ISharedFilesService _mockSharedFilesService;
    private ILogger<AssertFilePartIsDownloadedCommandHandler> _mockLogger;
    private AssertFilePartIsDownloadedCommandHandler _assertFilePartIsDownloadedCommandHandler;
    private ITransferLocationService _mockTransferLocationService;
    
    [SetUp]
    public void Setup()
    {
        _mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        _mockSharedFilesService = A.Fake<ISharedFilesService>();
        _mockLogger = A.Fake<ILogger<AssertFilePartIsDownloadedCommandHandler>>();
        _mockTransferLocationService = A.Fake<ITransferLocationService>();
        
        _assertFilePartIsDownloadedCommandHandler = new AssertFilePartIsDownloadedCommandHandler(
            _mockCloudSessionsRepository,
            _mockSharedFilesService,
            _mockLogger,
            _mockTransferLocationService);
    }

    [Test]
    public async Task Handle_ValidRequest_AssertsFilePartIsDownloaded()
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };
        var sharedFileDefinition = new SharedFileDefinition { Id = "file1" };
        var partNumber = 1;

        var request = new AssertFilePartIsDownloadedRequest(sessionId, client, sharedFileDefinition, partNumber);

        // Mock the session repository to return a valid session member
        var mockSessionMember = new SessionMemberData { ClientInstanceId = client.ClientInstanceId };
        A.CallTo(() => _mockCloudSessionsRepository.GetSessionMember(sessionId, client)).Returns(mockSessionMember);
        
        // Mock the transfer location service to return true for IsSharedFileDefinitionAllowed
        A.CallTo(() => _mockTransferLocationService.IsSharedFileDefinitionAllowed(mockSessionMember, sharedFileDefinition))
            .Returns(true);

        // Mock the shared files service
        A.CallTo(() => _mockSharedFilesService.AssertFilePartIsDownloaded(sharedFileDefinition, client, partNumber))
            .Returns(Task.CompletedTask);

        // Act
        await _assertFilePartIsDownloadedCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockCloudSessionsRepository.GetSessionMember(sessionId, client))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockTransferLocationService.IsSharedFileDefinitionAllowed(mockSessionMember, sharedFileDefinition))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_WhenServiceThrowsException_PropagatesException()
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };
        var sharedFileDefinition = new SharedFileDefinition { Id = "file1" };
        var partNumber = 1;
        var expectedException = new InvalidOperationException("Test exception");

        var request = new AssertFilePartIsDownloadedRequest(sessionId, client, sharedFileDefinition, partNumber);

        // Mock the session repository to throw an exception
        A.CallTo(() => _mockCloudSessionsRepository.GetSessionMember(sessionId, client))
            .Throws(expectedException);

        // Act & Assert
        var exception = await FluentActions.Awaiting(() => 
            _assertFilePartIsDownloadedCommandHandler.Handle(request, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();

        exception.Which.Should().Be(expectedException);
    }
    
} 