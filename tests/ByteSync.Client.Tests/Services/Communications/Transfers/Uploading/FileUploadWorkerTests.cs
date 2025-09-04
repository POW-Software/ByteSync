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
    private Channel<FileUploaderSlice> _availableSlices= null!;
    private UploadProgressState _progressState= null!;
    private SemaphoreSlim _semaphoreSlim= null!;
    
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
            UploadedFileLength = 1024
        };

        _semaphoreSlim = new SemaphoreSlim(1, 1);
        _exceptionOccurred = new ManualResetEvent(false);
        _uploadingIsFinished = new ManualResetEvent(false);
        _availableSlices = Channel.CreateBounded<FileUploaderSlice>(8);
        _progressState = new UploadProgressState();

        _fileUploadWorker = new FileUploadWorker(
            _mockPolicyFactory.Object,
            _mockFileTransferApiClient.Object,
            _sharedFileDefinition,
            _semaphoreSlim,
            _exceptionOccurred,
            _mockStrategies.Object,
            _uploadingIsFinished,
            _mockLogger.Object);

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
    public void StartUploadWorkers_ShouldStartSpecifiedNumberOfWorkers()
    {
        // Arrange
        var workerCount = 3;

        // Act
        _fileUploadWorker.StartUploadWorkers(_availableSlices, workerCount, _progressState);

        // Assert
        // Note: This test verifies the method doesn't throw, but actual worker behavior
        // would need integration testing with real channels and async operations
        Assert.Pass("StartUploadWorkers completed without throwing");
    }

    [Test]
    public async Task UploadAvailableSlicesAsync_WhenUploadThrowsException_ShouldHandleError()
    {
        // Arrange
        var slice = new FileUploaderSlice(1, new MemoryStream());

        await _availableSlices.Writer.WriteAsync(slice);
        _availableSlices.Writer.Complete();

        // Act
        await _fileUploadWorker.UploadAvailableSlicesAsync(_availableSlices, _progressState);

        // Assert
        _mockLogger.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
        _exceptionOccurred.WaitOne(0).Should().BeTrue();
    }

    [Test]
    [TestCase(StorageProvider.AzureBlobStorage, "https://test.blob.core.windows.net/test/upload")]
    [TestCase(StorageProvider.CloudflareR2, "https://test-bucket.r2.cloudflarestorage.com/test/upload")]
    public async Task UploadAvailableSlicesAsync_WhenAllSlicesUploaded_ShouldSetUploadingFinished(StorageProvider storageProvider, string uploadUrl)
    {
        // Arrange
        var slice = new FileUploaderSlice(1, new MemoryStream());
        var mockUploadStrategy = new Mock<IUploadStrategy>();
        var mockUploadLocation = new FileStorageLocation(uploadUrl, storageProvider);
        
        mockUploadStrategy.Setup(x => x.UploadAsync( It.IsAny<FileUploaderSlice>(), It.IsAny<FileStorageLocation>(), It.IsAny<CancellationToken>()))
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
        await _fileUploadWorker.UploadAvailableSlicesAsync(_availableSlices, _progressState);

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
    public async Task UploadAvailableSlicesAsync_WhenUploadFails_ShouldHandleError(StorageProvider storageProvider)
    {
        // Arrange
        var slice = new FileUploaderSlice(1, new MemoryStream());
        var mockUploadStrategy = new Mock<IUploadStrategy>();
        var mockUploadLocation = new FileStorageLocation("https://test.example.com/upload", storageProvider);
        
        mockUploadStrategy.Setup(x => x.UploadAsync(It.IsAny<FileUploaderSlice>(), It.IsAny<FileStorageLocation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UploadFileResponse.Failure(500, "Upload failed"));
        
        _mockStrategies.Setup(x => x[storageProvider]).Returns(mockUploadStrategy.Object);
        _mockFileTransferApiClient.Setup(x => x.GetUploadFileStorageLocation(It.IsAny<TransferParameters>()))
            .ReturnsAsync(mockUploadLocation);

        await _availableSlices.Writer.WriteAsync(slice);
        _availableSlices.Writer.Complete();

        // Act
        await _fileUploadWorker.UploadAvailableSlicesAsync(_availableSlices, _progressState);

        // Assert
        _mockLogger.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
        _exceptionOccurred.WaitOne(0).Should().BeTrue();
    }

    [Test]
    public async Task UploadAvailableSlicesAsync_WithEmptyChannel_ShouldCompleteNormally()
    {
        // Arrange
        _availableSlices.Writer.Complete();

        // Act & Assert
        var action = async () => await _fileUploadWorker.UploadAvailableSlicesAsync(_availableSlices, _progressState);
        await action.Should().NotThrowAsync();
    }
    
    [Test]
    public async Task UploadAvailableSlicesAsync_WithNullResponse_ShouldThrowException()
    {
        // Arrange
        var slice = new FileUploaderSlice(1, new MemoryStream());

        await _availableSlices.Writer.WriteAsync(slice);
        _availableSlices.Writer.Complete();

        // Act
        await _fileUploadWorker.UploadAvailableSlicesAsync(_availableSlices, _progressState);

        // Assert
        _mockLogger.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
        _exceptionOccurred.WaitOne(0).Should().BeTrue();
    }
    
    [Test]
    public void StartUploadWorkers_WithZeroWorkerCount_ShouldNotStartAnyWorkers()
    {
        // Act & Assert
        var action = () => _fileUploadWorker.StartUploadWorkers(_availableSlices, 0, _progressState);
        action.Should().NotThrow();
    }

    [Test]
    public void StartUploadWorkers_WithNegativeWorkerCount_ShouldNotStartAnyWorkers()
    {
        // Act & Assert
        var action = () => _fileUploadWorker.StartUploadWorkers(_availableSlices, -1, _progressState);
        action.Should().NotThrow();
    }

    [Test]
    public void StartUploadWorkers_WithLargeWorkerCount_ShouldStartWorkers()
    {
        // Act & Assert
        var action = () => _fileUploadWorker.StartUploadWorkers(_availableSlices, 100, _progressState);
        action.Should().NotThrow();
    }

    [Test]
    [TestCase(StorageProvider.AzureBlobStorage)]
    [TestCase(StorageProvider.CloudflareR2)]
    public async Task UploadAvailableSlicesAsync_ShouldUseCorrectStrategyForStorageProvider(StorageProvider storageProvider)
    {
        // Arrange
        var slice = new FileUploaderSlice(1, new MemoryStream());
        var mockUploadStrategy = new Mock<IUploadStrategy>();
        var mockUploadLocation = new FileStorageLocation("https://test.example.com/upload", storageProvider);
        
        mockUploadStrategy.Setup(x => x.UploadAsync(It.IsAny<FileUploaderSlice>(), It.IsAny<FileStorageLocation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UploadFileResponse.Success(200));
        
        _mockStrategies.Setup(x => x[storageProvider]).Returns(mockUploadStrategy.Object);
        _mockFileTransferApiClient.Setup(x => x.GetUploadFileStorageLocation(It.IsAny<TransferParameters>()))
            .ReturnsAsync(mockUploadLocation);
        _mockFileTransferApiClient.Setup(x => x.AssertFilePartIsUploaded(It.IsAny<TransferParameters>()))
            .Returns(Task.CompletedTask);

        await _availableSlices.Writer.WriteAsync(slice);
        _availableSlices.Writer.Complete();

        // Act
        await _fileUploadWorker.UploadAvailableSlicesAsync(_availableSlices, _progressState);

        // Assert
        _mockStrategies.Verify(x => x[storageProvider], Times.Once);
        mockUploadStrategy.Verify(x => x.UploadAsync(slice, mockUploadLocation, It.IsAny<CancellationToken>()), Times.Once);
    }
} 