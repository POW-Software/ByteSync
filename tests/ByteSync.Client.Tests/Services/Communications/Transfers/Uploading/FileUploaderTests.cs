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
public class FileUploaderTests
{
    private Mock<ISlicerEncrypter> _mockSlicerEncrypter = null!;
    private Mock<ILogger<FileUploader>> _mockLogger = null!;
    private Mock<IFileUploadCoordinator> _mockFileUploadCoordinator = null!;
    private Mock<IFileSlicer> _mockFileSlicer = null!;
    private Mock<IFileUploadWorker> _mockFileUploadWorker = null!;
    private Mock<IFilePartUploadAsserter> _mockFilePartUploadAsserter = null!;
    private SharedFileDefinition _sharedFileDefinition = null!;
    private string _testFilePath = null!;
    private MemoryStream _testMemoryStream = null!;
    private SemaphoreSlim _semaphoreSlim = null!;
    private Mock<IAdaptiveUploadController> _mockAdaptiveController = null!;

    [SetUp]
    public void SetUp()
    {
        _mockSlicerEncrypter = new Mock<ISlicerEncrypter>();
        _mockLogger = new Mock<ILogger<FileUploader>>();
        _mockFileUploadCoordinator = new Mock<IFileUploadCoordinator>();
        _mockFileSlicer = new Mock<IFileSlicer>();
        _mockFileUploadWorker = new Mock<IFileUploadWorker>();
        _mockFilePartUploadAsserter = new Mock<IFilePartUploadAsserter>();
        _semaphoreSlim = new SemaphoreSlim(1, 1);
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

        // Setup coordinator mocks
        _mockFileUploadCoordinator.Setup(x => x.AvailableSlices).Returns(Channel.CreateBounded<FileUploaderSlice>(8));
        _mockFileUploadCoordinator.Setup(x => x.WaitForCompletionAsync()).Returns(Task.CompletedTask);
        _mockFileUploadCoordinator.Setup(x => x.SyncRoot).Returns(new object());
        
        // Initialize semaphore
        _semaphoreSlim = new SemaphoreSlim(1, 1);
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
    public void Constructor_WithValidFile_ShouldCreateInstance()
    {
        // Act
        var fileUploader = new FileUploader(
            _testFilePath,
            null,
            _sharedFileDefinition,
            _mockFileUploadCoordinator.Object,
            _mockFileSlicer.Object,
            _mockFileUploadWorker.Object,
            _mockFilePartUploadAsserter.Object,
            _mockSlicerEncrypter.Object,
            _mockLogger.Object,
            _semaphoreSlim,
            _mockAdaptiveController.Object);

        // Assert
        fileUploader.Should().NotBeNull();
    }

    [Test]
    public void Constructor_WithValidMemoryStream_ShouldCreateInstance()
    {
        // Act
        var fileUploader = new FileUploader(
            null,
            _testMemoryStream,
            _sharedFileDefinition,
            _mockFileUploadCoordinator.Object,
            _mockFileSlicer.Object,
            _mockFileUploadWorker.Object,
            _mockFilePartUploadAsserter.Object,
            _mockSlicerEncrypter.Object,
            _mockLogger.Object,
            _semaphoreSlim,
            _mockAdaptiveController.Object);

        // Assert
        fileUploader.Should().NotBeNull();
    }

    [Test]
    public void Constructor_WithNullFileAndMemoryStream_ShouldThrowApplicationException()
    {
        // Act & Assert
        var action = () => new FileUploader(
            null,
            null,
            _sharedFileDefinition,
            _mockFileUploadCoordinator.Object,
            _mockFileSlicer.Object,
            _mockFileUploadWorker.Object,
            _mockFilePartUploadAsserter.Object,
            _mockSlicerEncrypter.Object,
            _mockLogger.Object,
            _semaphoreSlim,
            _mockAdaptiveController.Object);

        action.Should().Throw<ApplicationException>()
            .WithMessage("localFileToUpload and memoryStream are null");
    }

    [Test]
    public void Constructor_WithNullSharedFileDefinition_ShouldThrowNullReferenceException()
    {
        // Act & Assert
        var action = () => new FileUploader(
            _testFilePath,
            null,
            null!,
            _mockFileUploadCoordinator.Object,
            _mockFileSlicer.Object,
            _mockFileUploadWorker.Object,
            _mockFilePartUploadAsserter.Object,
            _mockSlicerEncrypter.Object,
            _mockLogger.Object,
            _semaphoreSlim,
            _mockAdaptiveController.Object);

        action.Should().Throw<NullReferenceException>()
            .WithMessage("SharedFileDefinition is null");
    }

    [Test]
    public async Task Upload_WithFile_ShouldCallUploadFile()
    {
        // Arrange
        var fileUploader = new FileUploader(
            _testFilePath,
            null,
            _sharedFileDefinition,
            _mockFileUploadCoordinator.Object,
            _mockFileSlicer.Object,
            _mockFileUploadWorker.Object,
            _mockFilePartUploadAsserter.Object,
            _mockSlicerEncrypter.Object,
            _mockLogger.Object,
            _semaphoreSlim,
            _mockAdaptiveController.Object);

        _mockSlicerEncrypter.Setup(x => x.Initialize(It.IsAny<FileInfo>(), It.IsAny<SharedFileDefinition>()));

        // Act
        await fileUploader.Upload();

        // Assert
        _mockSlicerEncrypter.Verify(x => x.Initialize(It.IsAny<FileInfo>(), It.IsAny<SharedFileDefinition>()), Times.Once);
        _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information), 
            It.IsAny<EventId>(), 
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting the E2EE upload")), 
            It.IsAny<Exception>(), 
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Test]
    public async Task Upload_WithMemoryStream_ShouldCallUploadMemoryStream()
    {
        // Arrange
        var fileUploader = new FileUploader(
            null,
            _testMemoryStream,
            _sharedFileDefinition,
            _mockFileUploadCoordinator.Object,
            _mockFileSlicer.Object,
            _mockFileUploadWorker.Object,
            _mockFilePartUploadAsserter.Object,
            _mockSlicerEncrypter.Object,
            _mockLogger.Object,
            _semaphoreSlim,
            _mockAdaptiveController.Object);

        _mockSlicerEncrypter.Setup(x => x.Initialize(It.IsAny<MemoryStream>(), It.IsAny<SharedFileDefinition>()));

        // Act
        await fileUploader.Upload();

        // Assert
        _mockSlicerEncrypter.Verify(x => x.Initialize(It.IsAny<MemoryStream>(), It.IsAny<SharedFileDefinition>()), Times.Once);
        _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information), 
            It.IsAny<EventId>(), 
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting the E2EE upload")), 
            It.IsAny<Exception>(), 
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Test]
    public void MaxSliceLength_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var fileUploader = new FileUploader(
            _testFilePath,
            null,
            _sharedFileDefinition,
            _mockFileUploadCoordinator.Object,
            _mockFileSlicer.Object,
            _mockFileUploadWorker.Object,
            _mockFilePartUploadAsserter.Object,
            _mockSlicerEncrypter.Object,
            _mockLogger.Object,
            _semaphoreSlim,
            _mockAdaptiveController.Object);

        var expectedValue = 1024 * 1024; // 1MB

        // Act
        fileUploader.MaxSliceLength = expectedValue;
        var result = fileUploader.MaxSliceLength;

        // Assert
        result.Should().Be(expectedValue);
    }

    [Test]
    public async Task Upload_ShouldPrepareUploadAndSetFileMetadata()
    {
        // Arrange
        var fileUploader = new FileUploader(
            _testFilePath,
            null,
            _sharedFileDefinition,
            _mockFileUploadCoordinator.Object,
            _mockFileSlicer.Object,
            _mockFileUploadWorker.Object,
            _mockFilePartUploadAsserter.Object,
            _mockSlicerEncrypter.Object,
            _mockLogger.Object,
            _semaphoreSlim,
            _mockAdaptiveController.Object);

        var fileInfo = new FileInfo(_testFilePath);
        var originalIV = _sharedFileDefinition.IV;

        _mockSlicerEncrypter.Setup(x => x.Initialize(It.IsAny<FileInfo>(), It.IsAny<SharedFileDefinition>()));

        // Act
        await fileUploader.Upload();

        // Assert
        _sharedFileDefinition.IV.Should().NotBeNull();
        if (originalIV != null)
        {
            _sharedFileDefinition.IV.Should().NotBeEquivalentTo(originalIV);
        }
        
        _sharedFileDefinition.UploadedFileLength.Should().Be(fileInfo.Length);
    }
} 