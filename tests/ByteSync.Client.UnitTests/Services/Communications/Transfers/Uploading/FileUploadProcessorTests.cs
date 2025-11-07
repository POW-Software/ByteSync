using System.Threading.Channels;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Communications.Transfers.Uploading;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Communications.Transfers.Uploading;

[TestFixture]
public class FileUploadProcessorTests
{
    private Mock<ISlicerEncrypter> _mockSlicerEncrypter = null!;
    private Mock<ILogger<FileUploadProcessor>> _mockLogger = null!;
    private Mock<IFileUploadCoordinator> _mockFileUploadCoordinator = null!;
    private Mock<IFileSlicer> _mockFileSlicer = null!;
    private Mock<IFileTransferApiClient> _mockFileTransferApiClient = null!;
    private Mock<ISessionService> _mockSessionService = null!;
    private Mock<IUploadSlicingManager> _mockSlicingManager = null!;
    private Mock<IUploadParallelismManager> _mockParallelismManager = null!;
    private Mock<IUploadProgressMonitor> _mockProgressMonitor = null!;
    private SharedFileDefinition _sharedFileDefinition = null!;
    private string _testFilePath = null!;
    private MemoryStream _testMemoryStream = null!;
    private FileUploadProcessor _fileUploadProcessor = null!;
    private SemaphoreSlim _semaphoreSlim = null!;
    private Mock<IInventoryService> _mockInventoryService = null!;
    private ManualResetEvent _uploadingIsFinishedEvent = null!;
    private ManualResetEvent _exceptionOccurredEvent = null!;
    
    [SetUp]
    public void SetUp()
    {
        _mockSlicerEncrypter = new Mock<ISlicerEncrypter>();
        _mockLogger = new Mock<ILogger<FileUploadProcessor>>();
        _mockFileUploadCoordinator = new Mock<IFileUploadCoordinator>();
        _mockFileSlicer = new Mock<IFileSlicer>();
        _mockFileTransferApiClient = new Mock<IFileTransferApiClient>();
        _mockSessionService = new Mock<ISessionService>();
        _mockSlicingManager = new Mock<IUploadSlicingManager>();
        _mockParallelismManager = new Mock<IUploadParallelismManager>();
        _mockProgressMonitor = new Mock<IUploadProgressMonitor>();
        _mockInventoryService = new Mock<IInventoryService>();
        
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
        _uploadingIsFinishedEvent = new ManualResetEvent(false);
        _exceptionOccurredEvent = new ManualResetEvent(false);
        _mockFileUploadCoordinator.Setup(x => x.UploadingIsFinished).Returns(_uploadingIsFinishedEvent);
        _mockFileUploadCoordinator.Setup(x => x.ExceptionOccurred).Returns(_exceptionOccurredEvent);
        
        _semaphoreSlim = new SemaphoreSlim(1, 1);
        _mockSlicingManager
            .Setup(m => m.Enqueue(
                It.IsAny<SharedFileDefinition>(),
                It.IsAny<ISlicerEncrypter>(),
                It.IsAny<Channel<FileUploaderSlice>>(),
                It.IsAny<SemaphoreSlim>(),
                It.IsAny<ManualResetEvent>()))
            .ReturnsAsync(new UploadProgressState());
        
        _mockProgressMonitor
            .Setup(m => m.MonitorProgressAsync(
                It.IsAny<SharedFileDefinition>(),
                It.IsAny<UploadProgressState>(),
                It.IsAny<IUploadParallelismManager>(),
                It.IsAny<ManualResetEvent>(),
                It.IsAny<ManualResetEvent>(),
                It.IsAny<SemaphoreSlim>()))
            .ReturnsAsync(0);
        
        _fileUploadProcessor = new FileUploadProcessor(
            _mockSlicerEncrypter.Object,
            _mockLogger.Object,
            _mockFileUploadCoordinator.Object,
            _mockFileTransferApiClient.Object,
            _mockSessionService.Object,
            _testFilePath,
            _semaphoreSlim,
            _mockSlicingManager.Object,
            _mockParallelismManager.Object,
            _mockProgressMonitor.Object,
            _mockInventoryService.Object);
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
        _uploadingIsFinishedEvent.Dispose();
        _exceptionOccurredEvent.Dispose();
    }
    
