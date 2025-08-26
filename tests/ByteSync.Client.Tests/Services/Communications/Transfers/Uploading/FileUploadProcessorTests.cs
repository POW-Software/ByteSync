using System.Threading.Channels;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Services.Communications.Transfers.Uploading;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Communications.Transfers.Uploading;

[TestFixture]
public class FileUploadProcessorTests
{
    private Mock<ISlicerEncrypter> _mockSlicerEncrypter = null!;
    private Mock<ILogger> _mockLogger = null!;
    private Mock<IFileUploadCoordinator> _mockFileUploadCoordinator = null!;
    private Mock<IFileSlicer> _mockFileSlicer = null!;
    private Mock<IFileUploadWorker> _mockFileUploadWorker = null!;
    private Mock<IFilePartUploadAsserter> _mockFilePartUploadAsserter = null!;
    private Mock<IAdaptiveUploadController> _mockAdaptiveController = null!;
    private SharedFileDefinition _sharedFileDefinition = null!;
    private string _testFilePath = null!;
    private MemoryStream _testMemoryStream = null!;
    private FileUploadProcessor _fileUploadProcessor = null!;
    private SemaphoreSlim _semaphoreSlim = null!;

    [SetUp]
    public void SetUp()
    {
        _mockSlicerEncrypter = new Mock<ISlicerEncrypter>();
        _mockLogger = new Mock<ILogger>();
        _mockFileUploadCoordinator = new Mock<IFileUploadCoordinator>();
        _mockFileSlicer = new Mock<IFileSlicer>();
        _mockFileUploadWorker = new Mock<IFileUploadWorker>();
        _mockFilePartUploadAsserter = new Mock<IFilePartUploadAsserter>();
        _mockAdaptiveController = new Mock<IAdaptiveUploadController>();

        _sharedFileDefinition = new SharedFileDefinition
        {
            Id = "test-file-id",
            SessionId = "test-session-id",
            UploadedFileLength = 1024
        };

        // Create test file
        _testFilePath = Path.GetTempFileName();
        File.WriteAllText(_testFilePath, "Test file content");

        // Create test memory stream
        _testMemoryStream = new MemoryStream();
        var writer = new StreamWriter(_testMemoryStream);
        writer.Write("Test memory stream content");
        writer.Flush();
        _testMemoryStream.Position = 0;

        _mockFileUploadCoordinator.Setup(x => x.AvailableSlices).Returns(Channel.CreateBounded<FileUploaderSlice>(8));
        _mockFileUploadCoordinator.Setup(x => x.WaitForCompletionAsync()).Returns(Task.CompletedTask);
        _mockFileUploadCoordinator.Setup(x => x.SyncRoot).Returns(new object());

        _semaphoreSlim = new SemaphoreSlim(1, 1);
        _mockAdaptiveController.Setup(x => x.CurrentParallelism).Returns(2);
        
        _fileUploadProcessor = new FileUploadProcessor(
            _mockSlicerEncrypter.Object,
            _mockLogger.Object,
            _mockFileUploadCoordinator.Object,
            _mockFileSlicer.Object,
            _mockFileUploadWorker.Object,
            _mockFilePartUploadAsserter.Object,
            _testFilePath,
            _semaphoreSlim,
            _mockAdaptiveController.Object);
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
        _testMemoryStream.Dispose();
        _semaphoreSlim.Dispose();
    }

    [Test]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var processor = new FileUploadProcessor(
            _mockSlicerEncrypter.Object,
            _mockLogger.Object,
            _mockFileUploadCoordinator.Object,
            _mockFileSlicer.Object,
            _mockFileUploadWorker.Object,
            _mockFilePartUploadAsserter.Object,
            _testFilePath,
            _semaphoreSlim,
            _mockAdaptiveController.Object);

