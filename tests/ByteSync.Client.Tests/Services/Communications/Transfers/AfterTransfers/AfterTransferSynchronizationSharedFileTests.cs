using NUnit.Framework;
using Moq;
using System.Reactive.Subjects;
using ByteSync.Business.Synchronizations;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Services.Communications.Transfers.AfterTransfers;
using FluentAssertions;
using ByteSync.Interfaces.Communications;

namespace ByteSync.Tests.Services.Communications.Transfers.AfterTransfers;

[TestFixture]
public class AfterTransferSynchronizationSharedFileTests
{
    private Mock<ISynchronizationService> _mockSynchronizationService = null!;
    private Mock<ISynchronizationApiClient> _mockSynchronizationApiClient = null!;
    private SynchronizationProcessData _synchronizationProcessData = null!;
    private BehaviorSubject<bool> _synchronizationDataTransmitted = null!;
    private CancellationTokenSource _cancellationTokenSource = null!;
    private AfterTransferSynchronizationSharedFile _afterTransferSynchronizationSharedFile = null!;
    private SharedFileDefinition _testSharedFileDefinition= null!;

    [SetUp]
    public void SetUp()
    {
        _mockSynchronizationService = new Mock<ISynchronizationService>();
        _mockSynchronizationApiClient = new Mock<ISynchronizationApiClient>();
        
        _synchronizationDataTransmitted = new BehaviorSubject<bool>(false);
        _cancellationTokenSource = new CancellationTokenSource();
        
        _synchronizationProcessData = new SynchronizationProcessData();
        _synchronizationProcessData.SynchronizationDataTransmitted.OnNext(false);
        
        _mockSynchronizationService.Setup(x => x.SynchronizationProcessData)
            .Returns(_synchronizationProcessData);

        _testSharedFileDefinition = new SharedFileDefinition
        {
            Id = "test-file-id",
            SessionId = "test-session-id",
            AdditionalName = "test-file"
        };

        _afterTransferSynchronizationSharedFile = new AfterTransferSynchronizationSharedFile(
            _mockSynchronizationService.Object,
            _mockSynchronizationApiClient.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _synchronizationDataTransmitted.Dispose();
        _cancellationTokenSource.Dispose();
        _synchronizationProcessData.CancellationTokenSource?.Dispose();
    }

    [Test]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var service = new AfterTransferSynchronizationSharedFile(
            _mockSynchronizationService.Object,
            _mockSynchronizationApiClient.Object);

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<IAfterTransferSharedFile>();
    }

    [Test]
    public void Constructor_WithNullSynchronizationService_ShouldNotThrow()
    {
        // Act & Assert
        Action act = () => new AfterTransferSynchronizationSharedFile(
            null!,
            _mockSynchronizationApiClient.Object);

        act.Should().NotThrow();
    }

    [Test]
    public void Constructor_WithNullSynchronizationApiClient_ShouldNotThrow()
    {
        // Act & Assert
        Action act = () => new AfterTransferSynchronizationSharedFile(
            _mockSynchronizationService.Object,
            null!);

        act.Should().NotThrow();
    }

    [Test]
    public async Task OnFilePartUploaded_WhenSynchronizationDataAlreadyTransmitted_ShouldCompleteImmediately()
    {
        // Arrange
        _synchronizationProcessData.SynchronizationDataTransmitted.OnNext(true);

        // Act
        var task = _afterTransferSynchronizationSharedFile.OnFilePartUploaded(_testSharedFileDefinition);

        // Assert
        await task.WaitAsync(TimeSpan.FromMilliseconds(100));
        task.IsCompletedSuccessfully.Should().BeTrue();
        _mockSynchronizationApiClient.Verify(x => x.InformSynchronizationActionError(It.IsAny<SharedFileDefinition>()), Times.Never);
    }

    [Test]
    public async Task OnFilePartUploaded_WhenSynchronizationDataTransmittedLater_ShouldWaitAndComplete()
    {
        // Arrange
        var task = _afterTransferSynchronizationSharedFile.OnFilePartUploaded(_testSharedFileDefinition);

        // Act
        await Task.Delay(50);
        _synchronizationProcessData.SynchronizationDataTransmitted.OnNext(true);

        // Assert
        await task.WaitAsync(TimeSpan.FromSeconds(1));
        task.IsCompletedSuccessfully.Should().BeTrue();
        _mockSynchronizationApiClient.Verify(x => x.InformSynchronizationActionError(It.IsAny<SharedFileDefinition>()), Times.Never);
    }