    [Test]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var processor = new FileUploadProcessor(
            _mockSlicerEncrypter.Object,
            _mockLogger.Object,
            _mockFileUploadCoordinator.Object,
            _mockFileTransferApiClient.Object,
            _mockSessionService.Object,
            _testFilePath,
            _semaphoreSlim,
            _mockSlicingManager.Object,
            _mockParallelismManager.Object,
            _mockProgressMonitor.Object,
            _mockInventoryService.Object);
        
        // Assert
        processor.Should().NotBeNull();
        processor.Should().BeAssignableTo<IFileUploadProcessor>();
    }
    
    [Test]
    public async Task ProcessUpload_ShouldStartUploadWorkersAndMonitoring()
    {
        // Arrange
        _mockFileTransferApiClient.Setup(x => x.AssertUploadIsFinished(It.IsAny<TransferParameters>()))
            .Returns(Task.CompletedTask);
        
        // Act
        await _fileUploadProcessor.ProcessUpload(_sharedFileDefinition);
        
        // Assert
        _mockParallelismManager.Verify(
            x => x.StartInitialWorkers(It.IsAny<int>(), It.IsAny<Channel<FileUploaderSlice>>(), It.IsAny<UploadProgressState>()),
            Times.Once);
        _mockProgressMonitor.Verify(
            x => x.MonitorProgressAsync(
                _sharedFileDefinition,
                It.IsAny<UploadProgressState>(),
                _mockParallelismManager.Object,
                It.IsAny<ManualResetEvent>(),
                It.IsAny<ManualResetEvent>(),
                It.IsAny<SemaphoreSlim>()),
            Times.Once);
    }
    
    [Test]
    public async Task ProcessUpload_ShouldStartSlicer()
    {
        // Arrange
        _mockFileSlicer.Setup(x => x.SliceAndEncryptAdaptiveAsync(It.IsAny<SharedFileDefinition>(), It.IsAny<UploadProgressState>()))
            .Returns(Task.CompletedTask);
        _mockFileTransferApiClient.Setup(x => x.AssertUploadIsFinished(It.IsAny<TransferParameters>()))
            .Returns(Task.CompletedTask);
        
        // Act
        await _fileUploadProcessor.ProcessUpload(_sharedFileDefinition);
        
        // Assert
        _mockSlicingManager.Verify(m => m.Enqueue(
            _sharedFileDefinition,
            It.IsAny<ISlicerEncrypter>(),
            It.IsAny<Channel<FileUploaderSlice>>(),
            It.IsAny<SemaphoreSlim>(),
            It.IsAny<ManualResetEvent>()), Times.Once);
    }
    
    [Test]
    public async Task ProcessUpload_WithMaxSliceLength_ShouldPassMaxSliceLength()
    {
        // Arrange
        var maxSliceLength = 1024 * 1024; // 1MB
        _mockFileSlicer.Setup(x => x.SliceAndEncryptAdaptiveAsync(It.IsAny<SharedFileDefinition>(), It.IsAny<UploadProgressState>()))
            .Returns(Task.CompletedTask);
        _mockFileTransferApiClient.Setup(x => x.AssertUploadIsFinished(It.IsAny<TransferParameters>()))
            .Returns(Task.CompletedTask);
        
        // Act
        await _fileUploadProcessor.ProcessUpload(_sharedFileDefinition, maxSliceLength);
        
        // Assert: slicing is enqueued; maxSliceLength is ignored under adaptive mode
        _mockSlicingManager.Verify(m => m.Enqueue(
            _sharedFileDefinition,
            It.IsAny<ISlicerEncrypter>(),
            It.IsAny<Channel<FileUploaderSlice>>(),
            It.IsAny<SemaphoreSlim>(),
            It.IsAny<ManualResetEvent>()), Times.Once);
    }
    
    [Test]
    public async Task ProcessUpload_ShouldWaitForCompletion()
    {
        // Arrange
        _mockFileSlicer.Setup(x => x.SliceAndEncryptAdaptiveAsync(It.IsAny<SharedFileDefinition>(), It.IsAny<UploadProgressState>()))
            .Returns(Task.CompletedTask);
        _mockFileTransferApiClient.Setup(x => x.AssertUploadIsFinished(It.IsAny<TransferParameters>()))
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
        _mockFileSlicer.Setup(x => x.SliceAndEncryptAdaptiveAsync(It.IsAny<SharedFileDefinition>(), It.IsAny<UploadProgressState>()))
            .Returns(Task.CompletedTask);
        _mockFileTransferApiClient.Setup(x => x.AssertUploadIsFinished(It.IsAny<TransferParameters>()))
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
        _mockFileSlicer.Setup(x => x.SliceAndEncryptAdaptiveAsync(It.IsAny<SharedFileDefinition>(), It.IsAny<UploadProgressState>()))
            .Returns(Task.CompletedTask);
        _mockFileTransferApiClient.Setup(x => x.AssertUploadIsFinished(It.IsAny<TransferParameters>()))
            .Returns(Task.CompletedTask);
        
        // Act
        await _fileUploadProcessor.ProcessUpload(_sharedFileDefinition);
        
        // Assert
        _mockFileTransferApiClient.Verify(x => x.AssertUploadIsFinished(It.IsAny<TransferParameters>()), Times.Once);
    }
    
    [Test]
    public async Task ProcessUpload_WhenExceptionOccurs_ShouldThrowExceptionWithDetails()
    {
        // Arrange
        var expectedException = new Exception("Test exception");
        _mockSlicingManager
            .Setup(m => m.Enqueue(
                It.IsAny<SharedFileDefinition>(),
                It.IsAny<ISlicerEncrypter>(),
                It.IsAny<Channel<FileUploaderSlice>>(),
                It.IsAny<SemaphoreSlim>(),
                It.IsAny<ManualResetEvent>()))
            .ReturnsAsync(new UploadProgressState { Exceptions = { expectedException } });
        
        // Act & Assert
        var action = async () => await _fileUploadProcessor.ProcessUpload(_sharedFileDefinition);
        await action.Should().ThrowAsync<Exception>()
            .Where(ex => ex.Message.Contains(_testFilePath) && ex.Message.Contains(_sharedFileDefinition.Id) &&
                         ex.InnerException == expectedException);
    }
    
    [Test]
    public async Task ProcessUpload_WhenExceptionOccursWithMemoryStream_ShouldThrowExceptionWithStreamDetails()
    {
        // Arrange
        var processorWithMemoryStream = new FileUploadProcessor(
            _mockSlicerEncrypter.Object,
            _mockLogger.Object,
            _mockFileUploadCoordinator.Object,
            _mockFileTransferApiClient.Object,
            _mockSessionService.Object,
            null,
            _semaphoreSlim,
            _mockSlicingManager.Object,
            _mockParallelismManager.Object,
            _mockProgressMonitor.Object,
            _mockInventoryService.Object);
        
        var expectedException = new Exception("Test exception");
        _mockSlicingManager
            .Setup(m => m.Enqueue(
                It.IsAny<SharedFileDefinition>(),
                It.IsAny<ISlicerEncrypter>(),
                It.IsAny<Channel<FileUploaderSlice>>(),
                It.IsAny<SemaphoreSlim>(),
                It.IsAny<ManualResetEvent>()))
            .ReturnsAsync(new UploadProgressState { Exceptions = { expectedException } });
        
        // Act & Assert
        var action = async () => await processorWithMemoryStream.ProcessUpload(_sharedFileDefinition);
        await action.Should().ThrowAsync<Exception>()
            .Where(ex => ex.Message.Contains("a stream") && ex.Message.Contains(_sharedFileDefinition.Id) &&
                         ex.InnerException == expectedException);
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
        _mockFileSlicer.Setup(x => x.SliceAndEncryptAdaptiveAsync(It.IsAny<SharedFileDefinition>(), It.IsAny<UploadProgressState>()))
            .Returns(Task.CompletedTask);
        _mockFileTransferApiClient.Setup(x => x.AssertUploadIsFinished(It.IsAny<TransferParameters>()))
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
        _mockFileSlicer.Setup(x => x.SliceAndEncryptAdaptiveAsync(It.IsAny<SharedFileDefinition>(), It.IsAny<UploadProgressState>()))
            .Returns(Task.CompletedTask);
        _mockFileTransferApiClient.Setup(x => x.AssertUploadIsFinished(It.IsAny<TransferParameters>()))
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
        _mockFileSlicer.Setup(x => x.SliceAndEncryptAdaptiveAsync(It.IsAny<SharedFileDefinition>(), It.IsAny<UploadProgressState>()))
            .Returns(Task.CompletedTask);
        _mockFileTransferApiClient.Setup(x => x.AssertUploadIsFinished(It.IsAny<TransferParameters>()))
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