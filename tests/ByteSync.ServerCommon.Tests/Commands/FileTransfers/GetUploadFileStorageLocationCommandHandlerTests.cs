using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Commands.FileTransfers;
using ByteSync.ServerCommon.Interfaces.Services;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Tests.Commands.FileTransfers;

[TestFixture]
public class GetUploadFileStorageLocationCommandHandlerTests
{
    private ITransferLocationService _mockTransferLocationService;
    private ILogger<GetUploadFileStorageLocationCommandHandler> _mockLogger;
    private GetUploadFileStorageLocationCommandHandler _getUploadFileStorageLocationCommandHandler;

    [SetUp]
    public void Setup()
    {
        _mockTransferLocationService = A.Fake<ITransferLocationService>();
        _mockLogger = A.Fake<ILogger<GetUploadFileStorageLocationCommandHandler>>();

        var mockAppSettings = A.Fake<IOptions<AppSettings>>();
        var appSettings = new AppSettings { DefaultStorageProvider = StorageProvider.AzureBlobStorage };
        A.CallTo(() => mockAppSettings.Value).Returns(appSettings);

        _getUploadFileStorageLocationCommandHandler = new GetUploadFileStorageLocationCommandHandler(
            _mockTransferLocationService,
            mockAppSettings,
            _mockLogger);
    }

    [Test]
    public async Task Handle_ValidRequest_ReturnsStorageLocation()
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
        var expectedStorageLocation = new FileStorageLocation("https://example.com/upload-url", StorageProvider.AzureBlobStorage);

        var request = new GetUploadFileStorageLocationRequest(sessionId, client, transferParameters);

        A.CallTo(() => _mockTransferLocationService.GetUploadFileUrl(sessionId, client, transferParameters))
            .Returns("https://example.com/upload-url");

        // Act
        var result = await _getUploadFileStorageLocationCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().Be(expectedStorageLocation);
        A.CallTo(() => _mockTransferLocationService.GetUploadFileUrl(sessionId, client, transferParameters))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_WithDifferentTransferParameters_ReturnsOtherStorageLocation()
    {
        // Arrange
        var sessionId = "session2";
        var client = new Client { ClientInstanceId = "client2" };
        var transferParameters = new TransferParameters
        {
            SessionId = sessionId,
            SharedFileDefinition = new SharedFileDefinition { Id = "file2" },
            PartNumber = 3
        };
        var expectedStorageLocation = new FileStorageLocation("https://example.com/download-url", StorageProvider.CloudflareR2);

        var request = new GetUploadFileStorageLocationRequest(sessionId, client, transferParameters);

        A.CallTo(() => _mockTransferLocationService.GetUploadFileUrl(sessionId, client, transferParameters))
            .Returns("https://example.com/download-url");

        // Act
        var result = await _getUploadFileStorageLocationCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBe(expectedStorageLocation);
        A.CallTo(() => _mockTransferLocationService.GetUploadFileUrl(sessionId, client, transferParameters))
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

        var request = new GetUploadFileStorageLocationRequest(sessionId, client, transferParameters);

        A.CallTo(() => _mockTransferLocationService.GetUploadFileUrl(sessionId, client, transferParameters))
            .Throws(expectedException);

        // Act & Assert
        var exception = await FluentActions.Awaiting(() => 
            _getUploadFileStorageLocationCommandHandler.Handle(request, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();

        exception.Which.Should().Be(expectedException);
    }
} 