        // Assert
        processor.Should().NotBeNull();
        processor.Should().BeAssignableTo<IFileUploadProcessor>();
    }

    [Test]
    public async Task ProcessUpload_ShouldStartUploadWorkers()
    {
        // Arrange
        _mockFileSlicer.Setup(x => x.SliceAndEncryptAdaptiveAsync(It.IsAny<SharedFileDefinition>(), It.IsAny<UploadProgressState>(), It.IsAny<IAdaptiveUploadController>()))
            .Returns(Task.CompletedTask);
        _mockFilePartUploadAsserter.Setup(x => x.AssertUploadIsFinished(It.IsAny<SharedFileDefinition>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);
        _mockFileUploadCoordinator.Setup(x => x.WaitForCompletionAsync())
            .Returns(Task.Delay(150)); // Small delay to let tasks start

        // Act
        await _fileUploadProcessor.ProcessUpload(_sharedFileDefinition);

        // Assert
        _mockFileUploadWorker.Verify(x => x.UploadAvailableSlicesAdaptiveAsync(It.IsAny<Channel<FileUploaderSlice>>(), It.IsAny<UploadProgressState>(), It.IsAny<IAdaptiveUploadController>()), Times.Exactly(2));
    }

    [Test]
    public async Task ProcessUpload_ShouldStartSlicer()
    {
        // Arrange
        _mockFileSlicer.Setup(x => x.SliceAndEncryptAdaptiveAsync(It.IsAny<SharedFileDefinition>(), It.IsAny<UploadProgressState>(), It.IsAny<IAdaptiveUploadController>()))
            .Returns(Task.CompletedTask);
        _mockFilePartUploadAsserter.Setup(x => x.AssertUploadIsFinished(It.IsAny<SharedFileDefinition>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        // Act
        await _fileUploadProcessor.ProcessUpload(_sharedFileDefinition);

        // Assert
        _mockFileSlicer.Verify(x => x.SliceAndEncryptAdaptiveAsync(_sharedFileDefinition, It.IsAny<UploadProgressState>(), _mockAdaptiveController.Object), Times.Once);
    }

    [Test]
    public async Task ProcessUpload_WithMaxSliceLength_ShouldPassMaxSliceLength()
    {
        // Arrange
        var maxSliceLength = 1024 * 1024; // 1MB
        _mockFileSlicer.Setup(x => x.SliceAndEncryptAdaptiveAsync(It.IsAny<SharedFileDefinition>(), It.IsAny<UploadProgressState>(), It.IsAny<IAdaptiveUploadController>()))
            .Returns(Task.CompletedTask);
        _mockFilePartUploadAsserter.Setup(x => x.AssertUploadIsFinished(It.IsAny<SharedFileDefinition>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        // Act
        await _fileUploadProcessor.ProcessUpload(_sharedFileDefinition, maxSliceLength);

        // Assert: adaptive slicer is used; maxSliceLength is ignored under adaptive mode
        _mockFileSlicer.Verify(x => x.SliceAndEncryptAdaptiveAsync(_sharedFileDefinition, It.IsAny<UploadProgressState>(), It.IsAny<IAdaptiveUploadController>()), Times.Once);
    }

    [Test]
    public async Task ProcessUpload_ShouldWaitForCompletion()
    {
        // Arrange
        _mockFileSlicer.Setup(x => x.SliceAndEncryptAdaptiveAsync(It.IsAny<SharedFileDefinition>(), It.IsAny<UploadProgressState>(), It.IsAny<IAdaptiveUploadController>()))
            .Returns(Task.CompletedTask);
        _mockFilePartUploadAsserter.Setup(x => x.AssertUploadIsFinished(It.IsAny<SharedFileDefinition>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        // Act
        await _fileUploadProcessor.ProcessUpload(_sharedFileDefinition);

        // Assert
        _mockFileUploadCoordinator.Verify(x => x.WaitForCompletionAsync(), Times.Once);
    }

    [Test]
    public async Task ProcessUpload_ShouldDisposeSlicerEncrypter()
    {
        // Arrange
        _mockFileSlicer.Setup(x => x.SliceAndEncryptAdaptiveAsync(It.IsAny<SharedFileDefinition>(), It.IsAny<UploadProgressState>(), It.IsAny<IAdaptiveUploadController>()))
            .Returns(Task.CompletedTask);
        _mockFilePartUploadAsserter.Setup(x => x.AssertUploadIsFinished(It.IsAny<SharedFileDefinition>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        // Act
        await _fileUploadProcessor.ProcessUpload(_sharedFileDefinition);

        // Assert
        _mockSlicerEncrypter.Verify(x => x.Dispose(), Times.Once);
    }

    [Test]
    public async Task ProcessUpload_ShouldAssertUploadIsFinished()
    {
        // Arrange
        _mockFileSlicer.Setup(x => x.SliceAndEncryptAdaptiveAsync(It.IsAny<SharedFileDefinition>(), It.IsAny<UploadProgressState>(), It.IsAny<IAdaptiveUploadController>()))
            .Returns(Task.CompletedTask);
        _mockFilePartUploadAsserter.Setup(x => x.AssertUploadIsFinished(It.IsAny<SharedFileDefinition>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        // Act
        await _fileUploadProcessor.ProcessUpload(_sharedFileDefinition);

        // Assert
        _mockFilePartUploadAsserter.Verify(x => x.AssertUploadIsFinished(_sharedFileDefinition, It.IsAny<int>()), Times.Once);
    }

    [Test]
    public async Task ProcessUpload_WhenExceptionOccurs_ShouldThrowExceptionWithDetails()
    {
        // Arrange
        var expectedException = new Exception("Test exception");
        _mockFileSlicer.Setup(x => x.SliceAndEncryptAdaptiveAsync(It.IsAny<SharedFileDefinition>(), It.IsAny<UploadProgressState>(), It.IsAny<IAdaptiveUploadController>()))
            .Callback<SharedFileDefinition, UploadProgressState, IAdaptiveUploadController>((_, progressState, _) => progressState.Exceptions.Add(expectedException))
            .Returns(Task.CompletedTask);

        // Act & Assert
        var action = async () => await _fileUploadProcessor.ProcessUpload(_sharedFileDefinition);
        await action.Should().ThrowAsync<Exception>()
            .Where(ex => ex.Message.Contains(_testFilePath) && ex.Message.Contains(_sharedFileDefinition.Id) && ex.InnerException == expectedException);
    }

    [Test]
    public async Task ProcessUpload_WhenExceptionOccursWithMemoryStream_ShouldThrowExceptionWithStreamDetails()
    {
        // Arrange
        var processorWithMemoryStream = new FileUploadProcessor(
            _mockSlicerEncrypter.Object,
            _mockLogger.Object,
            _mockFileUploadCoordinator.Object,
            _mockFileSlicer.Object,
            _mockFileUploadWorker.Object,
            _mockFilePartUploadAsserter.Object,
            null,
            _semaphoreSlim,
            _mockAdaptiveController.Object);

        var expectedException = new Exception("Test exception");
        _mockFileSlicer.Setup(x => x.SliceAndEncryptAdaptiveAsync(It.IsAny<SharedFileDefinition>(), It.IsAny<UploadProgressState>(), It.IsAny<IAdaptiveUploadController>()))
            .Callback<SharedFileDefinition, UploadProgressState, IAdaptiveUploadController>((_, progressState, _) => progressState.Exceptions.Add(expectedException))
            .Returns(Task.CompletedTask);

        // Act & Assert
        var action = async () => await processorWithMemoryStream.ProcessUpload(_sharedFileDefinition);
        await action.Should().ThrowAsync<Exception>()
            .Where(ex => ex.Message.Contains("a stream") && ex.Message.Contains(_sharedFileDefinition.Id) && ex.InnerException == expectedException);
    }

    [Test]
    public void GetTotalCreatedSlices_ShouldReturnCorrectValue()
    {
        // Act
        var result = _fileUploadProcessor.GetTotalCreatedSlices();

        // Assert
        result.Should().Be(0); // Should return 0 when no progress state exists
    }

    [Test]
    public void GetMaxConcurrentUploads_ShouldReturnCorrectValue()
    {
        // Act
        var result = _fileUploadProcessor.GetMaxConcurrentUploads();

        // Assert
        result.Should().Be(0); // Should return 0 when no progress state exists
    }

    [Test]
    public async Task GetTotalCreatedSlices_AfterProcessUpload_ShouldReturnCorrectValue()
    {
        // Arrange
        _mockFileSlicer.Setup(x => x.SliceAndEncryptAdaptiveAsync(It.IsAny<SharedFileDefinition>(), It.IsAny<UploadProgressState>(), It.IsAny<IAdaptiveUploadController>()))
            .Returns(Task.CompletedTask);
        _mockFilePartUploadAsserter.Setup(x => x.AssertUploadIsFinished(It.IsAny<SharedFileDefinition>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        // Act
        await _fileUploadProcessor.ProcessUpload(_sharedFileDefinition);
        var result = _fileUploadProcessor.GetTotalCreatedSlices();

        // Assert
        result.Should().Be(0); // Should return 0 when no slices were created
    }

    [Test]
    public async Task GetMaxConcurrentUploads_AfterProcessUpload_ShouldReturnCorrectValue()
    {
        // Arrange
        _mockFileSlicer.Setup(x => x.SliceAndEncryptAsync(It.IsAny<SharedFileDefinition>(), It.IsAny<UploadProgressState>(), It.IsAny<int?>()))
            .Returns(Task.CompletedTask);
        _mockFilePartUploadAsserter.Setup(x => x.AssertUploadIsFinished(It.IsAny<SharedFileDefinition>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        // Act
        await _fileUploadProcessor.ProcessUpload(_sharedFileDefinition);
        var result = _fileUploadProcessor.GetMaxConcurrentUploads();

        // Assert
        result.Should().Be(0); // Should return 0 when no concurrent uploads occurred
    }

    [Test]
    public async Task ProcessUpload_ShouldLogCompletion()
    {
        // Arrange
        _mockFileSlicer.Setup(x => x.SliceAndEncryptAsync(It.IsAny<SharedFileDefinition>(), It.IsAny<UploadProgressState>(), It.IsAny<int?>()))
            .Returns(Task.CompletedTask);
        _mockFilePartUploadAsserter.Setup(x => x.AssertUploadIsFinished(It.IsAny<SharedFileDefinition>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        // Act
        await _fileUploadProcessor.ProcessUpload(_sharedFileDefinition);

        // Assert
        _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information), 
            It.IsAny<EventId>(), 
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("E2EE upload") && v.ToString()!.Contains("is finished")), 
            It.IsAny<Exception>(), 
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
} 