using NUnit.Framework;
using Moq;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Communications.Transfers;
using ByteSync.Services.Communications.Transfers.Uploading;
using FluentAssertions;

namespace ByteSync.Tests.Services.Communications.Transfers;

[TestFixture]
public class FilePartUploadAsserterTests
{
    private Mock<IFileTransferApiClient> _mockFileTransferApiClient;
    private Mock<ISessionService> _mockSessionService;
    private FilePartUploadAsserter _filePartUploadAsserter;
    private SharedFileDefinition _sharedFileDefinition;

    [SetUp]
    public void SetUp()
    {
        _mockFileTransferApiClient = new Mock<IFileTransferApiClient>();
        _mockSessionService = new Mock<ISessionService>();

        _filePartUploadAsserter = new FilePartUploadAsserter(
            _mockFileTransferApiClient.Object,
            _mockSessionService.Object);

        _sharedFileDefinition = new SharedFileDefinition
        {
            Id = "test-file-id",
            SessionId = "test-session-id",
            UploadedFileLength = 1024
        };
    }

    [Test]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Assert
        _filePartUploadAsserter.Should().NotBeNull();
    }

    [Test]
    public async Task AssertFilePartIsUploaded_WithValidParameters_ShouldCallApiClient()
    {
        // Arrange
        var partNumber = 1;
        _mockFileTransferApiClient.Setup(x => x.AssertFilePartIsUploaded(It.IsAny<TransferParameters>()))
            .Returns(Task.CompletedTask);

        // Act
        await _filePartUploadAsserter.AssertFilePartIsUploaded(_sharedFileDefinition, partNumber);

        // Assert
        _mockFileTransferApiClient.Verify(x => x.AssertFilePartIsUploaded(It.IsAny<TransferParameters>()), Times.Once);
    }

    [Test]
    public async Task AssertFilePartIsUploaded_ShouldCreateCorrectTransferParameters()
    {
        // Arrange
        var partNumber = 5;
        TransferParameters? capturedParameters = null;
        
        _mockFileTransferApiClient.Setup(x => x.AssertFilePartIsUploaded(It.IsAny<TransferParameters>()))
            .Callback<TransferParameters>(p => capturedParameters = p)
            .Returns(Task.CompletedTask);

        // Act
        await _filePartUploadAsserter.AssertFilePartIsUploaded(_sharedFileDefinition, partNumber);

        // Assert
        capturedParameters.Should().NotBeNull();
        capturedParameters!.SessionId.Should().Be(_sharedFileDefinition.SessionId);
        capturedParameters.SharedFileDefinition.Should().Be(_sharedFileDefinition);
        capturedParameters.PartNumber.Should().Be(partNumber);
        capturedParameters.TotalParts.Should().BeNull();
    }

    [Test]
    public async Task AssertFilePartIsUploaded_WithZeroPartNumber_ShouldCallApiClient()
    {
        // Arrange
        var partNumber = 0;
        _mockFileTransferApiClient.Setup(x => x.AssertFilePartIsUploaded(It.IsAny<TransferParameters>()))
            .Returns(Task.CompletedTask);

        // Act
        await _filePartUploadAsserter.AssertFilePartIsUploaded(_sharedFileDefinition, partNumber);

        // Assert
        _mockFileTransferApiClient.Verify(x => x.AssertFilePartIsUploaded(It.IsAny<TransferParameters>()), Times.Once);
    }

    [Test]
    public async Task AssertFilePartIsUploaded_WithNegativePartNumber_ShouldCallApiClient()
    {
        // Arrange
        var partNumber = -1;
        _mockFileTransferApiClient.Setup(x => x.AssertFilePartIsUploaded(It.IsAny<TransferParameters>()))
            .Returns(Task.CompletedTask);

        // Act
        await _filePartUploadAsserter.AssertFilePartIsUploaded(_sharedFileDefinition, partNumber);

        // Assert
        _mockFileTransferApiClient.Verify(x => x.AssertFilePartIsUploaded(It.IsAny<TransferParameters>()), Times.Once);
    }

    [Test]
    public async Task AssertFilePartIsUploaded_WithLargePartNumber_ShouldCallApiClient()
    {
        // Arrange
        var partNumber = int.MaxValue;
        _mockFileTransferApiClient.Setup(x => x.AssertFilePartIsUploaded(It.IsAny<TransferParameters>()))
            .Returns(Task.CompletedTask);

        // Act
        await _filePartUploadAsserter.AssertFilePartIsUploaded(_sharedFileDefinition, partNumber);

        // Assert
        _mockFileTransferApiClient.Verify(x => x.AssertFilePartIsUploaded(It.IsAny<TransferParameters>()), Times.Once);
    }

    [Test]
    public async Task AssertFilePartIsUploaded_WhenApiClientThrowsException_ShouldPropagateException()
    {
        // Arrange
        var partNumber = 1;
        var expectedException = new Exception("API client error");
        
        _mockFileTransferApiClient.Setup(x => x.AssertFilePartIsUploaded(It.IsAny<TransferParameters>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var action = async () => await _filePartUploadAsserter.AssertFilePartIsUploaded(_sharedFileDefinition, partNumber);
        await action.Should().ThrowAsync<Exception>()
            .Where(ex => ex == expectedException);
    }

    [Test]
    public async Task AssertUploadIsFinished_WithValidParameters_ShouldCallApiClient()
    {
        // Arrange
        var totalParts = 5;
        var sessionId = "test-session-id";
        
        _mockSessionService.Setup(x => x.SessionId).Returns(sessionId);
        _mockFileTransferApiClient.Setup(x => x.AssertUploadIsFinished(It.IsAny<TransferParameters>()))
            .Returns(Task.CompletedTask);

        // Act
        await _filePartUploadAsserter.AssertUploadIsFinished(_sharedFileDefinition, totalParts);

        // Assert
        _mockFileTransferApiClient.Verify(x => x.AssertUploadIsFinished(It.IsAny<TransferParameters>()), Times.Once);
    }

    [Test]
    public async Task AssertUploadIsFinished_ShouldCreateCorrectTransferParameters()
    {
        // Arrange
        var totalParts = 10;
        var sessionId = "test-session-id";
        TransferParameters? capturedParameters = null;
        
        _mockSessionService.Setup(x => x.SessionId).Returns(sessionId);
        _mockFileTransferApiClient.Setup(x => x.AssertUploadIsFinished(It.IsAny<TransferParameters>()))
            .Callback<TransferParameters>(p => capturedParameters = p)
            .Returns(Task.CompletedTask);

        // Act
        await _filePartUploadAsserter.AssertUploadIsFinished(_sharedFileDefinition, totalParts);

        // Assert
        capturedParameters.Should().NotBeNull();
        capturedParameters!.SessionId.Should().Be(sessionId);
        capturedParameters.SharedFileDefinition.Should().Be(_sharedFileDefinition);
        capturedParameters.TotalParts.Should().Be(totalParts);
        capturedParameters.PartNumber.Should().BeNull();
    }

    [Test]
    public async Task AssertUploadIsFinished_WithZeroTotalParts_ShouldCallApiClient()
    {
        // Arrange
        var totalParts = 0;
        var sessionId = "test-session-id";
        
        _mockSessionService.Setup(x => x.SessionId).Returns(sessionId);
        _mockFileTransferApiClient.Setup(x => x.AssertUploadIsFinished(It.IsAny<TransferParameters>()))
            .Returns(Task.CompletedTask);

        // Act
        await _filePartUploadAsserter.AssertUploadIsFinished(_sharedFileDefinition, totalParts);

        // Assert
        _mockFileTransferApiClient.Verify(x => x.AssertUploadIsFinished(It.IsAny<TransferParameters>()), Times.Once);
    }

    [Test]
    public async Task AssertUploadIsFinished_WithNegativeTotalParts_ShouldCallApiClient()
    {
        // Arrange
        var totalParts = -1;
        var sessionId = "test-session-id";
        
        _mockSessionService.Setup(x => x.SessionId).Returns(sessionId);
        _mockFileTransferApiClient.Setup(x => x.AssertUploadIsFinished(It.IsAny<TransferParameters>()))
            .Returns(Task.CompletedTask);

        // Act
        await _filePartUploadAsserter.AssertUploadIsFinished(_sharedFileDefinition, totalParts);

        // Assert
        _mockFileTransferApiClient.Verify(x => x.AssertUploadIsFinished(It.IsAny<TransferParameters>()), Times.Once);
    }

    [Test]
    public async Task AssertUploadIsFinished_WithLargeTotalParts_ShouldCallApiClient()
    {
        // Arrange
        var totalParts = int.MaxValue;
        var sessionId = "test-session-id";
        
        _mockSessionService.Setup(x => x.SessionId).Returns(sessionId);
        _mockFileTransferApiClient.Setup(x => x.AssertUploadIsFinished(It.IsAny<TransferParameters>()))
            .Returns(Task.CompletedTask);

        // Act
        await _filePartUploadAsserter.AssertUploadIsFinished(_sharedFileDefinition, totalParts);

        // Assert
        _mockFileTransferApiClient.Verify(x => x.AssertUploadIsFinished(It.IsAny<TransferParameters>()), Times.Once);
    }

    [Test]
    public async Task AssertUploadIsFinished_WhenApiClientThrowsException_ShouldPropagateException()
    {
        // Arrange
        var totalParts = 5;
        var sessionId = "test-session-id";
        var expectedException = new Exception("API client error");
        
        _mockSessionService.Setup(x => x.SessionId).Returns(sessionId);
        _mockFileTransferApiClient.Setup(x => x.AssertUploadIsFinished(It.IsAny<TransferParameters>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var action = async () => await _filePartUploadAsserter.AssertUploadIsFinished(_sharedFileDefinition, totalParts);
        await action.Should().ThrowAsync<Exception>()
            .Where(ex => ex == expectedException);
    }

    [Test]
    public async Task AssertUploadIsFinished_WhenSessionIdIsNull_ShouldUseSharedFileDefinitionSessionId()
    {
        // Arrange
        var totalParts = 5;
        TransferParameters? capturedParameters = null;
        
        _mockSessionService.Setup(x => x.SessionId).Returns((string?)null);
        _mockFileTransferApiClient.Setup(x => x.AssertUploadIsFinished(It.IsAny<TransferParameters>()))
            .Callback<TransferParameters>(p => capturedParameters = p)
            .Returns(Task.CompletedTask);

        // Act
        await _filePartUploadAsserter.AssertUploadIsFinished(_sharedFileDefinition, totalParts);

        // Assert
        capturedParameters.Should().NotBeNull();
        capturedParameters!.SessionId.Should().Be(_sharedFileDefinition.SessionId);
    }

    [Test]
    public async Task AssertUploadIsFinished_WhenSessionIdIsEmpty_ShouldUseSharedFileDefinitionSessionId()
    {
        // Arrange
        var totalParts = 5;
        TransferParameters? capturedParameters = null;
        
        _mockSessionService.Setup(x => x.SessionId).Returns("");
        _mockFileTransferApiClient.Setup(x => x.AssertUploadIsFinished(It.IsAny<TransferParameters>()))
            .Callback<TransferParameters>(p => capturedParameters = p)
            .Returns(Task.CompletedTask);

        // Act
        await _filePartUploadAsserter.AssertUploadIsFinished(_sharedFileDefinition, totalParts);

        // Assert
        capturedParameters.Should().NotBeNull();
        capturedParameters!.SessionId.Should().Be(_sharedFileDefinition.SessionId);
    }

    [Test]
    public async Task AssertUploadIsFinished_WhenSessionIdIsWhitespace_ShouldUseSharedFileDefinitionSessionId()
    {
        // Arrange
        var totalParts = 5;
        TransferParameters? capturedParameters = null;
        
        _mockSessionService.Setup(x => x.SessionId).Returns("   ");
        _mockFileTransferApiClient.Setup(x => x.AssertUploadIsFinished(It.IsAny<TransferParameters>()))
            .Callback<TransferParameters>(p => capturedParameters = p)
            .Returns(Task.CompletedTask);

        // Act
        await _filePartUploadAsserter.AssertUploadIsFinished(_sharedFileDefinition, totalParts);

        // Assert
        capturedParameters.Should().NotBeNull();
        capturedParameters!.SessionId.Should().Be(_sharedFileDefinition.SessionId);
    }

    [Test]
    public async Task AssertFilePartIsUploaded_WithNullSharedFileDefinition_ShouldThrowNullReferenceException()
    {
        // Arrange
        var partNumber = 1;

        // Act & Assert
        var action = async () => await _filePartUploadAsserter.AssertFilePartIsUploaded(null!, partNumber);
        await action.Should().ThrowAsync<NullReferenceException>();
    }

    [Test]
    public async Task AssertUploadIsFinished_WithNullSharedFileDefinition_ShouldThrowNullReferenceException()
    {
        // Arrange
        var totalParts = 5;

        // Act & Assert
        var action = async () => await _filePartUploadAsserter.AssertUploadIsFinished(null!, totalParts);
        await action.Should().ThrowAsync<NullReferenceException>();
    }

    [Test]
    public async Task MultipleAssertFilePartIsUploadedCalls_ShouldWorkCorrectly()
    {
        // Arrange
        var partNumbers = new[] { 1, 2, 3, 4, 5 };
        _mockFileTransferApiClient.Setup(x => x.AssertFilePartIsUploaded(It.IsAny<TransferParameters>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        foreach (var partNumber in partNumbers)
        {
            var action = async () => await _filePartUploadAsserter.AssertFilePartIsUploaded(_sharedFileDefinition, partNumber);
            await action.Should().NotThrowAsync();
        }

        _mockFileTransferApiClient.Verify(x => x.AssertFilePartIsUploaded(It.IsAny<TransferParameters>()), Times.Exactly(5));
    }

    [Test]
    public async Task MultipleAssertUploadIsFinishedCalls_ShouldWorkCorrectly()
    {
        // Arrange
        var totalParts = 5;
        var sessionId = "test-session-id";
        
        _mockSessionService.Setup(x => x.SessionId).Returns(sessionId);
        _mockFileTransferApiClient.Setup(x => x.AssertUploadIsFinished(It.IsAny<TransferParameters>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        for (int i = 0; i < 3; i++)
        {
            var action = async () => await _filePartUploadAsserter.AssertUploadIsFinished(_sharedFileDefinition, totalParts);
            await action.Should().NotThrowAsync();
        }

        _mockFileTransferApiClient.Verify(x => x.AssertUploadIsFinished(It.IsAny<TransferParameters>()), Times.Exactly(3));
    }
} 