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

namespace ByteSync.Client.UnitTests.Services.Communications.Transfers.Uploading;

[TestFixture]
public class FileUploadWorkerTests
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
    private Mock<IAdaptiveUploadController> _mockAdaptiveController = null!;
    
    [SetUp]
    public void SetUp()
    {
        _mockPolicyFactory = new Mock<IPolicyFactory>();
        _mockFileTransferApiClient = new Mock<IFileTransferApiClient>();
        _mockLogger = new Mock<ILogger<FileUploadWorker>>();
        _mockStrategies = new Mock<IIndex<StorageProvider, IUploadStrategy>>();
        
        // Create a test policy that returns a mock response
        _policy = Policy<UploadFileResponse>
            .HandleResult(x => !x.IsSuccess)
            .Or<Exception>()
            .RetryAsync(0, onRetry: (_, _, _) => { });
        
        _sharedFileDefinition = new SharedFileDefinition
        {
            Id = "test-file-id",
            SessionId = "test-session-id",
            UploadedFileLength = 1024,
            SharedFileType = SharedFileTypes.FullSynchronization
        };
        
        _semaphoreSlim = new SemaphoreSlim(1, 1);
        _exceptionOccurred = new ManualResetEvent(false);
        _uploadingIsFinished = new ManualResetEvent(false);
        _availableSlices = Channel.CreateBounded<FileUploaderSlice>(8);
        _progressState = new UploadProgressState();
        _mockAdaptiveController = new Mock<IAdaptiveUploadController>();
        _mockAdaptiveController.Setup(x => x.CurrentChunkSizeBytes).Returns(500 * 1024);
        _mockAdaptiveController.Setup(x => x.CurrentParallelism).Returns(2);
        
        _fileUploadWorker = new FileUploadWorker(
            _mockPolicyFactory.Object,
            _mockFileTransferApiClient.Object,
            _sharedFileDefinition,
            _semaphoreSlim,
            _exceptionOccurred,
            _mockStrategies.Object,
            _uploadingIsFinished,
            _mockLogger.Object,
            _mockAdaptiveController.Object);
        
        _mockPolicyFactory.Setup(x => x.BuildFileUploadPolicy()).Returns(_policy);
    }
    
    [TearDown]
    public void TearDown()
    {
        _exceptionOccurred.Dispose();
        _uploadingIsFinished.Dispose();
    }
    
    [Test]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Assert
        _fileUploadWorker.Should().NotBeNull();
    }
    
    
    [Test]
    public async Task UploadAvailableSlicesAsync_WhenUploadThrowsException_ShouldHandleError()
    {
        // Arrange
        var slice = new FileUploaderSlice(1, new MemoryStream());
        
        await _availableSlices.Writer.WriteAsync(slice);
        _availableSlices.Writer.Complete();
        
        // Act
        await _fileUploadWorker.UploadAvailableSlicesAdaptiveAsync(_availableSlices, _progressState);
        
        // Assert
        _mockLogger.Verify(
            x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
        _exceptionOccurred.WaitOne(0).Should().BeTrue();
    }
    
    [Test]
    [TestCase(StorageProvider.AzureBlobStorage, "https://test.blob.core.windows.net/test/upload")]
    [TestCase(StorageProvider.CloudflareR2, "https://test-bucket.r2.cloudflarestorage.com/test/upload")]
    public async Task UploadAvailableSlicesAdaptiveAsync_WhenAllSlicesUploaded_ShouldSetUploadingFinished(StorageProvider storageProvider,
        string uploadUrl)
    {
        // Arrange
        var slice = new FileUploaderSlice(1, new MemoryStream());
        var mockUploadStrategy = new Mock<IUploadStrategy>();
        var mockUploadLocation = new FileStorageLocation(uploadUrl, storageProvider);
        
        mockUploadStrategy.Setup(x =>
                x.UploadAsync(It.IsAny<FileUploaderSlice>(), It.IsAny<FileStorageLocation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UploadFileResponse.Success(200));
        
        _mockStrategies.Setup(x => x[storageProvider]).Returns(mockUploadStrategy.Object);
        _mockFileTransferApiClient.Setup(x => x.GetUploadFileStorageLocation(It.IsAny<TransferParameters>()))
            .ReturnsAsync(mockUploadLocation);
        _mockFileTransferApiClient.Setup(x => x.AssertFilePartIsUploaded(It.IsAny<TransferParameters>()))
            .Returns(Task.CompletedTask);
        
        // Set progress state to indicate all slices created
        _progressState.TotalCreatedSlices = 1;
        
        await _availableSlices.Writer.WriteAsync(slice);
        _availableSlices.Writer.Complete();
        
        // Act
        await _fileUploadWorker.UploadAvailableSlicesAdaptiveAsync(_availableSlices, _progressState);
        
        // Assert
        // The upload should succeed and set uploading finished
        _uploadingIsFinished.WaitOne(1000).Should().BeTrue();
        
        // Verify the correct strategy was called for the storage provider
        _mockStrategies.Verify(x => x[storageProvider], Times.Once);
        mockUploadStrategy.Verify(x => x.UploadAsync(slice, mockUploadLocation, It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Test]
    [TestCase(StorageProvider.AzureBlobStorage)]
    [TestCase(StorageProvider.CloudflareR2)]
    public async Task UploadAvailableSlicesAdaptiveAsync_WhenUploadFails_ShouldHandleError(StorageProvider storageProvider)
    {
        // Arrange
        var slice = new FileUploaderSlice(1, new MemoryStream());
        var mockUploadStrategy = new Mock<IUploadStrategy>();
        var mockUploadLocation = new FileStorageLocation("https://test.example.com/upload", storageProvider);
        
        mockUploadStrategy.Setup(x =>
                x.UploadAsync(It.IsAny<FileUploaderSlice>(), It.IsAny<FileStorageLocation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UploadFileResponse.Failure(500, "Upload failed"));
        
        _mockStrategies.Setup(x => x[storageProvider]).Returns(mockUploadStrategy.Object);
        _mockFileTransferApiClient.Setup(x => x.GetUploadFileStorageLocation(It.IsAny<TransferParameters>()))
            .ReturnsAsync(mockUploadLocation);
        
        await _availableSlices.Writer.WriteAsync(slice);
        _availableSlices.Writer.Complete();
        
        // Act
        await _fileUploadWorker.UploadAvailableSlicesAdaptiveAsync(_availableSlices, _progressState);
        
        // Assert
        _mockLogger.Verify(
            x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
        _exceptionOccurred.WaitOne(0).Should().BeTrue();
    }
    
    [Test]
    public async Task UploadAvailableSlicesAdaptiveAsync_WithEmptyChannel_ShouldCompleteNormally()
    {
        // Arrange
        _availableSlices.Writer.Complete();
        
        // Act & Assert
        var action = async () => await _fileUploadWorker.UploadAvailableSlicesAdaptiveAsync(_availableSlices, _progressState);
        await action.Should().NotThrowAsync();
    }
    
    [Test]
    public async Task UploadAvailableSlicesAdaptiveAsync_WithNullResponse_ShouldThrowException()
    {
        // Arrange
        var slice = new FileUploaderSlice(1, new MemoryStream());
        
        await _availableSlices.Writer.WriteAsync(slice);
        _availableSlices.Writer.Complete();
        
        // Act
        await _fileUploadWorker.UploadAvailableSlicesAdaptiveAsync(_availableSlices, _progressState);
        
        // Assert
        _mockLogger.Verify(
            x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
        _exceptionOccurred.WaitOne(0).Should().BeTrue();
    }
    
    [Test]
    [TestCase(StorageProvider.AzureBlobStorage)]
    [TestCase(StorageProvider.CloudflareR2)]
    public async Task UploadAvailableSlicesAdaptiveAsync_ShouldUseCorrectStrategyForStorageProvider(StorageProvider storageProvider)
    {
        // Arrange
        var slice = new FileUploaderSlice(1, new MemoryStream());
        var mockUploadStrategy = new Mock<IUploadStrategy>();
        var mockUploadLocation = new FileStorageLocation("https://test.example.com/upload", storageProvider);
        
        mockUploadStrategy.Setup(x =>
                x.UploadAsync(It.IsAny<FileUploaderSlice>(), It.IsAny<FileStorageLocation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UploadFileResponse.Success(200));
        
        _mockStrategies.Setup(x => x[storageProvider]).Returns(mockUploadStrategy.Object);
        _mockFileTransferApiClient.Setup(x => x.GetUploadFileStorageLocation(It.IsAny<TransferParameters>()))
            .ReturnsAsync(mockUploadLocation);
        _mockFileTransferApiClient.Setup(x => x.AssertFilePartIsUploaded(It.IsAny<TransferParameters>()))
            .Returns(Task.CompletedTask);
        
        await _availableSlices.Writer.WriteAsync(slice);
        _availableSlices.Writer.Complete();
        
        // Act
        await _fileUploadWorker.UploadAvailableSlicesAdaptiveAsync(_availableSlices, _progressState);
        
        // Assert
        _mockStrategies.Verify(x => x[storageProvider], Times.Once);
        mockUploadStrategy.Verify(x => x.UploadAsync(slice, mockUploadLocation, It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Test]
    public async Task UploadAvailableSlicesAdaptiveAsync_OnSuccess_ShouldRecordResultWithFailureKindNone()
    {
        // Arrange
        var slice = new FileUploaderSlice(1, new MemoryStream());
        var mockUploadStrategy = new Mock<IUploadStrategy>();
        var mockUploadLocation = new FileStorageLocation("https://test.example.com/upload", StorageProvider.CloudflareR2);
        
        mockUploadStrategy.Setup(x =>
                x.UploadAsync(It.IsAny<FileUploaderSlice>(), It.IsAny<FileStorageLocation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UploadFileResponse.Success(200));
        
        _mockStrategies.Setup(x => x[StorageProvider.CloudflareR2]).Returns(mockUploadStrategy.Object);
        _mockFileTransferApiClient.Setup(x => x.GetUploadFileStorageLocation(It.IsAny<TransferParameters>()))
            .ReturnsAsync(mockUploadLocation);
        _mockFileTransferApiClient.Setup(x => x.AssertFilePartIsUploaded(It.IsAny<TransferParameters>()))
            .Returns(Task.CompletedTask);
        _progressState.TotalCreatedSlices = 1;
        
        await _availableSlices.Writer.WriteAsync(slice);
        _availableSlices.Writer.Complete();
        
        // Act
        await _fileUploadWorker.UploadAvailableSlicesAdaptiveAsync(_availableSlices, _progressState);
        
        // Assert
        _mockAdaptiveController.Verify(x => x.RecordUploadResult(It.Is<UploadResult>(result =>
            result.IsSuccess &&
            result.PartNumber == slice.PartNumber &&
            result.StatusCode == 200 &&
            result.Exception == null &&
            result.FileId == _sharedFileDefinition.Id &&
            result.FailureKind == UploadFailureKind.None)), Times.AtLeastOnce);
    }
    
    [Test]
    public async Task UploadAvailableSlicesAdaptiveAsync_OnStrategyClientCancellation_ShouldRecordResultWithClientFailureKind()
    {
        // Arrange
        var slice = new FileUploaderSlice(1, new MemoryStream());
        var mockUploadStrategy = new Mock<IUploadStrategy>();
        var mockUploadLocation = new FileStorageLocation("https://test.example.com/upload", StorageProvider.CloudflareR2);
        
        mockUploadStrategy.Setup(x =>
                x.UploadAsync(It.IsAny<FileUploaderSlice>(), It.IsAny<FileStorageLocation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UploadFileResponse.ClientCancellation(new TaskCanceledException("attempt timed out")));
        
        _mockStrategies.Setup(x => x[StorageProvider.CloudflareR2]).Returns(mockUploadStrategy.Object);
        _mockFileTransferApiClient.Setup(x => x.GetUploadFileStorageLocation(It.IsAny<TransferParameters>()))
            .ReturnsAsync(mockUploadLocation);
        
        await _availableSlices.Writer.WriteAsync(slice);
        _availableSlices.Writer.Complete();
        
        // Act
        await _fileUploadWorker.UploadAvailableSlicesAdaptiveAsync(_availableSlices, _progressState);
        
        // Assert: a client-side failure kind (Cancellation or Timeout) was reported, never ServerError
        _mockAdaptiveController.Verify(x => x.RecordUploadResult(It.Is<UploadResult>(result =>
                !result.IsSuccess &&
                result.PartNumber == slice.PartNumber &&
                (result.FailureKind == UploadFailureKind.ClientCancellation ||
                 result.FailureKind == UploadFailureKind.ClientTimeout))),
            Times.AtLeastOnce);
        
        _mockAdaptiveController.Verify(x => x.RecordUploadResult(It.Is<UploadResult>(result =>
                result.FailureKind == UploadFailureKind.ServerError)),
            Times.Never);
    }
    
    [Test]
    public async Task UploadAvailableSlicesAdaptiveAsync_OnStrategyServerFailure_ShouldRecordResultWithServerErrorKind()
    {
        // Arrange
        var slice = new FileUploaderSlice(1, new MemoryStream());
        var mockUploadStrategy = new Mock<IUploadStrategy>();
        var mockUploadLocation = new FileStorageLocation("https://test.example.com/upload", StorageProvider.CloudflareR2);
        
        mockUploadStrategy.Setup(x =>
                x.UploadAsync(It.IsAny<FileUploaderSlice>(), It.IsAny<FileStorageLocation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UploadFileResponse.Failure(503, "service unavailable"));
        
        _mockStrategies.Setup(x => x[StorageProvider.CloudflareR2]).Returns(mockUploadStrategy.Object);
        _mockFileTransferApiClient.Setup(x => x.GetUploadFileStorageLocation(It.IsAny<TransferParameters>()))
            .ReturnsAsync(mockUploadLocation);
        
        await _availableSlices.Writer.WriteAsync(slice);
        _availableSlices.Writer.Complete();
        
        // Act
        await _fileUploadWorker.UploadAvailableSlicesAdaptiveAsync(_availableSlices, _progressState);
        
        // Assert
        _mockAdaptiveController.Verify(x => x.RecordUploadResult(It.Is<UploadResult>(result =>
            !result.IsSuccess &&
            result.PartNumber == slice.PartNumber &&
            result.StatusCode == 503 &&
            result.FileId == _sharedFileDefinition.Id &&
            result.FailureKind == UploadFailureKind.ServerError)), Times.AtLeastOnce);
    }

    [Test]
    public async Task UploadAvailableSlicesAdaptiveAsync_WhenClientTimeoutIsRetried_ShouldKeepFailureKindAndEventuallySucceed()
    {
        // Arrange
        var slice = new FileUploaderSlice(1, new MemoryStream(new byte[625 * 1024]));
        var mockUploadStrategy = new Mock<IUploadStrategy>();
        var mockUploadLocation = new FileStorageLocation("https://test.example.com/upload", StorageProvider.CloudflareR2);
        var attempt = 0;

        _policy = Policy<UploadFileResponse>
            .HandleResult(x => !x.IsSuccess)
            .RetryAsync(1, onRetry: (_, _, _) => { });
        _mockPolicyFactory.Setup(x => x.BuildFileUploadPolicy()).Returns(_policy);
        _mockAdaptiveController.Setup(x => x.CurrentChunkSizeBytes).Returns(64 * 1024);

        mockUploadStrategy.Setup(x =>
                x.UploadAsync(It.IsAny<FileUploaderSlice>(), It.IsAny<FileStorageLocation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                attempt++;

                return attempt == 1
                    ? UploadFileResponse.ClientTimeout(new TaskCanceledException("attempt timed out"))
                    : UploadFileResponse.Success(200);
            });

        _mockStrategies.Setup(x => x[StorageProvider.CloudflareR2]).Returns(mockUploadStrategy.Object);
        _mockFileTransferApiClient.Setup(x => x.GetUploadFileStorageLocation(It.IsAny<TransferParameters>()))
            .ReturnsAsync(mockUploadLocation);
        _mockFileTransferApiClient.Setup(x => x.AssertFilePartIsUploaded(It.IsAny<TransferParameters>()))
            .Returns(Task.CompletedTask);
        _progressState.TotalCreatedSlices = 1;

        await _availableSlices.Writer.WriteAsync(slice);
        _availableSlices.Writer.Complete();

        // Act
        await _fileUploadWorker.UploadAvailableSlicesAdaptiveAsync(_availableSlices, _progressState);

        // Assert
        attempt.Should().Be(2);
        _uploadingIsFinished.WaitOne(1000).Should().BeTrue();
        _mockAdaptiveController.Verify(x => x.RecordUploadResult(It.Is<UploadResult>(result =>
            !result.IsSuccess &&
            result.FailureKind == UploadFailureKind.ClientTimeout)), Times.Once);
        _mockAdaptiveController.Verify(x => x.RecordUploadResult(It.Is<UploadResult>(result =>
            result.IsSuccess &&
            result.FailureKind == UploadFailureKind.None)), Times.Once);
    }

    [Test]
    public async Task UploadAvailableSlicesAdaptiveAsync_WhenOneWorkerFails_ShouldCancelOtherWorkers()
    {
        // Arrange
        var firstSlice = new FileUploaderSlice(1, new MemoryStream(new byte[64 * 1024]));
        var secondSlice = new FileUploaderSlice(2, new MemoryStream(new byte[64 * 1024]));
        var mockUploadStrategy = new Mock<IUploadStrategy>();
        var mockUploadLocation = new FileStorageLocation("https://test.example.com/upload", StorageProvider.CloudflareR2);
        using var bothUploadsStarted = new CountdownEvent(2);
        using var firstUploadCanFail = new ManualResetEventSlim(false);
        using var secondUploadCanceled = new ManualResetEventSlim(false);

        mockUploadStrategy.Setup(x =>
                x.UploadAsync(It.IsAny<FileUploaderSlice>(), It.IsAny<FileStorageLocation>(), It.IsAny<CancellationToken>()))
            .Returns<FileUploaderSlice, FileStorageLocation, CancellationToken>(async (slice, _, cancellationToken) =>
            {
                bothUploadsStarted.Signal();

                if (slice.PartNumber == 1)
                {
                    firstUploadCanFail.Wait(TimeSpan.FromSeconds(5));

                    return UploadFileResponse.Failure(500, "server failure");
                }

                firstUploadCanFail.Set();

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

                    return UploadFileResponse.Success(200);
                }
                catch (OperationCanceledException ex)
                {
                    secondUploadCanceled.Set();

                    return UploadFileResponse.ClientCancellation(ex);
                }
            });

        _mockStrategies.Setup(x => x[StorageProvider.CloudflareR2]).Returns(mockUploadStrategy.Object);
        _mockFileTransferApiClient.Setup(x => x.GetUploadFileStorageLocation(It.IsAny<TransferParameters>()))
            .ReturnsAsync(mockUploadLocation);
        _progressState.TotalCreatedSlices = 2;

        var firstWorker = _fileUploadWorker.UploadAvailableSlicesAdaptiveAsync(_availableSlices, _progressState);
        var secondWorker = _fileUploadWorker.UploadAvailableSlicesAdaptiveAsync(_availableSlices, _progressState);

        await _availableSlices.Writer.WriteAsync(firstSlice);
        await _availableSlices.Writer.WriteAsync(secondSlice);
        _availableSlices.Writer.Complete();

        // Act
        bothUploadsStarted.Wait(TimeSpan.FromSeconds(2)).Should().BeTrue();
        var allWorkers = Task.WhenAll(firstWorker, secondWorker);
        var completed = await Task.WhenAny(allWorkers, Task.Delay(TimeSpan.FromSeconds(3)));

        // Assert
        completed.Should().Be(allWorkers);
        secondUploadCanceled.Wait(TimeSpan.FromSeconds(1)).Should().BeTrue();
        _exceptionOccurred.WaitOne(0).Should().BeTrue();
        _progressState.Exceptions.Should().HaveCount(1);
    }
}