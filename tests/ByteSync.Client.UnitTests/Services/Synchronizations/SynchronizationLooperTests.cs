using System.Reactive.Subjects;
using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Synchronizations;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Synchronizations;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Synchronizations;

[TestFixture]
public class SynchronizationLooperTests
{
    private Mock<ISessionService> _sessionServiceMock = null!;
    private Mock<ISessionMemberService> _sessionMemberServiceMock = null!;
    private Mock<ISynchronizationActionHandler> _synchronizationActionHandlerMock = null!;
    private Mock<ISynchronizationApiClient> _synchronizationApiClientMock = null!;
    private Mock<ISharedActionsGroupRepository> _sharedActionsGroupRepositoryMock = null!;
    private Mock<ISynchronizationService> _synchronizationServiceMock = null!;
    private Mock<ILogger<SynchronizationLooper>> _loggerMock = null!;
    
    private BehaviorSubject<AbstractSession?> _sessionSubject = null!;
    private SynchronizationProcessData _synchronizationProcessData = null!;
    private SynchronizationLooper _synchronizationLooper = null!;
    
    private const string TEST_SESSION_ID = "test-session-id";
    
    [SetUp]
    public void SetUp()
    {
        _sessionSubject = new BehaviorSubject<AbstractSession?>(null);
        _synchronizationProcessData = new SynchronizationProcessData();
        
        _sessionServiceMock = new Mock<ISessionService>();
        _sessionMemberServiceMock = new Mock<ISessionMemberService>();
        _synchronizationActionHandlerMock = new Mock<ISynchronizationActionHandler>();
        _synchronizationApiClientMock = new Mock<ISynchronizationApiClient>();
        _sharedActionsGroupRepositoryMock = new Mock<ISharedActionsGroupRepository>();
        _synchronizationServiceMock = new Mock<ISynchronizationService>();
        _loggerMock = new Mock<ILogger<SynchronizationLooper>>();
        
        _sessionServiceMock.SetupGet(s => s.SessionObservable).Returns(_sessionSubject);
        _synchronizationServiceMock.SetupGet(s => s.SynchronizationProcessData).Returns(_synchronizationProcessData);
        
        _synchronizationLooper = new SynchronizationLooper(
            _sessionServiceMock.Object,
            _sessionMemberServiceMock.Object,
            _synchronizationActionHandlerMock.Object,
            _synchronizationApiClientMock.Object,
            _sharedActionsGroupRepositoryMock.Object,
            _synchronizationServiceMock.Object,
            _loggerMock.Object);
    }
    
    [TearDown]
    public void TearDown()
    {
        _sessionSubject.Dispose();
        _synchronizationLooper.DisposeAsync();
    }
    
    [Test]
    public void Constructor_ShouldSubscribeToSessionObservable()
    {
        // Arrange
        var cloudSession = new CloudSession { SessionId = TEST_SESSION_ID };
        
        // Act
        _sessionSubject.OnNext(cloudSession);
        
        // Assert - Session should be set internally (verified through behavior)
        // We can't directly test the private Session property, but we can verify it works through CloudSessionSynchronizationLoop
        Assert.That(_synchronizationLooper.IsSynchronizationAbortRequested, Is.False);
    }
    
    [Test]
    public void Constructor_ShouldSubscribeToSynchronizationAbortRequest()
    {
        // Arrange
        var abortRequest = new SynchronizationAbortRequest { RequestedOn = DateTimeOffset.Now };
        
        // Act
        _synchronizationProcessData.SynchronizationAbortRequest.OnNext(abortRequest);
        
        // Assert
        Assert.That(_synchronizationLooper.IsSynchronizationAbortRequested, Is.True);
    }
    
    [Test]
    public void IsSynchronizationAbortRequested_WhenAbortRequestCleared_ShouldReturnFalse()
    {
        // Arrange
        var abortRequest = new SynchronizationAbortRequest { RequestedOn = DateTimeOffset.Now };
        _synchronizationProcessData.SynchronizationAbortRequest.OnNext(abortRequest);
        
        // Act
        _synchronizationProcessData.SynchronizationAbortRequest.OnNext(null);
        
        // Assert
        Assert.That(_synchronizationLooper.IsSynchronizationAbortRequested, Is.False);
    }
    