    [Test]
    public async Task OnUploadFinished_WhenSynchronizationDataAlreadyTransmitted_ShouldCompleteImmediately()
    {
        // Arrange
        _synchronizationProcessData.SynchronizationDataTransmitted.OnNext(true);

        // Act
        var task = _afterTransferSynchronizationSharedFile.OnUploadFinished(_testSharedFileDefinition);

        // Assert
        await task.WaitAsync(TimeSpan.FromMilliseconds(100));
        task.IsCompletedSuccessfully.Should().BeTrue();
        _mockSynchronizationApiClient.Verify(x => x.InformSynchronizationActionError(It.IsAny<SharedFileDefinition>()), Times.Never);
    }

    [Test]
    public async Task OnUploadFinished_WhenSynchronizationDataTransmittedLater_ShouldWaitAndComplete()
    {
        // Arrange
        var task = _afterTransferSynchronizationSharedFile.OnUploadFinished(_testSharedFileDefinition);

        // Act
        await Task.Delay(50);
        _synchronizationProcessData.SynchronizationDataTransmitted.OnNext(true);

        // Assert
        await task.WaitAsync(TimeSpan.FromSeconds(1));
        task.IsCompletedSuccessfully.Should().BeTrue();
        _mockSynchronizationApiClient.Verify(x => x.InformSynchronizationActionError(It.IsAny<SharedFileDefinition>()), Times.Never);
    }

    [Test]
    public async Task OnFilePartUploadedError_WhenSynchronizationDataAlreadyTransmitted_ShouldCompleteAndInformError()
    {
        // Arrange
        _synchronizationProcessData.SynchronizationDataTransmitted.OnNext(true);
        var testException = new Exception("Test upload error");

        // Act
        var task = _afterTransferSynchronizationSharedFile.OnFilePartUploadedError(_testSharedFileDefinition, testException);

        // Assert
        await task.WaitAsync(TimeSpan.FromMilliseconds(500));
        task.IsCompletedSuccessfully.Should().BeTrue();
        _mockSynchronizationApiClient.Verify(x => x.InformSynchronizationActionError(_testSharedFileDefinition), Times.Once);
    }

    [Test]
    public async Task OnFilePartUploadedError_WhenSynchronizationDataTransmittedLater_ShouldWaitAndInformError()
    {
        // Arrange
        var testException = new Exception("Test upload error");
        var task = _afterTransferSynchronizationSharedFile.OnFilePartUploadedError(_testSharedFileDefinition, testException);

        // Act
        await Task.Delay(50);
        _synchronizationProcessData.SynchronizationDataTransmitted.OnNext(true);

        // Assert
        await task.WaitAsync(TimeSpan.FromSeconds(1));
        task.IsCompletedSuccessfully.Should().BeTrue();
        _mockSynchronizationApiClient.Verify(x => x.InformSynchronizationActionError(_testSharedFileDefinition), Times.Once);
    }

    [Test]
    public async Task OnUploadFinishedError_WhenSynchronizationDataAlreadyTransmitted_ShouldCompleteAndInformError()
    {
        // Arrange
        _synchronizationProcessData.SynchronizationDataTransmitted.OnNext(true);
        var testException = new Exception("Test upload error");

        // Act
        var task = _afterTransferSynchronizationSharedFile.OnUploadFinishedError(_testSharedFileDefinition, testException);

        // Assert
        await task.WaitAsync(TimeSpan.FromMilliseconds(500));
        task.IsCompletedSuccessfully.Should().BeTrue();
        _mockSynchronizationApiClient.Verify(x => x.InformSynchronizationActionError(_testSharedFileDefinition), Times.Once);
    }

    [Test]
    public async Task OnUploadFinishedError_WhenSynchronizationDataTransmittedLater_ShouldWaitAndInformError()
    {
        // Arrange
        var testException = new Exception("Test upload error");
        var task = _afterTransferSynchronizationSharedFile.OnUploadFinishedError(_testSharedFileDefinition, testException);

        // Act
        await Task.Delay(50);
        _synchronizationProcessData.SynchronizationDataTransmitted.OnNext(true);

        // Assert
        await task.WaitAsync(TimeSpan.FromSeconds(1));
        task.IsCompletedSuccessfully.Should().BeTrue();
        _mockSynchronizationApiClient.Verify(x => x.InformSynchronizationActionError(_testSharedFileDefinition), Times.Once);
    }

