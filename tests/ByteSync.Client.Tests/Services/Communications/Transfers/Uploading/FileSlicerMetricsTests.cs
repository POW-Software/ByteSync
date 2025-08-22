using System.Threading.Channels;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Services.Communications.Transfers.Uploading;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Communications.Transfers.Uploading;

[TestFixture]
public class FileSlicerMetricsTests
{
    private Mock<ISlicerEncrypter> _mockSlicerEncrypter;
    private Mock<ILogger<FileSlicer>> _mockLogger;
    private Channel<FileUploaderSlice> _availableSlices;
    private SemaphoreSlim _semaphoreSlim;
    private ManualResetEvent _exceptionOccurred;
    private FileSlicer _fileSlicer;
    private SharedFileDefinition _sharedFileDefinition;
    private UploadProgressState _progressState;

    [SetUp]
    public void SetUp()
    {
        _mockSlicerEncrypter = new Mock<ISlicerEncrypter>();
        _mockLogger = new Mock<ILogger<FileSlicer>>();
        _availableSlices = Channel.CreateBounded<FileUploaderSlice>(8);
        _semaphoreSlim = new SemaphoreSlim(1, 1);
        _exceptionOccurred = new ManualResetEvent(false);

        _fileSlicer = new FileSlicer(
            _mockSlicerEncrypter.Object,
            _availableSlices,
            _semaphoreSlim,
            _exceptionOccurred,
            _mockLogger.Object);

        _sharedFileDefinition = new SharedFileDefinition
        {
            Id = "test-file-id",
            SessionId = "test-session-id",
            UploadedFileLength = 1024
        };

        _progressState = new UploadProgressState();
    }

    [TearDown]
    public void TearDown()
    {
        _exceptionOccurred?.Dispose();
        _semaphoreSlim?.Dispose();
    }

    [Test]
    public async Task SliceAndEncryptAsync_ShouldAccumulateCreatedBytes()
    {
        var slice1 = new FileUploaderSlice(1, new MemoryStream(new byte[100]));
        var slice2 = new FileUploaderSlice(2, new MemoryStream(new byte[200]));

        _mockSlicerEncrypter.SetupSequence(x => x.SliceAndEncrypt())
            .ReturnsAsync(slice1)
            .ReturnsAsync(slice2)
            .ReturnsAsync((FileUploaderSlice?)null);

        await _fileSlicer.SliceAndEncryptAsync(_sharedFileDefinition, _progressState);

        _progressState.TotalCreatedSlices.Should().Be(2);
        _progressState.TotalCreatedBytes.Should().Be(300);
    }

    [Test]
    public async Task SliceAndEncryptAsync_OnException_ShouldRecordError()
    {
        _mockSlicerEncrypter.Setup(x => x.SliceAndEncrypt()).ThrowsAsync(new Exception("boom"));

        await _fileSlicer.SliceAndEncryptAsync(_sharedFileDefinition, _progressState);

        _progressState.LastException.Should().NotBeNull();
        _progressState.Exceptions.Count.Should().Be(1);
    }
}


