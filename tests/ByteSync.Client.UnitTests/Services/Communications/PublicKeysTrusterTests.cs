using System.Collections.ObjectModel;
using System.Text;
using ByteSync.Business.Communications.PublicKeysTrusting;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.Trust.Connections;
using ByteSync.Common.Business.Versions;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Services.Communications;
using ByteSync.TestsCommon;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Communications;

[TestFixture]
public class PublicKeysTrusterTests : AbstractTester
{
    private Mock<IEnvironmentService> _environmentService = null!;
    private Mock<ITrustApiClient> _trustApiClient = null!;
    private Mock<IPublicKeysManager> _publicKeysManager = null!;
    private Mock<ITrustProcessPublicKeysRepository> _trustProcessPublicKeysRepository = null!;
    private Mock<IDialogService> _dialogService = null!;
    private Mock<IFlyoutElementViewModelFactory> _flyoutElementViewModelFactory = null!;
    private Mock<ISessionMemberApiClient> _sessionMemberApiClient = null!;
    private Mock<ILogger<PublicKeysTruster>> _logger = null!;
    private PublicKeysTruster _publicKeysTruster = null!;
    
    [SetUp]
    public void SetUp()
    {
        _environmentService = new Mock<IEnvironmentService>();
        _trustApiClient = new Mock<ITrustApiClient>();
        _publicKeysManager = new Mock<IPublicKeysManager>();
        _trustProcessPublicKeysRepository = new Mock<ITrustProcessPublicKeysRepository>();
        _dialogService = new Mock<IDialogService>();
        _flyoutElementViewModelFactory = new Mock<IFlyoutElementViewModelFactory>();
        _sessionMemberApiClient = new Mock<ISessionMemberApiClient>();
        _logger = new Mock<ILogger<PublicKeysTruster>>();
        
        _environmentService
            .Setup(e => e.ClientId)
            .Returns("TestClientId");
        
        _environmentService
            .Setup(e => e.ClientInstanceId)
            .Returns("TestClientInstanceId");
        
        _publicKeysManager
            .Setup(m => m.GetMyPublicKeyInfo())
            .Returns(new PublicKeyInfo
            {
                ClientId = "TestClientId",
                PublicKey = Encoding.UTF8.GetBytes("TestPublicKey"),
                ProtocolVersion = ProtocolVersion.CURRENT
            });
        
        _publicKeysTruster = new PublicKeysTruster(
            _environmentService.Object,
            _trustApiClient.Object,
            _publicKeysManager.Object,
            _trustProcessPublicKeysRepository.Object,
            _dialogService.Object,
            _flyoutElementViewModelFactory.Object,
            _sessionMemberApiClient.Object,
            _logger.Object
        );
    }
    
    
    [Test]
    public async Task OnTrustPublicKeyRequestedAsync_WithIncompatibleProtocolVersion_ShouldThrowInvalidOperationException()
    {
        var sessionId = "TestSessionId";
        var incompatibleVersion = 2;
        var joinerInstanceId = "JoinerInstanceId";
        
        var myPublicKeyCheckData = new PublicKeyCheckData
        {
            IssuerPublicKeyInfo = new PublicKeyInfo
            {
                ClientId = "TestClientId",
                PublicKey = Encoding.UTF8.GetBytes("TestPublicKey"),
                ProtocolVersion = ProtocolVersion.CURRENT
            },
            Salt = "TestSalt123",
            OtherPartyPublicKeyInfo = new PublicKeyInfo
            {
                ClientId = "JoinerClientId",
                PublicKey = Encoding.UTF8.GetBytes("JoinerPublicKey"),
                ProtocolVersion = ProtocolVersion.CURRENT
            }
        };
        
        var joinerPublicKeyCheckData = new PublicKeyCheckData
        {
            IssuerPublicKeyInfo = new PublicKeyInfo
            {
                ClientId = "JoinerClientId",
                PublicKey = Encoding.UTF8.GetBytes("JoinerPublicKey"),
                ProtocolVersion = incompatibleVersion
            },
            IssuerClientInstanceId = joinerInstanceId,
            Salt = "TestSalt123",
            ProtocolVersion = incompatibleVersion
        };
        
        _trustProcessPublicKeysRepository
            .Setup(r => r.GetLocalPublicKeyCheckData(sessionId, joinerInstanceId))
            .ReturnsAsync(myPublicKeyCheckData);
        
        var requestParameters = new RequestTrustProcessParameters(sessionId, joinerPublicKeyCheckData, joinerInstanceId);
        
        await FluentActions.Invoking(async () => await _publicKeysTruster.OnTrustPublicKeyRequestedAsync(requestParameters))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Protocol version incompatible");
    }
    
