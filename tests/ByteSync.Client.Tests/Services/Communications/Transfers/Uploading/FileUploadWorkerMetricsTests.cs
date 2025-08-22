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
    private Mock<IPolicyFactory> _mockPolicyFactory;
    private Mock<IFileTransferApiClient> _mockFileTransferApiClient;
    private Mock<ILogger<FileUploadWorker>> _mockLogger;
    private Mock<IIndex<StorageProvider, IUploadStrategy>> _mockStrategies;
    private AsyncRetryPolicy<UploadFileResponse> _policy;
    private SharedFileDefinition _sharedFileDefinition;
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
        _exceptionOccurred?.Dispose();
        _uploadingIsFinished?.Dispose();
        _semaphoreSlim?.Dispose();
    }

    [Test]
    public async Task UploadAvailableSlicesAsync_ShouldTrackBytesAndConcurrency()
    {
        var slice = new FileUploaderSlice(1, new MemoryStream(new byte[256]));
        var storageProvider = StorageProvider.AzureBlobStorage;
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

        await _fileUploadWorker.UploadAvailableSlicesAsync(_availableSlices, _progressState);

        _progressState.TotalUploadedSlices.Should().Be(1);
        _progressState.TotalUploadedBytes.Should().Be(256);
        _progressState.MaxConcurrentUploads.Should().BeGreaterThanOrEqualTo(1);
        _progressState.LastSliceUploadedBytes.Should().Be(256);
        _progressState.LastSliceUploadDurationMs.Should().BeGreaterThan(0);
        _progressState.SliceMetrics.Should().HaveCount(1);
        var metric = _progressState.SliceMetrics[0];
        metric.PartNumber.Should().Be(1);
        metric.Bytes.Should().Be(256);
        metric.DurationMs.Should().BeGreaterThan(0);
        metric.BandwidthKbps.Should().BeGreaterThanOrEqualTo(0);

        _mockLogger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Slice") && v.ToString()!.Contains("bytes") && v.ToString()!.Contains("kbps")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Test]
    public async Task UploadAvailableSlicesAsync_OnError_ShouldAccumulateExceptions()
    {
        var slice = new FileUploaderSlice(1, new MemoryStream(new byte[128]));
        _mockFileTransferApiClient.Setup(x => x.GetUploadFileStorageLocation(It.IsAny<TransferParameters>()))
            .ThrowsAsync(new Exception("net error"));

        await _availableSlices.Writer.WriteAsync(slice);
        _availableSlices.Writer.Complete();

        await _fileUploadWorker.UploadAvailableSlicesAsync(_availableSlices, _progressState);

        _progressState.LastException.Should().NotBeNull();
        _progressState.Exceptions.Should().NotBeNull();
        _progressState.Exceptions.Count.Should().Be(1);
    }
}


