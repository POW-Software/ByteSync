using System.Reactive.Subjects;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Controls.TimeTracking;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Synchronizations;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Sessions;

[TestFixture]
public class SynchronizationServiceTests
{
    private Mock<ISessionService> _sessionServiceMock;
    private Mock<ISessionMemberService> _sessionMemberServiceMock;
    private Mock<ISynchronizationApiClient> _synchronizationApiClientMock;
    private Mock<ISynchronizationLooperFactory> _synchronizationLooperFactoryMock;
    private Mock<ITimeTrackingCache> _timeTrackingCacheMock;
    private Mock<ILogger<SynchronizationService>> _loggerMock;
    
    [SetUp]
    public void Setup()
    {
        _sessionServiceMock = new Mock<ISessionService>();
        _sessionMemberServiceMock = new Mock<ISessionMemberService>();
        _synchronizationApiClientMock = new Mock<ISynchronizationApiClient>();
        _synchronizationLooperFactoryMock = new Mock<ISynchronizationLooperFactory>();
        _timeTrackingCacheMock = new Mock<ITimeTrackingCache>();
        _loggerMock = new Mock<ILogger<SynchronizationService>>();
    }
    
    [Test]
    public async Task AbortSynchronization_CloudSession_AbortsSynchronization()
    {
        // Arrange
        var cloudSession = new CloudSession();
        var sessionObservable = new BehaviorSubject<AbstractSession?>(cloudSession);
        _sessionServiceMock
            .SetupGet(x => x.SessionObservable)
            .Returns(sessionObservable);
        
        var sessionStatusObservable = new BehaviorSubject<SessionStatus>(SessionStatus.Preparation);
        _sessionServiceMock
            .SetupGet(x => x.SessionStatusObservable)
            .Returns(sessionStatusObservable);
        
        var synchronizationService = new SynchronizationService(_sessionServiceMock.Object, _sessionMemberServiceMock.Object, _synchronizationApiClientMock.Object, 
            _synchronizationLooperFactoryMock.Object, _timeTrackingCacheMock.Object, _loggerMock.Object);
    
        // Act
        await synchronizationService.AbortSynchronization();
    }
    
    [Test]
    public void SynchronizationStart_WhenDataTransmitted_ShouldStartSynchronizationLoop()
    {
        // Arrange
        var cloudSession = new CloudSession();
        var sessionObservable = new BehaviorSubject<AbstractSession?>(cloudSession);
        _sessionServiceMock
            .SetupGet(x => x.SessionObservable)
            .Returns(sessionObservable);
    
        var sessionStatusObservable = new BehaviorSubject<SessionStatus>(SessionStatus.Preparation);
        _sessionServiceMock
            .SetupGet(x => x.SessionStatusObservable)
            .Returns(sessionStatusObservable);

        var mockSynchronizationLooper = new Mock<ISynchronizationLooper>();
        _synchronizationLooperFactoryMock
            .Setup(x => x.CreateSynchronizationLooper())
            .Returns(mockSynchronizationLooper.Object);

        var synchronizationService = new SynchronizationService(
            _sessionServiceMock.Object, 
            _sessionMemberServiceMock.Object, 
            _synchronizationApiClientMock.Object, 
            _synchronizationLooperFactoryMock.Object, 
            _timeTrackingCacheMock.Object, 
            _loggerMock.Object
        );

        var synchronization = new Synchronization { SessionId = "test-session" };

        // Act
        synchronizationService.SynchronizationProcessData.SynchronizationStart.OnNext(synchronization);
        synchronizationService.SynchronizationProcessData.SynchronizationDataTransmitted.OnNext(true);

        // Wait until the asynchronous task can start
        Thread.Sleep(100);

        // Assert
        _synchronizationLooperFactoryMock.Verify(x => x.CreateSynchronizationLooper(), Times.Once);
        mockSynchronizationLooper.Verify(x => x.CloudSessionSynchronizationLoop(), Times.Once);
    }
    
    [Test]
    public async Task SynchronizationStart_WhenDataTransmitted_ShouldRunInSeparateTask()
    {
        // Arrange
        var cloudSession = new CloudSession();
        var sessionObservable = new BehaviorSubject<AbstractSession?>(cloudSession);
        _sessionServiceMock
            .SetupGet(x => x.SessionObservable)
            .Returns(sessionObservable);

        var sessionStatusObservable = new BehaviorSubject<SessionStatus>(SessionStatus.Preparation);
        _sessionServiceMock
            .SetupGet(x => x.SessionStatusObservable)
            .Returns(sessionStatusObservable);

        // Setup un ManualResetEvent pour capturer l'exécution de la méthode
        var executionCaptured = new ManualResetEventSlim(false);
        var executionThreadId = 0;
        
        var mockSynchronizationLooper = new Mock<ISynchronizationLooper>();
        mockSynchronizationLooper
            .Setup(x => x.CloudSessionSynchronizationLoop())
            .Callback(() => {
                executionThreadId = Environment.CurrentManagedThreadId;
                executionCaptured.Set();
            });
        
        _synchronizationLooperFactoryMock
            .Setup(x => x.CreateSynchronizationLooper())
            .Returns(mockSynchronizationLooper.Object);

        var synchronizationService = new SynchronizationService(
            _sessionServiceMock.Object,
            _sessionMemberServiceMock.Object,
            _synchronizationApiClientMock.Object,
            _synchronizationLooperFactoryMock.Object,
            _timeTrackingCacheMock.Object,
            _loggerMock.Object
        );

        var synchronization = new Synchronization { SessionId = "test-session" };
        var currentThreadId = Environment.CurrentManagedThreadId;

        // Act
        synchronizationService.SynchronizationProcessData.SynchronizationStart.OnNext(synchronization);
        synchronizationService.SynchronizationProcessData.SynchronizationDataTransmitted.OnNext(true);

        // Attendre l'exécution avec timeout
        var executed = executionCaptured.Wait(TimeSpan.FromSeconds(1));

        // Assert
        executed.Should().BeTrue();
        currentThreadId.Should().NotBe(executionThreadId);
        // Assert.IsTrue(executed, "La méthode CloudSessionSynchronizationLoop n'a pas été exécutée");
        // Assert.AreNotEqual(currentThreadId, executionThreadId, 
        //     "La méthode CloudSessionSynchronizationLoop a été exécutée sur le même thread, elle devrait être dans un Task séparé");
        
        _synchronizationLooperFactoryMock.Verify(x => x.CreateSynchronizationLooper(), Times.Once);
        mockSynchronizationLooper.Verify(x => x.CloudSessionSynchronizationLoop(), Times.Once);
    }
}