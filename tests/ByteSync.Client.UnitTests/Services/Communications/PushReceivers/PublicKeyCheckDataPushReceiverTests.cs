using System.Reactive.Subjects;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.Versions;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.SignalR;
using ByteSync.Services.Communications.PushReceivers;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Communications.PushReceivers;

[TestFixture]
public class PublicKeyCheckDataPushReceiverTests
{
    private Subject<InformProtocolVersionIncompatibleParameters> _informProtocolVersionIncompatibleSubject = null!;
    private Mock<IHubPushHandler2> _hubPushHandlerMock = null!;
    private Mock<IPublicKeysTruster> _publicKeysTrusterMock = null!;
    private Mock<ITrustProcessPublicKeysRepository> _trustProcessPublicKeysRepositoryMock = null!;
    private Mock<IDigitalSignaturesChecker> _digitalSignaturesCheckerMock = null!;
    private Mock<ILogger<PublicKeyCheckDataPushReceiver>> _loggerMock = null!;
    
    [SetUp]
    public void SetUp()
    {
        _informProtocolVersionIncompatibleSubject = new Subject<InformProtocolVersionIncompatibleParameters>();
        
        _hubPushHandlerMock = new Mock<IHubPushHandler2>();
        _publicKeysTrusterMock = new Mock<IPublicKeysTruster>();
        _trustProcessPublicKeysRepositoryMock = new Mock<ITrustProcessPublicKeysRepository>();
        _digitalSignaturesCheckerMock = new Mock<IDigitalSignaturesChecker>();
        _loggerMock = new Mock<ILogger<PublicKeyCheckDataPushReceiver>>();
        
        _hubPushHandlerMock.SetupGet(h => h.AskPublicKeyCheckData)
            .Returns(new Subject<(string, string, PublicKeyInfo)>());
        _hubPushHandlerMock.SetupGet(h => h.GiveMemberPublicKeyCheckData)
            .Returns(new Subject<(string, PublicKeyCheckData)>());
        _hubPushHandlerMock.SetupGet(h => h.RequestTrustPublicKey)
            .Returns(new Subject<RequestTrustProcessParameters>());
        _hubPushHandlerMock.SetupGet(h => h.InformPublicKeyValidationIsFinished)
            .Returns(new Subject<PublicKeyValidationParameters>());
        _hubPushHandlerMock.SetupGet(h => h.RequestCheckDigitalSignature)
            .Returns(new Subject<DigitalSignatureCheckInfo>());
        _hubPushHandlerMock.SetupGet(h => h.InformProtocolVersionIncompatible)
            .Returns(_informProtocolVersionIncompatibleSubject);
        
        _ = new PublicKeyCheckDataPushReceiver(
            _hubPushHandlerMock.Object,
            _publicKeysTrusterMock.Object,
            _trustProcessPublicKeysRepositoryMock.Object,
            _digitalSignaturesCheckerMock.Object,
            _loggerMock.Object);
    }
    
    [Test]
    public async Task OnProtocolVersionIncompatible_ShouldCallSetProtocolVersionIncompatible()
    {
        var sessionId = "test-session-id";
        var memberClientInstanceId = "member-instance-id";
        var joinerClientInstanceId = "joiner-instance-id";
        
        var tcs = new TaskCompletionSource<bool>();
        _trustProcessPublicKeysRepositoryMock
            .Setup(r => r.SetProtocolVersionIncompatible(sessionId, memberClientInstanceId))
            .Returns(Task.CompletedTask)
            .Callback(() => tcs.TrySetResult(true));
        
        var parameters = new InformProtocolVersionIncompatibleParameters
        {
            SessionId = sessionId,
            MemberClientInstanceId = memberClientInstanceId,
            JoinerClientInstanceId = joinerClientInstanceId,
            MemberProtocolVersion = ProtocolVersion.CURRENT,
            JoinerProtocolVersion = 0
        };
        
        _informProtocolVersionIncompatibleSubject.OnNext(parameters);
        
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        
        _trustProcessPublicKeysRepositoryMock.Verify(
            r => r.SetProtocolVersionIncompatible(sessionId, memberClientInstanceId),
            Times.Once);
    }
}