using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Commands.FileTransfers;
using ByteSync.ServerCommon.Interfaces.Services;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Tests.Commands.FileTransfers;

[TestFixture]
public class GetUploadFileUrlCommandHandlerTests
{
    private ITransferLocationService _mockTransferLocationService;
    private ILogger<GetUploadFileUrlCommandHandler> _mockLogger;
    private GetUploadFileUrlCommandHandler _getUploadFileUrlCommandHandler;

    [SetUp]
    public void Setup()
    {
        _mockTransferLocationService = A.Fake<ITransferLocationService>();
        _mockLogger = A.Fake<ILogger<GetUploadFileUrlCommandHandler>>();

        _getUploadFileUrlCommandHandler = new GetUploadFileUrlCommandHandler(
            _mockTransferLocationService,
            _mockLogger);
    }

    [Test]
    public async Task Handle_ValidRequest_ReturnsUploadUrl()
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };
        var sharedFileDefinition = new SharedFileDefinition { Id = "file1" };
        var partNumber = 1;
        var expectedUrl = "https://example.com/upload-url";

        var transferParameters = new TransferParameters
        {
            SessionId = sessionId,
            SharedFileDefinition = sharedFileDefinition,
            PartNumber = partNumber
        };
        var request = new GetUploadFileUrlRequest(sessionId, client, transferParameters);

        A.CallTo(() => _mockTransferLocationService.GetUploadFileUrl(sessionId, client, transferParameters))
            .Returns(expectedUrl);

        // Act
        var result = await _getUploadFileUrlCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().Be(expectedUrl);
        A.CallTo(() => _mockTransferLocationService.GetUploadFileUrl(sessionId, client, transferParameters))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_WithDifferentPartNumber_ReturnsCorrectUrl()
    {
        // Arrange
        var sessionId = "session1";
        var client = new Client { ClientInstanceId = "client1" };
        var sharedFileDefinition = new SharedFileDefinition { Id = "file1" };
        var partNumber = 5;
        var expectedUrl = "https://example.com/upload-url-part5";

        var transferParameters = new TransferParameters
        {
            SessionId = sessionId,
            SharedFileDefinition = sharedFileDefinition,
            PartNumber = partNumber
        };
        var request = new GetUploadFileUrlRequest(sessionId, client, transferParameters);

        A.CallTo(() => _mockTransferLocationService.GetUploadFileUrl(sessionId, client, transferParameters))
            .Returns(expectedUrl);

        // Act
        var result = await _getUploadFileUrlCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().Be(expectedUrl);
        A.CallTo(() => _mockTransferLocationService.GetUploadFileUrl(sessionId, client, transferParameters))
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

        var transferParameters = new TransferParameters
        {
            SessionId = sessionId,
            SharedFileDefinition = sharedFileDefinition,
            PartNumber = partNumber
        };
        var request = new GetUploadFileUrlRequest(sessionId, client, transferParameters);

        A.CallTo(() => _mockTransferLocationService.GetUploadFileUrl(sessionId, client, transferParameters))
            .Throws(expectedException);

        // Act & Assert
        var exception = await FluentActions.Awaiting(() => 
            _getUploadFileUrlCommandHandler.Handle(request, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();

        exception.Which.Should().Be(expectedException);
    }
} 