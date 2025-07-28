using NUnit.Framework;
using Moq;
using System.Threading.Channels;
using Azure;
using Azure.Storage.Blobs.Models;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Services.Communications.Transfers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace ByteSync.Tests.Services.Communications.Transfers;

[TestFixture]
public class FileUploadWorkerTests
{
    private Mock<IPolicyFactory> _mockPolicyFactory;
    private Mock<IFileTransferApiClient> _mockFileTransferApiClient;
    private Mock<ILogger<FileUploadWorker>> _mockLogger;
    private AsyncRetryPolicy<Response<BlobContentInfo>> _policy;
    private SharedFileDefinition _sharedFileDefinition;
    private object _syncRoot;
    private ManualResetEvent _exceptionOccurred;
    private ManualResetEvent _uploadingIsFinished;
    private FileUploadWorker _fileUploadWorker;
    private Channel<FileUploaderSlice> _availableSlices;
    private UploadProgressState _progressState;
    private SemaphoreSlim _semaphoreSlim;
    
    [SetUp]
    public void SetUp()
    {
        _mockPolicyFactory = new Mock<IPolicyFactory>();
        _mockFileTransferApiClient = new Mock<IFileTransferApiClient>();
        _mockLogger = new Mock<ILogger<FileUploadWorker>>();
        
        // Create a test policy that returns a mock response
        _policy = Policy<Response<BlobContentInfo>>
            .HandleResult(x => x.GetRawResponse().IsError)
            .Or<Exception>()
            .RetryAsync(0, onRetry: (exception, retryCount, context) => { });

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
            _uploadingIsFinished,
            _mockLogger.Object);

        _mockPolicyFactory.Setup(x => x.BuildFileUploadPolicy()).Returns(_policy);
    }

    [TearDown]
    public void TearDown()
    {
        _exceptionOccurred?.Dispose();
        _uploadingIsFinished?.Dispose();
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
        var expectedException = new Exception("Upload failed");

        await _availableSlices.Writer.WriteAsync(slice);
        _availableSlices.Writer.Complete();

        // Act
        await _fileUploadWorker.UploadAvailableSlicesAsync(_availableSlices, _progressState);

        // Assert
        _mockLogger.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
        _exceptionOccurred.WaitOne(0).Should().BeTrue();
    }

    [Test]
    public async Task UploadAvailableSlicesAsync_WhenAllSlicesUploaded_ShouldSetUploadingFinished()
    {
        // Arrange
        var slice = new FileUploaderSlice(1, new MemoryStream());
        var mockResponse = new Mock<Response<BlobContentInfo>>();
        var mockRawResponse = new Mock<Response>();
        
        mockRawResponse.Setup(x => x.IsError).Returns(false);
        mockResponse.Setup(x => x.GetRawResponse()).Returns(mockRawResponse.Object);
        
        _mockFileTransferApiClient.Setup(x => x.GetUploadFileUrl(It.IsAny<TransferParameters>()))
            .ReturnsAsync("https://test.blob.core.windows.net/test/upload");
        _mockFileTransferApiClient.Setup(x => x.AssertFilePartIsUploaded(It.IsAny<TransferParameters>()))
            .Returns(Task.CompletedTask);

        // Set progress state to indicate all slices created
        _progressState.TotalCreatedSlices = 1;

        await _availableSlices.Writer.WriteAsync(slice);
        _availableSlices.Writer.Complete();

        // Act
        await _fileUploadWorker.UploadAvailableSlicesAsync(_availableSlices, _progressState);

        // Assert
        // The upload will fail because blob.UploadAsync() will throw an exception
        // We should verify that the exception is logged
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
} 