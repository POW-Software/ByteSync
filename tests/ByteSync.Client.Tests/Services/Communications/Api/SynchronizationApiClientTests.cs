using System.Threading;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Services.Communications.Api;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Communications.Api;

[TestFixture]
public class SynchronizationApiClientTests
{
    private Mock<IApiInvoker> _mockApiInvoker = null!;
    private Mock<ILogger<SynchronizationApiClient>> _mockLogger = null!;
    private SynchronizationApiClient _synchronizationApiClient = null!;

    [SetUp]
    public void SetUp()
    {
        _mockApiInvoker = new Mock<IApiInvoker>();
        _mockLogger = new Mock<ILogger<SynchronizationApiClient>>();
        
        _synchronizationApiClient = new SynchronizationApiClient(
            _mockApiInvoker.Object,
            _mockLogger.Object);
    }

    [Test]
    public async Task StartSynchronization_WithValidRequest_ShouldCallApiInvokerWithCorrectParameters()
    {
        // Arrange
        var sessionId = "test-session-id";
        var request = new SynchronizationStartRequest
        {
            SessionId = sessionId,
            ActionsGroupDefinitions = []
        };

        _mockApiInvoker
            .Setup(x => x.PostAsync($"session/{sessionId}/synchronization/start", request, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _synchronizationApiClient.StartSynchronization(request);

        // Assert
        _mockApiInvoker.Verify(
            x => x.PostAsync($"session/{sessionId}/synchronization/start", request, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task StartSynchronization_WhenApiInvokerThrows_ShouldRethrowException()
    {
        // Arrange
        var sessionId = "test-session-id";
        var request = new SynchronizationStartRequest
        {
            SessionId = sessionId,
            ActionsGroupDefinitions = []
        };
        var expectedException = new Exception("API error");

        _mockApiInvoker
            .Setup(x => x.PostAsync($"session/{sessionId}/synchronization/start", request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        Func<Task> act = async () => await _synchronizationApiClient.StartSynchronization(request);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("API error");
    }

    [Test]
    public async Task AssertLocalCopyIsDone_WithValidParameters_ShouldCallApiInvokerWithCorrectParameters()
    {
        // Arrange
        var sessionId = "test-session-id";
        var request = new SynchronizationActionRequest(["action-group-1"], "node-id");

        _mockApiInvoker
            .Setup(x => x.PostAsync($"session/{sessionId}/synchronization/localCopyIsDone", request, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _synchronizationApiClient.AssertLocalCopyIsDone(sessionId, request);

        // Assert
        _mockApiInvoker.Verify(
            x => x.PostAsync($"session/{sessionId}/synchronization/localCopyIsDone", request, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task AssertLocalCopyIsDone_WhenApiInvokerThrows_ShouldRethrowException()
    {
        // Arrange
        var sessionId = "test-session-id";
        var request = new SynchronizationActionRequest(["action-group-1"], "node-id");
        var expectedException = new Exception("API error");

        _mockApiInvoker
            .Setup(x => x.PostAsync($"session/{sessionId}/synchronization/localCopyIsDone", request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        Func<Task> act = async () => await _synchronizationApiClient.AssertLocalCopyIsDone(sessionId, request);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("API error");
    }

    [Test]
    public async Task AssertDateIsCopied_WithValidParameters_ShouldCallApiInvokerWithCorrectParameters()
    {
        // Arrange
        var sessionId = "test-session-id";
        var request = new SynchronizationActionRequest(["action-group-1"], "node-id");

        _mockApiInvoker
            .Setup(x => x.PostAsync($"session/{sessionId}/synchronization/dateIsCopied", request, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _synchronizationApiClient.AssertDateIsCopied(sessionId, request);

        // Assert
        _mockApiInvoker.Verify(
            x => x.PostAsync($"session/{sessionId}/synchronization/dateIsCopied", request, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task AssertDateIsCopied_WhenApiInvokerThrows_ShouldRethrowException()
    {
        // Arrange
        var sessionId = "test-session-id";
        var request = new SynchronizationActionRequest(["action-group-1"], "node-id");
        var expectedException = new Exception("API error");

        _mockApiInvoker
            .Setup(x => x.PostAsync($"session/{sessionId}/synchronization/dateIsCopied", request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        Func<Task> act = async () => await _synchronizationApiClient.AssertDateIsCopied(sessionId, request);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("API error");
    }

    [Test]
    public async Task AssertFileOrDirectoryIsDeleted_WithValidParameters_ShouldCallApiInvokerWithCorrectParameters()
    {
        // Arrange
        var sessionId = "test-session-id";
        var request = new SynchronizationActionRequest(["action-group-1"], "node-id");

        _mockApiInvoker
            .Setup(x => x.PostAsync($"session/{sessionId}/synchronization/fileOrDirectoryIsDeleted", request, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _synchronizationApiClient.AssertFileOrDirectoryIsDeleted(sessionId, request);

        // Assert
        _mockApiInvoker.Verify(
            x => x.PostAsync($"session/{sessionId}/synchronization/fileOrDirectoryIsDeleted", request, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task AssertFileOrDirectoryIsDeleted_WhenApiInvokerThrows_ShouldRethrowException()
    {
        // Arrange
        var sessionId = "test-session-id";
        var request = new SynchronizationActionRequest(["action-group-1"], "node-id");
        var expectedException = new Exception("API error");

        _mockApiInvoker
            .Setup(x => x.PostAsync($"session/{sessionId}/synchronization/fileOrDirectoryIsDeleted", request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        Func<Task> act = async () => await _synchronizationApiClient.AssertFileOrDirectoryIsDeleted(sessionId, request);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("API error");
    }

    [Test]
    public async Task AssertDirectoryIsCreated_WithValidParameters_ShouldCallApiInvokerWithCorrectParameters()
    {
        // Arrange
        var sessionId = "test-session-id";
        var request = new SynchronizationActionRequest(["action-group-1"], "node-id");

        _mockApiInvoker
            .Setup(x => x.PostAsync($"session/{sessionId}/synchronization/directoryIsCreated", request, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _synchronizationApiClient.AssertDirectoryIsCreated(sessionId, request);

        // Assert
        _mockApiInvoker.Verify(
            x => x.PostAsync($"session/{sessionId}/synchronization/directoryIsCreated", request, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task AssertDirectoryIsCreated_WhenApiInvokerThrows_ShouldRethrowException()
    {
        // Arrange
        var sessionId = "test-session-id";
        var request = new SynchronizationActionRequest(["action-group-1"], "node-id");
        var expectedException = new Exception("API error");

        _mockApiInvoker
            .Setup(x => x.PostAsync($"session/{sessionId}/synchronization/directoryIsCreated", request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        Func<Task> act = async () => await _synchronizationApiClient.AssertDirectoryIsCreated(sessionId, request);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("API error");
    }

    [Test]
    public async Task RequestAbortSynchronization_WithValidSessionId_ShouldCallApiInvokerWithCorrectParameters()
    {
        // Arrange
        var sessionId = "test-session-id";

        _mockApiInvoker
            .Setup(x => x.PostAsync($"session/{sessionId}/synchronization/abort", null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _synchronizationApiClient.RequestAbortSynchronization(sessionId);

        // Assert
        _mockApiInvoker.Verify(
            x => x.PostAsync($"session/{sessionId}/synchronization/abort", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task RequestAbortSynchronization_WhenApiInvokerThrows_ShouldRethrowException()
    {
        // Arrange
        var sessionId = "test-session-id";
        var expectedException = new Exception("API error");

        _mockApiInvoker
            .Setup(x => x.PostAsync($"session/{sessionId}/synchronization/abort", null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        Func<Task> act = async () => await _synchronizationApiClient.RequestAbortSynchronization(sessionId);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("API error");
    }

    [Test]
    public async Task InformCurrentMemberHasFinishedSynchronization_WithValidCloudSession_ShouldCallApiInvokerWithCorrectParameters()
    {
        // Arrange
        var cloudSession = new CloudSession("test-session-id", "creator-instance-id");

        _mockApiInvoker
            .Setup(x => x.PostAsync($"session/{cloudSession.SessionId}/synchronization/memberHasFinished", null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _synchronizationApiClient.InformCurrentMemberHasFinishedSynchronization(cloudSession);

        // Assert
        _mockApiInvoker.Verify(
            x => x.PostAsync($"session/{cloudSession.SessionId}/synchronization/memberHasFinished", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task InformCurrentMemberHasFinishedSynchronization_WhenApiInvokerThrows_ShouldRethrowException()
    {
        // Arrange
        var cloudSession = new CloudSession("test-session-id", "creator-instance-id");
        var expectedException = new Exception("API error");

        _mockApiInvoker
            .Setup(x => x.PostAsync($"session/{cloudSession.SessionId}/synchronization/memberHasFinished", null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        Func<Task> act = async () => await _synchronizationApiClient.InformCurrentMemberHasFinishedSynchronization(cloudSession);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("API error");
    }

    [Test]
    public async Task InformSynchronizationActionError_WithValidParameters_ShouldCreateRequestAndCallInformSynchronizationActionErrors()
    {
        // Arrange
        var sharedFileDefinition = new SharedFileDefinition
        {
            SessionId = "test-session-id",
            ActionsGroupIds = ["action-group-1", "action-group-2"]
        };
        var nodeId = "test-node-id";

        _mockApiInvoker
            .Setup(x => x.PostAsync($"session/{sharedFileDefinition.SessionId}/synchronization/errors/", 
                It.IsAny<SynchronizationActionRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _synchronizationApiClient.InformSynchronizationActionError(sharedFileDefinition, nodeId);

        // Assert
        _mockApiInvoker.Verify(
            x => x.PostAsync($"session/{sharedFileDefinition.SessionId}/synchronization/errors/", 
                It.IsAny<SynchronizationActionRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task InformSynchronizationActionError_WithNullNodeId_ShouldCreateRequestWithNullNodeId()
    {
        // Arrange
        var sharedFileDefinition = new SharedFileDefinition
        {
            SessionId = "test-session-id",
            ActionsGroupIds = ["action-group-1"]
        };

        _mockApiInvoker
            .Setup(x => x.PostAsync($"session/{sharedFileDefinition.SessionId}/synchronization/errors/", 
                It.IsAny<SynchronizationActionRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _synchronizationApiClient.InformSynchronizationActionError(sharedFileDefinition, null);

        // Assert
        _mockApiInvoker.Verify(
            x => x.PostAsync($"session/{sharedFileDefinition.SessionId}/synchronization/errors/", 
                It.IsAny<SynchronizationActionRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task InformSynchronizationActionErrors_WithValidParameters_ShouldCallApiInvokerWithCorrectParameters()
    {
        // Arrange
        var sessionId = "test-session-id";
        var request = new SynchronizationActionRequest(["action-group-1"], "node-id");

        _mockApiInvoker
            .Setup(x => x.PostAsync($"session/{sessionId}/synchronization/errors/", request, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _synchronizationApiClient.InformSynchronizationActionErrors(sessionId, request);

        // Assert
        _mockApiInvoker.Verify(
            x => x.PostAsync($"session/{sessionId}/synchronization/errors/", request, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task InformSynchronizationActionErrors_WhenApiInvokerThrows_ShouldRethrowException()
    {
        // Arrange
        var sessionId = "test-session-id";
        var request = new SynchronizationActionRequest(["action-group-1"], "node-id");
        var expectedException = new Exception("API error");

        _mockApiInvoker
            .Setup(x => x.PostAsync($"session/{sessionId}/synchronization/errors/", request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        Func<Task> act = async () => await _synchronizationApiClient.InformSynchronizationActionErrors(sessionId, request);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("API error");
    }
}
