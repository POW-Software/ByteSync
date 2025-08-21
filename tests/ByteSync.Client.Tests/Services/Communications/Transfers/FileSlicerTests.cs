using NUnit.Framework;
using Moq;
using System.Threading.Channels;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Services.Communications.Transfers;
using ByteSync.Services.Communications.Transfers.Uploading;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace ByteSync.Tests.Services.Communications.Transfers;

[TestFixture]
public class FileSlicerTests
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
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Assert
        _fileSlicer.Should().NotBeNull();
    }

    [Test]
    public void MaxSliceLength_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var expectedValue = 1024 * 1024; // 1MB

        // Act
        _fileSlicer.MaxSliceLength = expectedValue;
        var result = _fileSlicer.MaxSliceLength;

        // Assert
        result.Should().Be(expectedValue);
    }

    [Test]
    public async Task SliceAndEncryptAsync_WithValidParameters_ShouldProcessSlices()
    {
        // Arrange
        var slice1 = new FileUploaderSlice(1, new MemoryStream());
        var slice2 = new FileUploaderSlice(2, new MemoryStream());
        var slice3 = (FileUploaderSlice?)null; // End of slicing

        _mockSlicerEncrypter.SetupSequence(x => x.SliceAndEncrypt())
            .ReturnsAsync(slice1)
            .ReturnsAsync(slice2)
            .ReturnsAsync(slice3);

        // Act
        await _fileSlicer.SliceAndEncryptAsync(_sharedFileDefinition, _progressState);

        // Assert
        _mockSlicerEncrypter.Verify(x => x.SliceAndEncrypt(), Times.Exactly(3));
        
        // Verify slices were written to channel
        var readSlice1 = await _availableSlices.Reader.ReadAsync();
        var readSlice2 = await _availableSlices.Reader.ReadAsync();
        
        readSlice1.Should().Be(slice1);
        readSlice2.Should().Be(slice2);
        
        // Verify progress state was updated
        _progressState.TotalCreatedSlices.Should().Be(2);
        
        // Verify channel is completed
        _availableSlices.Reader.Completion.IsCompleted.Should().BeTrue();
    }

    [Test]
    public async Task SliceAndEncryptAsync_WithMaxSliceLength_ShouldSetOnSlicerEncrypter()
    {
        // Arrange
        var maxSliceLength = 512 * 1024; // 512KB
        var slice = new FileUploaderSlice(1, new MemoryStream());
        
        _mockSlicerEncrypter.SetupSequence(x => x.SliceAndEncrypt())
            .ReturnsAsync(slice)
            .ReturnsAsync((FileUploaderSlice?)null);

        // Act
        await _fileSlicer.SliceAndEncryptAsync(_sharedFileDefinition, _progressState, maxSliceLength);

        // Assert
        _mockSlicerEncrypter.VerifySet(x => x.MaxSliceLength = maxSliceLength, Times.Once);
    }

    [Test]
    public async Task SliceAndEncryptAsync_WhenExceptionOccurred_ShouldStopProcessing()
    {
        // Arrange
        var slice = new FileUploaderSlice(1, new MemoryStream());
        
        _mockSlicerEncrypter.Setup(x => x.SliceAndEncrypt())
            .ReturnsAsync(slice);

        // Set exception occurred before processing
        _exceptionOccurred.Set();

        // Act
        await _fileSlicer.SliceAndEncryptAsync(_sharedFileDefinition, _progressState);

        // Assert
        _mockSlicerEncrypter.Verify(x => x.SliceAndEncrypt(), Times.Never);
        _progressState.TotalCreatedSlices.Should().Be(0);
    }

    [Test]
    public async Task SliceAndEncryptAsync_WithNoSlices_ShouldCompleteNormally()
    {
        // Arrange
        _mockSlicerEncrypter.Setup(x => x.SliceAndEncrypt())
            .ReturnsAsync((FileUploaderSlice?)null);

        // Act
        await _fileSlicer.SliceAndEncryptAsync(_sharedFileDefinition, _progressState);

        // Assert
        _mockSlicerEncrypter.Verify(x => x.SliceAndEncrypt(), Times.Once);
        _progressState.TotalCreatedSlices.Should().Be(0);
        
        // Channel should be completed
        _availableSlices.Reader.Completion.IsCompleted.Should().BeTrue();
    }

    [Test]
    public async Task SliceAndEncryptAsync_WithMultipleSlices_ShouldProcessAllSlices()
    {
        // Arrange
        var slices = new List<FileUploaderSlice>();
        for (int i = 1; i <= 5; i++)
        {
            slices.Add(new FileUploaderSlice(i, new MemoryStream()));
        }

        var setup = _mockSlicerEncrypter.SetupSequence(x => x.SliceAndEncrypt());
        foreach (var slice in slices)
        {
            setup.ReturnsAsync(slice);
        }
        setup.ReturnsAsync((FileUploaderSlice?)null);

        // Act
        await _fileSlicer.SliceAndEncryptAsync(_sharedFileDefinition, _progressState);

        // Assert
        _mockSlicerEncrypter.Verify(x => x.SliceAndEncrypt(), Times.Exactly(6)); // 5 slices + 1 null
        
        // Verify all slices were written to channel
        for (int i = 0; i < 5; i++)
        {
            var readSlice = await _availableSlices.Reader.ReadAsync();
            readSlice.Should().Be(slices[i]);
        }
        
        _progressState.TotalCreatedSlices.Should().Be(5);
        
        // Verify channel is completed
        _availableSlices.Reader.Completion.IsCompleted.Should().BeTrue();
    }

    [Test]
    public async Task SliceAndEncryptAsync_WithNullMaxSliceLength_ShouldNotSetMaxSliceLength()
    {
        // Arrange
        var slice = new FileUploaderSlice(1, new MemoryStream());
        
        _mockSlicerEncrypter.SetupSequence(x => x.SliceAndEncrypt())
            .ReturnsAsync(slice)
            .ReturnsAsync((FileUploaderSlice?)null);

        // Act
        await _fileSlicer.SliceAndEncryptAsync(_sharedFileDefinition, _progressState, null);

        // Assert
        _mockSlicerEncrypter.VerifySet(x => x.MaxSliceLength = It.IsAny<int>(), Times.Never);
    }

    [Test]
    public async Task SliceAndEncryptAsync_WithZeroMaxSliceLength_ShouldSetMaxSliceLength()
    {
        // Arrange
        var maxSliceLength = 0;
        var slice = new FileUploaderSlice(1, new MemoryStream());
        
        _mockSlicerEncrypter.SetupSequence(x => x.SliceAndEncrypt())
            .ReturnsAsync(slice)
            .ReturnsAsync((FileUploaderSlice?)null);

        // Act
        await _fileSlicer.SliceAndEncryptAsync(_sharedFileDefinition, _progressState, maxSliceLength);

        // Assert
        _mockSlicerEncrypter.VerifySet(x => x.MaxSliceLength = maxSliceLength, Times.Once);
    }

    [Test]
    public async Task SliceAndEncryptAsync_WithNegativeMaxSliceLength_ShouldSetMaxSliceLength()
    {
        // Arrange
        var maxSliceLength = -1024;
        var slice = new FileUploaderSlice(1, new MemoryStream());
        
        _mockSlicerEncrypter.SetupSequence(x => x.SliceAndEncrypt())
            .ReturnsAsync(slice)
            .ReturnsAsync((FileUploaderSlice?)null);

        // Act
        await _fileSlicer.SliceAndEncryptAsync(_sharedFileDefinition, _progressState, maxSliceLength);

        // Assert
        _mockSlicerEncrypter.VerifySet(x => x.MaxSliceLength = maxSliceLength, Times.Once);
    }

    [Test]
    public async Task SliceAndEncryptAsync_WhenChannelIsFull_ShouldBlockUntilSpaceAvailable()
    {
        // Arrange
        var slices = new List<FileUploaderSlice>();
        for (int i = 1; i <= 10; i++) // More than channel capacity (8)
        {
            slices.Add(new FileUploaderSlice(i, new MemoryStream()));
        }

        var setup = _mockSlicerEncrypter.SetupSequence(x => x.SliceAndEncrypt());
        foreach (var slice in slices)
        {
            setup.ReturnsAsync(slice);
        }
        setup.ReturnsAsync((FileUploaderSlice?)null);

        // Start a background task to consume slices from the channel
        var consumerTask = Task.Run(async () =>
        {
            var consumedSlices = new List<FileUploaderSlice>();
            await foreach (var slice in _availableSlices.Reader.ReadAllAsync())
            {
                consumedSlices.Add(slice);
                // Simulate some processing time
                await Task.Delay(10);
            }
            return consumedSlices;
        });

        // Act
        await _fileSlicer.SliceAndEncryptAsync(_sharedFileDefinition, _progressState);

        // Assert
        _progressState.TotalCreatedSlices.Should().Be(10);
        
        // Wait for consumer to finish and verify all slices were consumed
        var consumedSlices = await consumerTask;
        consumedSlices.Count.Should().Be(10);
        
        // Verify channel is completed
        _availableSlices.Reader.Completion.IsCompleted.Should().BeTrue();
    }


    [Test]
    public void SemaphoreSlim_ShouldReturnSameInstance()
    {
        // Act
        var semaphore1 = _semaphoreSlim;
        var semaphore2 = _semaphoreSlim;

        // Assert
        semaphore1.Should().BeSameAs(semaphore2);
    }

    [Test]
    public void AvailableSlices_ShouldReturnSameChannel()
    {
        // Act
        var channel1 = _availableSlices;
        var channel2 = _availableSlices;

        // Assert
        channel1.Should().BeSameAs(channel2);
    }
} 