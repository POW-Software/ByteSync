using System.Reactive.Subjects;
using ByteSync.Business.Synchronizations;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Communications.SignalR;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Communications.PushReceivers;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Communications.PushReceivers;

[TestFixture]
public class SynchronizationProgressPushReceiverTests
{
    private Subject<SynchronizationProgressPush> _synchronizationProgressUpdatedSubject = null!;
    private Mock<IHubPushHandler2> _hubPushHandlerMock = null!;
    private Mock<ISessionService> _sessionServiceMock = null!;
    private Mock<ISynchronizationService> _synchronizationServiceMock = null!;
    private Mock<ISharedActionsGroupRepository> _sharedActionsGroupRepositoryMock = null!;
    private Mock<ISynchronizationApiClient> _synchronizationApiClientMock = null!;
    private Mock<ILogger<SynchronizationProgressPushReceiver>> _loggerMock = null!;
    private SynchronizationProcessData _synchronizationProcessData = null!;
    
    // ReSharper disable once NotAccessedField.Local
    private SynchronizationProgressPushReceiver _synchronizationProgressPushReceiver = null!;

    private const string TEST_SESSION_ID = "test-session-id";

    [SetUp]
    public void SetUp()
    {
        _synchronizationProgressUpdatedSubject = new Subject<SynchronizationProgressPush>();
        
        _hubPushHandlerMock = new Mock<IHubPushHandler2>();
        _sessionServiceMock = new Mock<ISessionService>();
        _synchronizationServiceMock = new Mock<ISynchronizationService>();
        _sharedActionsGroupRepositoryMock = new Mock<ISharedActionsGroupRepository>();
        _synchronizationApiClientMock = new Mock<ISynchronizationApiClient>();
        _loggerMock = new Mock<ILogger<SynchronizationProgressPushReceiver>>();
        _synchronizationProcessData = new SynchronizationProcessData();

        _hubPushHandlerMock.SetupGet(h => h.SynchronizationProgressUpdated)
            .Returns(_synchronizationProgressUpdatedSubject);

        _synchronizationServiceMock.SetupGet(s => s.SynchronizationProcessData)
            .Returns(_synchronizationProcessData);

        _synchronizationProgressPushReceiver = new SynchronizationProgressPushReceiver(
            _hubPushHandlerMock.Object,
            _sessionServiceMock.Object,
            _synchronizationServiceMock.Object,
            _sharedActionsGroupRepositoryMock.Object,
            _synchronizationApiClientMock.Object,
            _loggerMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _synchronizationProgressUpdatedSubject.Dispose();
    }

    [Test]
    public async Task SynchronizationProgressChanged_WhenCurrentSessionIsNull_ShouldLogWarningAndReturn()
    {
        // Arrange
        _sessionServiceMock.SetupGet(s => s.CurrentSession).Returns((CloudSession?)null);
        var synchronizationProgressPush = CreateSynchronizationProgressPush(TEST_SESSION_ID);

        // Act
        _synchronizationProgressUpdatedSubject.OnNext(synchronizationProgressPush);

        // Give time for async operation to complete
        await Task.Delay(100);

        // Assert
        VerifyLoggerWarning("Received a synchronization progress push but there is no current session");
        _synchronizationServiceMock.Verify(s => s.OnSynchronizationProgressChanged(It.IsAny<SynchronizationProgressPush>()), Times.Never);
        _sharedActionsGroupRepositoryMock.Verify(r => r.OnSynchronizationProgressChanged(It.IsAny<SynchronizationProgressPush>()), Times.Never);
    }

    [Test]
    public async Task SynchronizationProgressChanged_WhenSessionIdDifferent_ShouldLogWarningAndReturn()
    {
        // Arrange
        var currentSession = new CloudSession { SessionId = TEST_SESSION_ID };
        _sessionServiceMock.SetupGet(s => s.CurrentSession).Returns(currentSession);
        var differentSessionId = "different-session-id";
        var synchronizationProgressPush = CreateSynchronizationProgressPush(differentSessionId);

        // Act
        _synchronizationProgressUpdatedSubject.OnNext(synchronizationProgressPush);

        // Give time for async operation to complete
        await Task.Delay(100);

        // Assert
        VerifyLoggerWarning("Received a synchronization progress push for a different session than the current one");
        _synchronizationServiceMock.Verify(s => s.OnSynchronizationProgressChanged(It.IsAny<SynchronizationProgressPush>()), Times.Never);
        _sharedActionsGroupRepositoryMock.Verify(r => r.OnSynchronizationProgressChanged(It.IsAny<SynchronizationProgressPush>()), Times.Never);
    }

    [Test]
    public async Task SynchronizationProgressChanged_WhenSuccessful_ShouldProcessPush()
    {
        // Arrange
        var currentSession = new CloudSession { SessionId = TEST_SESSION_ID };
        _sessionServiceMock.SetupGet(s => s.CurrentSession).Returns(currentSession);
        var synchronizationProgressPush = CreateSynchronizationProgressPush(TEST_SESSION_ID);
        
        SetupSynchronizationDataTransmittedSuccess();

        // Act
        _synchronizationProgressUpdatedSubject.OnNext(synchronizationProgressPush);

        // Give time for async operation to complete
        await Task.Delay(200);

        // Assert
        _synchronizationServiceMock.Verify(s => s.OnSynchronizationProgressChanged(synchronizationProgressPush), Times.Once);
        _sharedActionsGroupRepositoryMock.Verify(r => r.OnSynchronizationProgressChanged(synchronizationProgressPush), Times.Once);
    }

    [Test]
    public async Task SynchronizationProgressChanged_WhenTimeoutWaiting_ShouldLogErrorAndReturn()
    {
        // Arrange
        var currentSession = new CloudSession { SessionId = TEST_SESSION_ID };
        _sessionServiceMock.SetupGet(s => s.CurrentSession).Returns(currentSession);
        var synchronizationProgressPush = CreateSynchronizationProgressPush(TEST_SESSION_ID);
        
        // Create a testable receiver with short timeout
        var testableReceiver = new TestableProgressPushReceiver(
            _hubPushHandlerMock.Object,
            _sessionServiceMock.Object,
            _synchronizationServiceMock.Object,
            _sharedActionsGroupRepositoryMock.Object,
            _synchronizationApiClientMock.Object,
            _loggerMock.Object);
        
        SetupSynchronizationDataTransmittedTimeout();

        // Act
        _synchronizationProgressUpdatedSubject.OnNext(synchronizationProgressPush);

        // Give time for timeout to occur
        await Task.Delay(200);

        // Assert
        VerifyLoggerError($"Timeout waiting for synchronization data transmission ({testableReceiver.TimeoutForTest}) for session {TEST_SESSION_ID}");
        _synchronizationServiceMock.Verify(s => s.OnSynchronizationProgressChanged(It.IsAny<SynchronizationProgressPush>()), Times.Never);
        _sharedActionsGroupRepositoryMock.Verify(r => r.OnSynchronizationProgressChanged(It.IsAny<SynchronizationProgressPush>()), Times.Never);
    }

    [Test]
    public async Task SynchronizationProgressChanged_WhenCancelled_ShouldLogInformationAndReturn()
    {
        // Arrange
        var currentSession = new CloudSession { SessionId = TEST_SESSION_ID };
        _sessionServiceMock.SetupGet(s => s.CurrentSession).Returns(currentSession);
        var synchronizationProgressPush = CreateSynchronizationProgressPush(TEST_SESSION_ID);
        
        SetupSynchronizationDataTransmittedCancellation();

        // Act
        _synchronizationProgressUpdatedSubject.OnNext(synchronizationProgressPush);

        // Give time for async operation to complete
        await Task.Delay(200);

        // Assert
        VerifyLoggerInformation($"Synchronization progress processing cancelled for session {TEST_SESSION_ID}");
        _synchronizationServiceMock.Verify(s => s.OnSynchronizationProgressChanged(It.IsAny<SynchronizationProgressPush>()), Times.Never);
        _sharedActionsGroupRepositoryMock.Verify(r => r.OnSynchronizationProgressChanged(It.IsAny<SynchronizationProgressPush>()), Times.Never);
    }

    [Test]
    public async Task SynchronizationProgressChanged_WhenExceptionThrown_ShouldLogErrorAndCallAssertSynchronizationActionErrors()
    {
        // Arrange
        var currentSession = new CloudSession { SessionId = TEST_SESSION_ID };
        _sessionServiceMock.SetupGet(s => s.CurrentSession).Returns(currentSession);
        var trackingActionSummaries = new List<TrackingActionSummary>
        {
            new TrackingActionSummary { ActionsGroupId = "group1" },
            new TrackingActionSummary { ActionsGroupId = "group2" }
        };
        var synchronizationProgressPush = CreateSynchronizationProgressPush(TEST_SESSION_ID, trackingActionSummaries);
        
        SetupSynchronizationDataTransmittedSuccess();
        _synchronizationServiceMock.Setup(s => s.OnSynchronizationProgressChanged(It.IsAny<SynchronizationProgressPush>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        _synchronizationProgressUpdatedSubject.OnNext(synchronizationProgressPush);

        // Give time for async operation to complete
        await Task.Delay(200);

        // Assert
        VerifyLoggerError("Error processing synchronization progress push");
        _synchronizationApiClientMock.Verify(c => c.InformSynchronizationActionErrors(
            TEST_SESSION_ID, 
            It.Is<SynchronizationActionRequest>(list => list.ActionsGroupIds.Count == 2 && list.ActionsGroupIds.Contains("group1") && list.ActionsGroupIds.Contains("group2"))), 
            Times.Once);
    }

    [Test]
    public async Task SynchronizationProgressChanged_WhenExceptionThrownWithoutTrackingActionSummaries_ShouldLogErrorButNotCallAssertSynchronizationActionErrors()
    {
        // Arrange
        var currentSession = new CloudSession { SessionId = TEST_SESSION_ID };
        _sessionServiceMock.SetupGet(s => s.CurrentSession).Returns(currentSession);
        var synchronizationProgressPush = CreateSynchronizationProgressPush(TEST_SESSION_ID);
        
        SetupSynchronizationDataTransmittedSuccess();
        _synchronizationServiceMock.Setup(s => s.OnSynchronizationProgressChanged(It.IsAny<SynchronizationProgressPush>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        _synchronizationProgressUpdatedSubject.OnNext(synchronizationProgressPush);

        // Give time for async operation to complete
        await Task.Delay(200);

        // Assert
        VerifyLoggerError("Error processing synchronization progress push");
        _synchronizationApiClientMock.Verify(c => c.InformSynchronizationActionErrors(It.IsAny<string>(), It.IsAny<SynchronizationActionRequest>()), Times.Never);
    }

    [Test]
    public async Task SynchronizationProgressChanged_WhenAssertSynchronizationActionErrorsThrows_ShouldLogSecondError()
    {
        // Arrange
        var currentSession = new CloudSession { SessionId = TEST_SESSION_ID };
        _sessionServiceMock.SetupGet(s => s.CurrentSession).Returns(currentSession);
        var trackingActionSummaries = new List<TrackingActionSummary>
        {
            new TrackingActionSummary { ActionsGroupId = "group1" }
        };
        var synchronizationProgressPush = CreateSynchronizationProgressPush(TEST_SESSION_ID, trackingActionSummaries);
        
        SetupSynchronizationDataTransmittedSuccess();
        _synchronizationServiceMock.Setup(s => s.OnSynchronizationProgressChanged(It.IsAny<SynchronizationProgressPush>()))
            .ThrowsAsync(new Exception("Test exception"));
        _synchronizationApiClientMock
            .Setup(c => c.InformSynchronizationActionErrors(
                It.IsAny<string>(),
                It.IsAny<SynchronizationActionRequest>()))
            .ThrowsAsync(new Exception("API exception"));

        // Act
        _synchronizationProgressUpdatedSubject.OnNext(synchronizationProgressPush);

        // Give time for async operation to complete
        await Task.Delay(200);

        // Assert
        VerifyLoggerError("Error processing synchronization progress push");
        VerifyLoggerError("Error asserting synchronization action errors");
    }

    private SynchronizationProgressPush CreateSynchronizationProgressPush(string sessionId, List<TrackingActionSummary>? trackingActionSummaries = null)
    {
        return new SynchronizationProgressPush
        {
            SessionId = sessionId,
            ProcessedVolume = 1000,
            ExchangedVolume = 500,
            FinishedActionsCount = 10,
            ErrorActionsCount = 1,
            Version = DateTimeOffset.UtcNow.Ticks,
            TrackingActionSummaries = trackingActionSummaries
        };
    }

    private void SetupSynchronizationDataTransmittedSuccess()
    {
        _synchronizationProcessData.SynchronizationDataTransmitted.OnNext(true);
    }

    private void SetupSynchronizationDataTransmittedTimeout()
    {
        _synchronizationProcessData.SynchronizationDataTransmitted.OnNext(false);
    }

    private void SetupSynchronizationDataTransmittedCancellation()
    {
        _synchronizationProcessData.CancellationTokenSource.Cancel();
        _synchronizationProcessData.SynchronizationDataTransmitted.OnNext(false);
    }

    private void VerifyLoggerWarning(string message)
    {
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private void VerifyLoggerError(string message)
    {
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private void VerifyLoggerInformation(string message)
    {
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

// Test helper class that allows overriding the timeout for testing
internal class TestableProgressPushReceiver : SynchronizationProgressPushReceiver
{
    protected override TimeSpan SynchronizationDataTransmissionTimeout => TimeSpan.FromMilliseconds(50);
    
    // Public property for test assertion
    public TimeSpan TimeoutForTest => SynchronizationDataTransmissionTimeout;

    public TestableProgressPushReceiver(
        IHubPushHandler2 hubPushHandler2,
        ISessionService sessionService,
        ISynchronizationService synchronizationService,
        ISharedActionsGroupRepository sharedActionsGroupRepository,
        ISynchronizationApiClient synchronizationApiClient,
        // ReSharper disable once ContextualLoggerProblem
        ILogger<SynchronizationProgressPushReceiver> logger)
        : base(hubPushHandler2, sessionService, synchronizationService, sharedActionsGroupRepository, synchronizationApiClient, logger)
    {
    }
}