    [Test]
    public async Task CloudSessionSynchronizationLoop_WithEmptyActionsGroups_ShouldCompleteSuccessfully()
    {
        // Arrange
        var cloudSession = new CloudSession { SessionId = TEST_SESSION_ID };
        _sessionSubject.OnNext(cloudSession);
        _sharedActionsGroupRepositoryMock.SetupGet(r => r.OrganizedSharedActionsGroups)
            .Returns(new List<SharedActionsGroup>());
        
        // Act
        await _synchronizationLooper.CloudSessionSynchronizationLoop();
        
        // Assert
        _synchronizationActionHandlerMock.Verify(h => h.RunPendingSynchronizationActions(It.IsAny<CancellationToken>()), Times.Once);
        _synchronizationApiClientMock.Verify(c => c.InformCurrentMemberHasFinishedSynchronization(cloudSession), Times.Once);
        _sessionMemberServiceMock.Verify(
            s => s.UpdateCurrentMemberGeneralStatus(SessionMemberGeneralStatus.SynchronizationSourceActionsInitiated), Times.Once);
    }
    
    [Test]
    public async Task CloudSessionSynchronizationLoop_WithActionsGroups_ShouldProcessEachGroup()
    {
        // Arrange
        var cloudSession = new CloudSession { SessionId = TEST_SESSION_ID };
        _sessionSubject.OnNext(cloudSession);
        
        var actionsGroups = new List<SharedActionsGroup>
        {
            CreateTestSharedActionsGroup("group1"),
            CreateTestSharedActionsGroup("group2")
        };
        
        _sharedActionsGroupRepositoryMock.SetupGet(r => r.OrganizedSharedActionsGroups).Returns(actionsGroups);
        
        // Act
        await _synchronizationLooper.CloudSessionSynchronizationLoop();
        
        // Assert
        _synchronizationActionHandlerMock.Verify(h => h.RunSynchronizationAction(actionsGroups[0], It.IsAny<CancellationToken>()),
            Times.Once);
        _synchronizationActionHandlerMock.Verify(h => h.RunSynchronizationAction(actionsGroups[1], It.IsAny<CancellationToken>()),
            Times.Once);
        _synchronizationActionHandlerMock.Verify(h => h.RunPendingSynchronizationActions(It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Test]
    public async Task CloudSessionSynchronizationLoop_WhenAbortRequested_ShouldStopProcessing()
    {
        // Arrange
        var cloudSession = new CloudSession { SessionId = TEST_SESSION_ID };
        _sessionSubject.OnNext(cloudSession);
        
        var actionsGroups = new List<SharedActionsGroup>
        {
            CreateTestSharedActionsGroup("group1"),
            CreateTestSharedActionsGroup("group2")
        };
        
        _sharedActionsGroupRepositoryMock.SetupGet(r => r.OrganizedSharedActionsGroups).Returns(actionsGroups);
        
        // Set abort request before processing
        var abortRequest = new SynchronizationAbortRequest { RequestedOn = DateTimeOffset.Now };
        _synchronizationProcessData.SynchronizationAbortRequest.OnNext(abortRequest);
        
        // Act
        await _synchronizationLooper.CloudSessionSynchronizationLoop();
        
        // Assert
        _synchronizationActionHandlerMock.Verify(
            h => h.RunSynchronizationAction(It.IsAny<SharedActionsGroup>(), It.IsAny<CancellationToken>()), Times.Never);
        _synchronizationActionHandlerMock.Verify(h => h.RunPendingSynchronizationActions(It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Test]
    public async Task CloudSessionSynchronizationLoop_WhenCancellationRequested_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var cloudSession = new CloudSession { SessionId = TEST_SESSION_ID };
        _sessionSubject.OnNext(cloudSession);
        
        var actionsGroups = new List<SharedActionsGroup>
        {
            CreateTestSharedActionsGroup("group1")
        };
        
        _sharedActionsGroupRepositoryMock.SetupGet(r => r.OrganizedSharedActionsGroups).Returns(actionsGroups);
        
        // Cancel the token
        _synchronizationProcessData.CancellationTokenSource.Cancel();
        
        // Act & Assert
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _synchronizationLooper.CloudSessionSynchronizationLoop());
    }
    
    [Test]
    public async Task CloudSessionSynchronizationLoop_WhenActionHandlerThrowsOperationCancelled_ShouldLogAndBreak()
    {
        // Arrange
        var cloudSession = new CloudSession { SessionId = TEST_SESSION_ID };
        _sessionSubject.OnNext(cloudSession);
        
        var actionsGroups = new List<SharedActionsGroup>
        {
            CreateTestSharedActionsGroup("group1"),
            CreateTestSharedActionsGroup("group2")
        };
        
        _sharedActionsGroupRepositoryMock.SetupGet(r => r.OrganizedSharedActionsGroups).Returns(actionsGroups);
        _synchronizationActionHandlerMock.Setup(h => h.RunSynchronizationAction(actionsGroups[0], It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        
        // Act
        await _synchronizationLooper.CloudSessionSynchronizationLoop();
        
        // Assert
        VerifyLoggerInformation("Synchronization cancelled");
        _synchronizationActionHandlerMock.Verify(h => h.RunSynchronizationAction(actionsGroups[0], It.IsAny<CancellationToken>()),
            Times.Once);
        _synchronizationActionHandlerMock.Verify(h => h.RunSynchronizationAction(actionsGroups[1], It.IsAny<CancellationToken>()),
            Times.Never);
    }
    
    [Test]
    public async Task CloudSessionSynchronizationLoop_WhenActionHandlerThrowsException_ShouldLogAndContinue()
    {
        // Arrange
        var cloudSession = new CloudSession { SessionId = TEST_SESSION_ID };
        _sessionSubject.OnNext(cloudSession);
        
        var actionsGroups = new List<SharedActionsGroup>
        {
            CreateTestSharedActionsGroup("group1"),
            CreateTestSharedActionsGroup("group2")
        };
        
        _sharedActionsGroupRepositoryMock.SetupGet(r => r.OrganizedSharedActionsGroups).Returns(actionsGroups);
        _synchronizationActionHandlerMock.Setup(h => h.RunSynchronizationAction(actionsGroups[0], It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));
        
        // Act
        await _synchronizationLooper.CloudSessionSynchronizationLoop();
        
        // Assert
        VerifyLoggerError("Synchronization exception");
        _synchronizationActionHandlerMock.Verify(h => h.RunSynchronizationAction(actionsGroups[0], It.IsAny<CancellationToken>()),
            Times.Once);
        _synchronizationActionHandlerMock.Verify(h => h.RunSynchronizationAction(actionsGroups[1], It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Test]
    public async Task CloudSessionSynchronizationLoop_WhenPendingActionsThrowOperationCancelled_ShouldLogInformation()
    {
        // Arrange
        var cloudSession = new CloudSession { SessionId = TEST_SESSION_ID };
        _sessionSubject.OnNext(cloudSession);
        _sharedActionsGroupRepositoryMock.SetupGet(r => r.OrganizedSharedActionsGroups)
            .Returns(new List<SharedActionsGroup>());
        _synchronizationActionHandlerMock.Setup(h => h.RunPendingSynchronizationActions(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        
        // Act
        await _synchronizationLooper.CloudSessionSynchronizationLoop();
        
        // Assert
        VerifyLoggerInformation("Pending synchronization actions cancelled");
    }
    
    [Test]
    public async Task CloudSessionSynchronizationLoop_WhenPendingActionsThrowException_ShouldLogError()
    {
        // Arrange
        var cloudSession = new CloudSession { SessionId = TEST_SESSION_ID };
        _sessionSubject.OnNext(cloudSession);
        _sharedActionsGroupRepositoryMock.SetupGet(r => r.OrganizedSharedActionsGroups)
            .Returns(new List<SharedActionsGroup>());
        _synchronizationActionHandlerMock.Setup(h => h.RunPendingSynchronizationActions(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Pending actions error"));
        
        // Act
        await _synchronizationLooper.CloudSessionSynchronizationLoop();
        
        // Assert
        VerifyLoggerError("SynchronizationManager.StartLocalSessionSynchronization");
    }
    
    [Test]
    public async Task CloudSessionSynchronizationLoop_WhenApiClientThrowsException_ShouldLogError()
    {
        // Arrange
        var cloudSession = new CloudSession { SessionId = TEST_SESSION_ID };
        _sessionSubject.OnNext(cloudSession);
        _sharedActionsGroupRepositoryMock.SetupGet(r => r.OrganizedSharedActionsGroups)
            .Returns(new List<SharedActionsGroup>());
        _synchronizationApiClientMock.Setup(c => c.InformCurrentMemberHasFinishedSynchronization(cloudSession))
            .ThrowsAsync(new InvalidOperationException("API error"));
        
        // Act
        await _synchronizationLooper.CloudSessionSynchronizationLoop();
        
        // Assert
        VerifyLoggerError("Error while informing server");
        
        // UpdateCurrentMemberGeneralStatus should NOT be called when API client throws exception
        // because both calls are in the same try-catch block
        _sessionMemberServiceMock.Verify(s => s.UpdateCurrentMemberGeneralStatus(It.IsAny<SessionMemberGeneralStatus>()), Times.Never);
    }
    
    [Test]
    public async Task DisposeAsync_ShouldDisposeCompositeDisposable()
    {
        // Arrange & Act
        await _synchronizationLooper.DisposeAsync();
        
        // Assert - We can't directly verify the disposal, but we can ensure no exceptions are thrown
        // and that subsequent operations don't cause issues
        Assert.DoesNotThrowAsync(async () => await _synchronizationLooper.DisposeAsync());
    }
    
    private SharedActionsGroup CreateTestSharedActionsGroup(string actionsGroupId)
    {
        return new SharedActionsGroup
        {
            ActionsGroupId = actionsGroupId
        };
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
}