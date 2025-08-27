using System.Threading.Channels;
using Autofac.Features.Indexed;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Services.Communications.Transfers.Uploading;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Polly;
using Polly.Retry;

namespace ByteSync.Tests.Services.Communications.Transfers.Uploading;

[TestFixture]
public class FileUploadWorkerMetricsTests
{
    private Mock<IPolicyFactory> _mockPolicyFactory = null!;
    private Mock<IFileTransferApiClient> _mockFileTransferApiClient = null!;
    private Mock<ILogger<FileUploadWorker>> _mockLogger = null!;
    private Mock<IIndex<StorageProvider, IUploadStrategy>> _mockStrategies = null!;
    private AsyncRetryPolicy<UploadFileResponse> _policy = null!;
    private SharedFileDefinition _sharedFileDefinition = null!;
    private ManualResetEvent _exceptionOccurred = null!;
    private ManualResetEvent _uploadingIsFinished = null!;
    private FileUploadWorker _fileUploadWorker = null!;
    private Channel<FileUploaderSlice> _availableSlices = null!;
    private UploadProgressState _progressState = null!;
    private SemaphoreSlim _semaphoreSlim = null!;
    private Mock<IAdaptiveUploadController> _mockAdaptiveUploadController = null!;

    [SetUp]
    public void SetUp()
    {
        _mockPolicyFactory = new Mock<IPolicyFactory>();
        _mockFileTransferApiClient = new Mock<IFileTransferApiClient>();
        _mockLogger = new Mock<ILogger<FileUploadWorker>>();
        _mockStrategies = new Mock<IIndex<StorageProvider, IUploadStrategy>>();

        _policy = Policy<UploadFileResponse>
            .HandleResult(x => !x.IsSuccess)
            .Or<Exception>()
            .RetryAsync(0);

        _sharedFileDefinition = new SharedFileDefinition
        {
            Id = "test-file-id",
            SessionId = "test-session-id",
            UploadedFileLength = 1024
        };

        _semaphoreSlim = new SemaphoreSlim(1, 1);
        _exceptionOccurred = new ManualResetEvent(false);
        _uploadingIsFinished = new ManualResetEvent(false);
        _availableSlices = Channel.CreateBounded<FileUploaderSlice>(8);
        _progressState = new UploadProgressState();
        _mockAdaptiveUploadController = new Mock<IAdaptiveUploadController>();

        _fileUploadWorker = new FileUploadWorker(
            _mockPolicyFactory.Object,
            _mockFileTransferApiClient.Object,
            _sharedFileDefinition,
            _semaphoreSlim,
            _exceptionOccurred,
            _mockStrategies.Object,
            _uploadingIsFinished,
            _mockLogger.Object,
            _mockAdaptiveUploadController.Object);

        _mockPolicyFactory.Setup(x => x.BuildFileUploadPolicy()).Returns(_policy);
    }
    
    [TearDown]
    public void TearDown()
    {
        _exceptionOccurred.Dispose();
        _uploadingIsFinished.Dispose();
        _semaphoreSlim.Dispose();
    }

    [Test]
    public async Task UploadAvailableSlicesAsync_OnError_ShouldAccumulateExceptions()
    {
        // Arrange
        var slice = new FileUploaderSlice(1, new MemoryStream(new byte[128]));
        _mockFileTransferApiClient.Setup(x => x.GetUploadFileStorageLocation(It.IsAny<TransferParameters>()))
            .ThrowsAsync(new Exception("net error"));

        await _availableSlices.Writer.WriteAsync(slice);
        _availableSlices.Writer.Complete();

        // Act
        await _fileUploadWorker.UploadAvailableSlicesAsync(_availableSlices, _progressState);

        // Assert
        _progressState.Exceptions.Should().NotBeNull();
        _progressState.Exceptions.Count.Should().Be(1);
    }
}


