using System.Reactive.Subjects;
using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Misc;
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
    private Mock<ISessionService> _sessionServiceMock = null!;
    private Mock<ISessionMemberService> _sessionMemberServiceMock = null!;
    private Mock<ISynchronizationApiClient> _synchronizationApiClientMock = null!;
    private Mock<ISynchronizationLooperFactory> _synchronizationLooperFactoryMock = null!;
    private Mock<ITimeTrackingCache> _timeTrackingCacheMock = null!;
    private Mock<ILogger<SynchronizationService>> _loggerMock = null!;

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
    public async Task OnSynchronizationStarted_SetsStatus_StartsTimer_PublishesStart()
    {
        var sessionObservable = new BehaviorSubject<AbstractSession?>(new CloudSession());
        var sessionStatusObservable = new BehaviorSubject<SessionStatus>(SessionStatus.Preparation);
        _sessionServiceMock.SetupGet(x => x.SessionObservable).Returns(sessionObservable);
        _sessionServiceMock.SetupGet(x => x.SessionStatusObservable).Returns(sessionStatusObservable);
        _sessionServiceMock.SetupGet(x => x.SessionId).Returns("sid-1");

        var timeComputer = new Mock<ITimeTrackingComputer>();
        _timeTrackingCacheMock
            .Setup(x => x.GetTimeTrackingComputer("sid-1", TimeTrackingComputerType.Synchronization))
            .ReturnsAsync(timeComputer.Object);

        var svc = new SynchronizationService(_sessionServiceMock.Object, _sessionMemberServiceMock.Object,
            _synchronizationApiClientMock.Object, _synchronizationLooperFactoryMock.Object,
            _timeTrackingCacheMock.Object, _loggerMock.Object);

        var startedAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        var sync = new Synchronization { SessionId = "sid-1", Started = startedAt };

        await svc.OnSynchronizationStarted(sync);

        _sessionServiceMock.Verify(x => x.SetSessionStatus(SessionStatus.Synchronization), Times.Once);
        timeComputer.Verify(x => x.Start(startedAt), Times.Once);
        svc.SynchronizationProcessData.SynchronizationStart.Value.Should().Be(sync);
        _sessionMemberServiceMock.Verify(x => x.UpdateCurrentMemberGeneralStatus(SessionMemberGeneralStatus.SynchronizationRunning),
            Times.Once);
    }

    [Test]
    public async Task OnSynchronizationUpdated_WhenEnded_StopsTimer_PublishesEnd_UpdatesMemberStatus()
    {
        var sessionObservable = new BehaviorSubject<AbstractSession?>(new CloudSession());
        var sessionStatusObservable = new BehaviorSubject<SessionStatus>(SessionStatus.Preparation);
        _sessionServiceMock.SetupGet(x => x.SessionObservable).Returns(sessionObservable);
        _sessionServiceMock.SetupGet(x => x.SessionStatusObservable).Returns(sessionStatusObservable);
        _sessionServiceMock.SetupGet(x => x.SessionId).Returns("sid-end");

        var timeComputer = new Mock<ITimeTrackingComputer>();
        _timeTrackingCacheMock
            .Setup(x => x.GetTimeTrackingComputer("sid-end", TimeTrackingComputerType.Synchronization))
            .ReturnsAsync(timeComputer.Object);

        var svc = new SynchronizationService(_sessionServiceMock.Object, _sessionMemberServiceMock.Object,
            _synchronizationApiClientMock.Object, _synchronizationLooperFactoryMock.Object,
            _timeTrackingCacheMock.Object, _loggerMock.Object);

        var endedAt = DateTimeOffset.UtcNow;
        var sync = new Synchronization { SessionId = "sid-end", Ended = endedAt, EndStatus = SynchronizationEndStatuses.Regular };

        await svc.OnSynchronizationUpdated(sync);

        timeComputer.Verify(x => x.Stop(), Times.Once);
        var end = svc.SynchronizationProcessData.SynchronizationEnd.Value;
        end.Should().NotBeNull();
        end.SessionId.Should().Be("sid-end");
        end.FinishedOn.Should().Be(endedAt);
        end.Status.Should().Be(SynchronizationEndStatuses.Regular);
        _sessionMemberServiceMock.Verify(x => x.UpdateCurrentMemberGeneralStatus(SessionMemberGeneralStatus.SynchronizationFinished),
            Times.Once);
    }

    [Test]
    public async Task OnSynchronizationUpdated_WhenAbortRequested_PublishesAbortRequest()
    {
        var sessionObservable = new BehaviorSubject<AbstractSession?>(new CloudSession());
        var sessionStatusObservable = new BehaviorSubject<SessionStatus>(SessionStatus.Preparation);
        _sessionServiceMock.SetupGet(x => x.SessionObservable).Returns(sessionObservable);
        _sessionServiceMock.SetupGet(x => x.SessionStatusObservable).Returns(sessionStatusObservable);
        _sessionServiceMock.SetupGet(x => x.SessionId).Returns("sid-abort");

        var svc = new SynchronizationService(_sessionServiceMock.Object, _sessionMemberServiceMock.Object,
            _synchronizationApiClientMock.Object, _synchronizationLooperFactoryMock.Object,
            _timeTrackingCacheMock.Object, _loggerMock.Object);

        var when = DateTimeOffset.UtcNow;
        var by = new List<string> { "c1", "c2" };
        var sync = new Synchronization { SessionId = "sid-abort", AbortRequestedOn = when, AbortRequestedBy = by };

        await svc.OnSynchronizationUpdated(sync);

        var abort = svc.SynchronizationProcessData.SynchronizationAbortRequest.Value;
        abort.Should().NotBeNull();
        abort.SessionId.Should().Be("sid-abort");
        abort.RequestedOn.Should().Be(when);
        abort.RequestedBy.Should().BeEquivalentTo(by);
    }

    [Test]
    public async Task OnSynchronizationDataTransmitted_SetsTotals_AndSignalsTransmitted()
    {
        var sessionObservable = new BehaviorSubject<AbstractSession?>(new CloudSession());
        var sessionStatusObservable = new BehaviorSubject<SessionStatus>(SessionStatus.Preparation);
        _sessionServiceMock.SetupGet(x => x.SessionObservable).Returns(sessionObservable);
        _sessionServiceMock.SetupGet(x => x.SessionStatusObservable).Returns(sessionStatusObservable);

        var svc = new SynchronizationService(_sessionServiceMock.Object, _sessionMemberServiceMock.Object,
            _synchronizationApiClientMock.Object, _synchronizationLooperFactoryMock.Object,
            _timeTrackingCacheMock.Object, _loggerMock.Object);

        var data = new SharedSynchronizationStartData { TotalVolumeToProcess = 123, TotalAtomicActionsToProcess = 45 };

        await svc.OnSynchronizationDataTransmitted(data);

        svc.SynchronizationProcessData.TotalVolumeToProcess.Should().Be(123);
        svc.SynchronizationProcessData.TotalActionsToProcess.Should().Be(45);
        svc.SynchronizationProcessData.SynchronizationDataTransmitted.Value.Should().BeTrue();
    }

    [Test]
    public async Task OnSynchronizationProgressChanged_UpdatesWhenNewerVersion_AndIgnoresOlder()
    {
        var sessionObservable = new BehaviorSubject<AbstractSession?>(new CloudSession());
        var sessionStatusObservable = new BehaviorSubject<SessionStatus>(SessionStatus.Preparation);
        _sessionServiceMock.SetupGet(x => x.SessionObservable).Returns(sessionObservable);
        _sessionServiceMock.SetupGet(x => x.SessionStatusObservable).Returns(sessionStatusObservable);

        var svc = new SynchronizationService(_sessionServiceMock.Object, _sessionMemberServiceMock.Object,
            _synchronizationApiClientMock.Object, _synchronizationLooperFactoryMock.Object,
            _timeTrackingCacheMock.Object, _loggerMock.Object);

        svc.SynchronizationProcessData.TotalVolumeToProcess = 999;

        var pushV1 = new SynchronizationProgressPush
        {
            Version = 10,
            ActualUploadedVolume = 3,
            ActualDownloadedVolume = 4,
            LocalCopyTransferredVolume = 5,
            SynchronizedVolume = 6,
            FinishedActionsCount = 7,
            ErrorActionsCount = 8
        };

        await svc.OnSynchronizationProgressChanged(pushV1);

        var p1 = svc.SynchronizationProcessData.SynchronizationProgress.Value;
        p1.Should().NotBeNull();
        p1.Version.Should().Be(10);
        p1.TotalVolumeToProcess.Should().Be(999);
        p1.ActualUploadedVolume.Should().Be(3);
        p1.ActualDownloadedVolume.Should().Be(4);
        p1.LocalCopyTransferredVolume.Should().Be(5);
        p1.SynchronizedVolume.Should().Be(6);
        p1.FinishedActionsCount.Should().Be(7);
        p1.ErrorActionsCount.Should().Be(8);

        var pushOld = new SynchronizationProgressPush { Version = 9, SynchronizedVolume = 100 };
        await svc.OnSynchronizationProgressChanged(pushOld);

        var p2 = svc.SynchronizationProcessData.SynchronizationProgress.Value;
        p2!.Version.Should().Be(10);
        p2.SynchronizedVolume.Should().Be(6);
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

        var synchronizationService = new SynchronizationService(_sessionServiceMock.Object, _sessionMemberServiceMock.Object,
            _synchronizationApiClientMock.Object,
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
    public void SynchronizationStart_WhenDataTransmitted_ShouldRunInSeparateTask()
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
            .Callback(() =>
            {
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