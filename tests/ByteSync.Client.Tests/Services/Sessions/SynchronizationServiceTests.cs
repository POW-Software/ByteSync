using System.Reactive.Subjects;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.TimeTracking;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Synchronizations;
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

    // private SynchronizationService _synchronizationService;
    
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
        // _connectionManagerMock
        //     .Setup(x => x.HubWrapper.RequestAbortSynchronization(cloudSession.SessionId))
        //     .ReturnsAsync(new SynchronizationAbortRequest());
        // _connectionManagerMock.SetupPushHandler();
        
        var synchronizationService = new SynchronizationService(_sessionServiceMock.Object, _sessionMemberServiceMock.Object, _synchronizationApiClientMock.Object, 
            _synchronizationLooperFactoryMock.Object, _timeTrackingCacheMock.Object, _loggerMock.Object);
    
        // Act
        await synchronizationService.AbortSynchronization();

        // Assert
        // _connectionManagerMock.Verify(x => x.HubWrapper.RequestAbortSynchronization(cloudSession.SessionId), Times.Once);
        // _connectionManagerMock.Verify();
    }
}