    [Test]
    public async Task OnTrustPublicKeyRequestedAsync_WithCompatibleProtocolVersion_ShouldNotThrow()
    {
        var sessionId = "TestSessionId";
        var joinerInstanceId = "JoinerInstanceId";
        
        var myPublicKeyCheckData = new PublicKeyCheckData
        {
            IssuerPublicKeyInfo = new PublicKeyInfo
            {
                ClientId = "TestClientId",
                PublicKey = Encoding.UTF8.GetBytes("TestPublicKey"),
                ProtocolVersion = ProtocolVersion.CURRENT
            },
            Salt = "TestSalt123",
            OtherPartyPublicKeyInfo = new PublicKeyInfo
            {
                ClientId = "JoinerClientId",
                PublicKey = Encoding.UTF8.GetBytes("JoinerPublicKey"),
                ProtocolVersion = ProtocolVersion.CURRENT
            }
        };
        
        var joinerPublicKeyCheckData = new PublicKeyCheckData
        {
            IssuerPublicKeyInfo = new PublicKeyInfo
            {
                ClientId = "JoinerClientId",
                PublicKey = Encoding.UTF8.GetBytes("JoinerPublicKey"),
                ProtocolVersion = ProtocolVersion.CURRENT
            },
            IssuerClientInstanceId = joinerInstanceId,
            Salt = "TestSalt123",
            ProtocolVersion = ProtocolVersion.CURRENT
        };
        
        var peerTrustProcessData = new PeerTrustProcessData("JoinerClientId");
        
        _trustProcessPublicKeysRepository
            .Setup(r => r.GetLocalPublicKeyCheckData(sessionId, joinerInstanceId))
            .ReturnsAsync(myPublicKeyCheckData);
        
        _trustProcessPublicKeysRepository
            .Setup(r => r.ResetPeerTrustProcessData(sessionId, "JoinerClientId"))
            .ReturnsAsync(peerTrustProcessData);
        
        var requestParameters = new RequestTrustProcessParameters(sessionId, joinerPublicKeyCheckData, joinerInstanceId);
        
        await FluentActions.Invoking(async () => await _publicKeysTruster.OnTrustPublicKeyRequestedAsync(requestParameters))
            .Should()
            .NotThrowAsync();
    }
    