    [Test]
    public void OnFilePartUploaded_WhenCancellationRequested_ShouldThrowOperationCanceledException()
    {
        // Arrange
        _synchronizationProcessData.CancellationTokenSource.Cancel();

        // Act & Assert
        Func<Task> act = async () => await _afterTransferSynchronizationSharedFile.OnFilePartUploaded(_testSharedFileDefinition);
        act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public void OnUploadFinished_WhenCancellationRequested_ShouldThrowOperationCanceledException()
    {
        // Arrange
        _synchronizationProcessData.CancellationTokenSource.Cancel();

        // Act & Assert
        Func<Task> act = async () => await _afterTransferSynchronizationSharedFile.OnUploadFinished(_testSharedFileDefinition);
        act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public void OnFilePartUploadedError_WhenCancellationRequested_ShouldThrowOperationCanceledException()
    {
        // Arrange
        _synchronizationProcessData.CancellationTokenSource.Cancel();
        var testException = new Exception("Test error");

        // Act & Assert
        Func<Task> act = async () => await _afterTransferSynchronizationSharedFile.OnFilePartUploadedError(_testSharedFileDefinition, testException);
        act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public void OnUploadFinishedError_WhenCancellationRequested_ShouldThrowOperationCanceledException()
    {
        // Arrange
        _synchronizationProcessData.CancellationTokenSource.Cancel();
        var testException = new Exception("Test error");

        // Act & Assert
        Func<Task> act = async () => await _afterTransferSynchronizationSharedFile.OnUploadFinishedError(_testSharedFileDefinition, testException);
        act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task OnFilePartUploadedError_WhenApiClientThrows_ShouldPropagateException()
    {
        // Arrange
        _synchronizationProcessData.SynchronizationDataTransmitted.OnNext(true);
        var testException = new Exception("Test upload error");
        var apiException = new Exception("API error");
        _mockSynchronizationApiClient.Setup(x => x.InformSynchronizationActionError(It.IsAny<SharedFileDefinition>()))
            .ThrowsAsync(apiException);

        // Act & Assert
        Func<Task> act = async () => await _afterTransferSynchronizationSharedFile.OnFilePartUploadedError(_testSharedFileDefinition, testException);
        await act.Should().ThrowAsync<Exception>().WithMessage("API error");
    }

    [Test]
    public async Task OnUploadFinishedError_WhenApiClientThrows_ShouldPropagateException()
    {
        // Arrange
        _synchronizationProcessData.SynchronizationDataTransmitted.OnNext(true);
        var testException = new Exception("Test upload error");
        var apiException = new Exception("API error");
        _mockSynchronizationApiClient.Setup(x => x.InformSynchronizationActionError(It.IsAny<SharedFileDefinition>()))
            .ThrowsAsync(apiException);

        // Act & Assert
        Func<Task> act = async () => await _afterTransferSynchronizationSharedFile.OnUploadFinishedError(_testSharedFileDefinition, testException);
        await act.Should().ThrowAsync<Exception>().WithMessage("API error");
    }

    [Test]
    public async Task OnFilePartUploaded_WithNullSharedFileDefinition_ShouldNotThrow()
    {
        // Arrange
        _synchronizationProcessData.SynchronizationDataTransmitted.OnNext(true);

        // Act
        var task = _afterTransferSynchronizationSharedFile.OnFilePartUploaded(null!);

        // Assert
        await task.WaitAsync(TimeSpan.FromMilliseconds(100));
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Test]
    public async Task OnUploadFinished_WithNullSharedFileDefinition_ShouldNotThrow()
    {
        // Arrange
        _synchronizationProcessData.SynchronizationDataTransmitted.OnNext(true);

        // Act
        var task = _afterTransferSynchronizationSharedFile.OnUploadFinished(null!);

        // Assert
        await task.WaitAsync(TimeSpan.FromMilliseconds(100));
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Test]
    public async Task OnFilePartUploadedError_WithNullSharedFileDefinition_ShouldStillInformError()
    {
        // Arrange
        _synchronizationProcessData.SynchronizationDataTransmitted.OnNext(true);
        var testException = new Exception("Test error");

        // Act
        var task = _afterTransferSynchronizationSharedFile.OnFilePartUploadedError(null!, testException);

        // Assert
        await task.WaitAsync(TimeSpan.FromMilliseconds(500));
        task.IsCompletedSuccessfully.Should().BeTrue();
        _mockSynchronizationApiClient.Verify(x => x.InformSynchronizationActionError(null!), Times.Once);
    }

    [Test]
    public async Task OnUploadFinishedError_WithNullSharedFileDefinition_ShouldStillInformError()
    {
        // Arrange
        _synchronizationProcessData.SynchronizationDataTransmitted.OnNext(true);
        var testException = new Exception("Test error");

        // Act
        var task = _afterTransferSynchronizationSharedFile.OnUploadFinishedError(null!, testException);

        // Assert
        await task.WaitAsync(TimeSpan.FromMilliseconds(500));
        task.IsCompletedSuccessfully.Should().BeTrue();
        _mockSynchronizationApiClient.Verify(x => x.InformSynchronizationActionError(null!), Times.Once);
    }
}