    [Test]
    public async Task TrustAllMembersPublicKeys_WithIncompatibleProtocolVersion_ShouldReturnIncompatibleProtocolVersion()
    {
        var sessionId = "TestSessionId";
        var incompatibleVersion = 2;
        
        var startTrustCheckResult = new StartTrustCheckResult
        {
            IsOK = true,
            MembersInstanceIds = ["MemberInstanceId"]
        };
        
        _trustApiClient
            .Setup(c => c.StartTrustCheck(It.Is<TrustCheckParameters>(p =>
                    p.SessionId == sessionId &&
                    p.ProtocolVersion == ProtocolVersion.CURRENT),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(startTrustCheckResult);
        
        _trustProcessPublicKeysRepository
            .Setup(r => r.ResetJoinerTrustProcessData(sessionId))
            .Returns(Task.CompletedTask);
        
        _trustProcessPublicKeysRepository
            .Setup(r => r.SetExpectedPublicKeyCheckDataCount(sessionId, It.IsAny<List<string>>()))
            .Returns(Task.CompletedTask);
        
        var trustProcessData = new TrustProcessPublicKeysData(sessionId);
        trustProcessData.JoinerTrustProcessData.WaitForAllPublicKeyCheckDatasReceived.Set();
        
        _trustProcessPublicKeysRepository
            .Setup(r => r.WaitAsync(sessionId, It.IsAny<Func<TrustProcessPublicKeysData, EventWaitHandle>>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);
        
        var incompatiblePublicKeyCheckData = new PublicKeyCheckData
        {
            IssuerPublicKeyInfo = new PublicKeyInfo
            {
                ClientId = "MemberClientId",
                PublicKey = Encoding.UTF8.GetBytes("MemberPublicKey"),
                ProtocolVersion = incompatibleVersion
            },
            IssuerClientInstanceId = "MemberInstanceId",
            ProtocolVersion = incompatibleVersion,
            Salt = "TestSalt123"
        };
        
        _trustProcessPublicKeysRepository
            .Setup(r => r.GetReceivedPublicKeyCheckData(sessionId))
            .ReturnsAsync(new ReadOnlyCollection<PublicKeyCheckData>(new List<PublicKeyCheckData> { incompatiblePublicKeyCheckData }));
        
        var result = await _publicKeysTruster.TrustAllMembersPublicKeys(sessionId);
        
        result.Status.Should().Be(JoinSessionStatus.IncompatibleProtocolVersion);
    }
    
    [Test]
    public async Task TrustAllMembersPublicKeys_WithCompatibleProtocolVersion_ShouldReturnProcessingNormally()
    {
        var sessionId = "TestSessionId";
        
        var startTrustCheckResult = new StartTrustCheckResult
        {
            IsOK = true,
            MembersInstanceIds = ["MemberInstanceId"]
        };
        
        _sessionMemberApiClient
            .Setup(c => c.GetMembersClientInstanceIds(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["MemberInstanceId"]);
        
        _trustApiClient
            .Setup(c => c.StartTrustCheck(It.Is<TrustCheckParameters>(p =>
                    p.SessionId == sessionId &&
                    p.ProtocolVersion == ProtocolVersion.CURRENT),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(startTrustCheckResult);
        
        _trustProcessPublicKeysRepository
            .Setup(r => r.ResetJoinerTrustProcessData(sessionId))
            .Returns(Task.CompletedTask);
        
        _trustProcessPublicKeysRepository
            .Setup(r => r.SetExpectedPublicKeyCheckDataCount(sessionId, It.IsAny<List<string>>()))
            .Returns(Task.CompletedTask);
        
        var trustProcessData = new TrustProcessPublicKeysData(sessionId);
        trustProcessData.JoinerTrustProcessData.WaitForAllPublicKeyCheckDatasReceived.Set();
        
        _trustProcessPublicKeysRepository
            .Setup(r => r.WaitAsync(sessionId, It.IsAny<Func<TrustProcessPublicKeysData, EventWaitHandle>>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);
        
        var compatiblePublicKeyCheckData = new PublicKeyCheckData
        {
            IssuerPublicKeyInfo = new PublicKeyInfo
            {
                ClientId = "MemberClientId",
                PublicKey = Encoding.UTF8.GetBytes("MemberPublicKey"),
                ProtocolVersion = ProtocolVersion.CURRENT
            },
            IssuerClientInstanceId = "MemberInstanceId",
            ProtocolVersion = ProtocolVersion.CURRENT,
            Salt = "TestSalt123",
            OtherPartyCheckResponse = true
        };
        
        _trustProcessPublicKeysRepository
            .Setup(r => r.GetReceivedPublicKeyCheckData(sessionId))
            .ReturnsAsync(new ReadOnlyCollection<PublicKeyCheckData>(new List<PublicKeyCheckData> { compatiblePublicKeyCheckData }));
        
        _publicKeysManager
            .Setup(m => m.IsTrusted(It.IsAny<PublicKeyCheckData>()))
            .Returns(true);
        
        _trustProcessPublicKeysRepository
            .Setup(r => r.SetFullyTrusted(sessionId, compatiblePublicKeyCheckData))
            .Returns(Task.CompletedTask);
        
        var result = await _publicKeysTruster.TrustAllMembersPublicKeys(sessionId);
        
        result.Status.Should().Be(JoinSessionStatus.ProcessingNormally);
    }
    
    [Test]
    public async Task InitiateAndWaitForTrustCheck_ShouldIncludeProtocolVersionInTrustCheckParameters()
    {
        var sessionId = "TestSessionId";
        var memberInstanceId = "MemberInstanceId";
        
        var startTrustCheckResult = new StartTrustCheckResult
        {
            IsOK = true,
            MembersInstanceIds = [memberInstanceId]
        };
        
        TrustCheckParameters? capturedParameters = null;
        
        _trustApiClient
            .Setup(c => c.StartTrustCheck(It.IsAny<TrustCheckParameters>(), It.IsAny<CancellationToken>()))
            .Callback<TrustCheckParameters, CancellationToken>((p, _) => capturedParameters = p)
            .ReturnsAsync(startTrustCheckResult);
        
        _trustProcessPublicKeysRepository
            .Setup(r => r.ResetJoinerTrustProcessData(sessionId))
            .Returns(Task.CompletedTask);
        
        _trustProcessPublicKeysRepository
            .Setup(r => r.SetExpectedPublicKeyCheckDataCount(sessionId, It.IsAny<List<string>>()))
            .Returns(Task.CompletedTask);
        
        var trustProcessData = new TrustProcessPublicKeysData(sessionId);
        trustProcessData.JoinerTrustProcessData.WaitForAllPublicKeyCheckDatasReceived.Set();
        
        _trustProcessPublicKeysRepository
            .Setup(r => r.WaitAsync(sessionId, It.IsAny<Func<TrustProcessPublicKeysData, EventWaitHandle>>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);
        
        _trustProcessPublicKeysRepository
            .Setup(r => r.GetReceivedPublicKeyCheckData(sessionId))
            .ReturnsAsync(new ReadOnlyCollection<PublicKeyCheckData>(new List<PublicKeyCheckData>()));
        
        await _publicKeysTruster.TrustAllMembersPublicKeys(sessionId);
        
        capturedParameters.Should().NotBeNull();
        capturedParameters!.ProtocolVersion.Should().Be(ProtocolVersion.CURRENT);
    }
    
    [Test]
    public async Task OnPublicKeyCheckDataAskedAsync_ShouldPropagateProtocolVersion()
    {
        var sessionId = "TestSessionId";
        var clientInstanceId = "ClientInstanceId";
        var publicKeyInfo = new PublicKeyInfo
        {
            ClientId = "OtherClientId",
            PublicKey = Encoding.UTF8.GetBytes("OtherPublicKey"),
            ProtocolVersion = ProtocolVersion.CURRENT
        };
        
        PublicKeyCheckData? capturedPublicKeyCheckData = null;
        
        _publicKeysManager
            .Setup(m => m.IsTrusted(publicKeyInfo))
            .Returns(false);
        
        _publicKeysManager
            .Setup(m => m.BuildMemberPublicKeyCheckData(publicKeyInfo, false))
            .Returns((PublicKeyInfo pkInfo, bool _) =>
            {
                var checkData = new PublicKeyCheckData
                {
                    IssuerPublicKeyInfo = _publicKeysManager.Object.GetMyPublicKeyInfo(),
                    OtherPartyPublicKeyInfo = pkInfo,
                    Salt = "TestSalt123",
                    ProtocolVersion = ProtocolVersion.CURRENT
                };
                capturedPublicKeyCheckData = checkData;
                
                return checkData;
            });
        
        _trustProcessPublicKeysRepository
            .Setup(r => r.StoreLocalPublicKeyCheckData(sessionId, clientInstanceId, It.IsAny<PublicKeyCheckData>()))
            .Returns(Task.CompletedTask);
        
        _trustApiClient
            .Setup(c => c.GiveMemberPublicKeyCheckData(It.IsAny<GiveMemberPublicKeyCheckDataParameters>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        await _publicKeysTruster.OnPublicKeyCheckDataAskedAsync((sessionId, clientInstanceId, publicKeyInfo));
        
        capturedPublicKeyCheckData.Should().NotBeNull();
        capturedPublicKeyCheckData!.ProtocolVersion.Should().Be(ProtocolVersion.CURRENT);
    }
}