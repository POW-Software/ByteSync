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
        
        _environmentService.SetupGet(e => e.Identity).Returns(new string('A', 64));

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
    public async Task FinishedValidation_SendsTheExpectedExchangeAndRecipient()
    {
        var peer = new PublicKeyCheckData { IssuerClientInstanceId = "peer-instance", Salt = "exchange-salt",
            IssuerPublicKeyInfo = new PublicKeyInfo { ClientId = "ABCD-EFG-HIJ", PublicKey = [1, 2, 3] } };
        _publicKeysManager.Setup(m => m.GetMyPublicKeyInfo()).Returns(new PublicKeyInfo { ClientId = "KLMN-OPQ-RST" });
        var pending = new ByteSync.Business.Communications.PublicKeysTrusting.PeerTrustProcessData("ABCD-EFG-HIJ");
        var context = new ByteSync.Business.TrustDataParameters(1, 1, true, "session", pending);

        await _publicKeysTruster.OnPublicKeyValidationFinished(peer, context, true);

        _trustApiClient.Verify(a => a.InformPublicKeyValidationIsFinished(It.Is<PublicKeyValidationParameters>(p =>
            p.SessionId == "session" && p.ExchangeId == peer.Salt && p.OtherPartyClientInstanceId == peer.IssuerClientInstanceId &&
            p.OtherPartyPublicKey == peer.IssuerPublicKeyInfo.PublicKey && p.IsValidated), It.IsAny<CancellationToken>()), Times.Once);
        pending.HasLocalValidatedPublicKey.Should().BeTrue();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task ReceivedConfirmation_OnlyUpdatesItsIntendedRecipient(bool intended)
    {
        var parameters = new PublicKeyValidationParameters { SessionId = "session",
            OtherPartyClientInstanceId = intended ? "TestClientInstanceId" : "other-instance" };

        await _publicKeysTruster.OnPublicKeyValidationIsFinishedAsync(parameters);

        _trustProcessPublicKeysRepository.Verify(r => r.SetOtherPartyChecked("session", parameters),
            intended ? Times.Once() : Times.Never());
    }

    [Test]
    public async Task TrustCheck_SkipsOnlyInstancesOfTheCryptographicIdentity()
    {
        const string alias = "ABCD-EFG-HIJ";
        var identity = new string('A', 64);
        var ownInstance = identity + "_" + Guid.NewGuid().ToString("D");
        var aliasInstance = alias + "_" + Guid.NewGuid().ToString("D");
        var otherInstance = new string('B', 64) + "_" + Guid.NewGuid().ToString("D");
        _environmentService.SetupGet(e => e.ClientId).Returns(alias);
        _environmentService.SetupGet(e => e.Identity).Returns(identity);
        _sessionMemberApiClient.Setup(c => c.GetMembersClientInstanceIds("session", It.IsAny<CancellationToken>()))
            .ReturnsAsync([ownInstance, aliasInstance, otherInstance]);
        _trustApiClient.Setup(c => c.StartTrustCheck(It.IsAny<TrustCheckParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StartTrustCheckResult { IsOK = false });

        await _publicKeysTruster.TrustAllMembersPublicKeys("session");

        _trustApiClient.Verify(c => c.StartTrustCheck(It.Is<TrustCheckParameters>(p =>
            p.MembersInstanceIdsToCheck.Count == 2 && p.MembersInstanceIdsToCheck.Contains(aliasInstance) &&
            p.MembersInstanceIdsToCheck.Contains(otherInstance) && !p.MembersInstanceIdsToCheck.Contains(ownInstance)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task OnTrustPublicKeyRequestedAsync_WithIncompatibleProtocolVersion_ShouldThrowInvalidOperationException()
    {
        var sessionId = "TestSessionId";
        var incompatibleVersion = ProtocolVersion.CURRENT - 1;
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
            .Setup(r => r.ResetPeerTrustProcessData(sessionId, joinerPublicKeyCheckData))
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
        var incompatibleVersion = ProtocolVersion.CURRENT - 1;
        
        _sessionMemberApiClient
            .Setup(c => c.GetMembersClientInstanceIds(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["MemberInstanceId"]);
        
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
        
        _sessionMemberApiClient
            .Setup(c => c.GetMembersClientInstanceIds(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([memberInstanceId]);
        
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
    
    [Test]
    public async Task OnPublicKeyCheckDataAskedAsync_WithIncompatibleProtocolVersion_ShouldNotRespond()
    {
        var sessionId = "TestSessionId";
        var clientInstanceId = "ClientInstanceId";
        var incompatibleVersion = 0;
        
        var publicKeyInfo = new PublicKeyInfo
        {
            ClientId = "OtherClientId",
            PublicKey = Encoding.UTF8.GetBytes("OtherPublicKey"),
            ProtocolVersion = incompatibleVersion
        };
        
        await _publicKeysTruster.OnPublicKeyCheckDataAskedAsync((sessionId, clientInstanceId, publicKeyInfo));
        
        _publicKeysManager.Verify(m => m.IsTrusted(It.IsAny<PublicKeyInfo>()), Times.Never);
        _publicKeysManager.Verify(m => m.BuildMemberPublicKeyCheckData(It.IsAny<PublicKeyInfo>(), It.IsAny<bool>()), Times.Never);
        _trustProcessPublicKeysRepository.Verify(r => r.StoreLocalPublicKeyCheckData(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PublicKeyCheckData>()), Times.Never);
        _trustApiClient.Verify(c => c.GiveMemberPublicKeyCheckData(It.IsAny<GiveMemberPublicKeyCheckDataParameters>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Test]
    public async Task OnPublicKeyCheckDataAskedAsync_WithIncompatibleProtocolVersion_ShouldLogWarning()
    {
        var sessionId = "TestSessionId";
        var clientInstanceId = "ClientInstanceId";
        var incompatibleVersion = ProtocolVersion.CURRENT - 1;
        
        var publicKeyInfo = new PublicKeyInfo
        {
            ClientId = "OtherClientId",
            PublicKey = Encoding.UTF8.GetBytes("OtherPublicKey"),
            ProtocolVersion = incompatibleVersion
        };
        
        await _publicKeysTruster.OnPublicKeyCheckDataAskedAsync((sessionId, clientInstanceId, publicKeyInfo));
        
        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Protocol version mismatch")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    
    [Test]
    public async Task OnPublicKeyCheckDataAskedAsync_WithCompatibleProtocolVersion_ShouldRespond()
    {
        var sessionId = "TestSessionId";
        var clientInstanceId = "ClientInstanceId";
        
        var publicKeyInfo = new PublicKeyInfo
        {
            ClientId = "OtherClientId",
            PublicKey = Encoding.UTF8.GetBytes("OtherPublicKey"),
            ProtocolVersion = ProtocolVersion.CURRENT
        };
        
        var memberPublicKeyCheckData = new PublicKeyCheckData
        {
            IssuerPublicKeyInfo = _publicKeysManager.Object.GetMyPublicKeyInfo(),
            OtherPartyPublicKeyInfo = publicKeyInfo,
            Salt = "TestSalt123",
            ProtocolVersion = ProtocolVersion.CURRENT
        };
        
        _publicKeysManager
            .Setup(m => m.IsTrusted(publicKeyInfo))
            .Returns(false);
        
        _publicKeysManager
            .Setup(m => m.BuildMemberPublicKeyCheckData(publicKeyInfo, false))
            .Returns(memberPublicKeyCheckData);
        
        _trustProcessPublicKeysRepository
            .Setup(r => r.StoreLocalPublicKeyCheckData(sessionId, clientInstanceId, memberPublicKeyCheckData))
            .Returns(Task.CompletedTask);
        
        _trustApiClient
            .Setup(c => c.GiveMemberPublicKeyCheckData(It.IsAny<GiveMemberPublicKeyCheckDataParameters>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        await _publicKeysTruster.OnPublicKeyCheckDataAskedAsync((sessionId, clientInstanceId, publicKeyInfo));
        
        _publicKeysManager.Verify(m => m.IsTrusted(publicKeyInfo), Times.Once);
        _publicKeysManager.Verify(m => m.BuildMemberPublicKeyCheckData(publicKeyInfo, false), Times.Once);
        _trustProcessPublicKeysRepository.Verify(r => r.StoreLocalPublicKeyCheckData(sessionId, clientInstanceId, memberPublicKeyCheckData), Times.Once);
        _trustApiClient.Verify(c => c.GiveMemberPublicKeyCheckData(It.Is<GiveMemberPublicKeyCheckDataParameters>(p =>
            p.SessionId == sessionId &&
            p.ClientInstanceId == clientInstanceId &&
            p.PublicKeyCheckData == memberPublicKeyCheckData), It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Test]
    public async Task OnPublicKeyCheckDataAskedAsync_WithIncompatibleProtocolVersion_ShouldSendNotification()
    {
        var sessionId = "TestSessionId";
        var joinerClientInstanceId = "JoinerClientInstanceId";
        var incompatibleVersion = 0;
        
        var publicKeyInfo = new PublicKeyInfo
        {
            ClientId = "JoinerClientId",
            PublicKey = Encoding.UTF8.GetBytes("JoinerPublicKey"),
            ProtocolVersion = incompatibleVersion
        };
        
        _trustApiClient
            .Setup(c => c.InformProtocolVersionIncompatible(It.IsAny<InformProtocolVersionIncompatibleParameters>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        await _publicKeysTruster.OnPublicKeyCheckDataAskedAsync((sessionId, joinerClientInstanceId, publicKeyInfo));
        
        _trustApiClient.Verify(c => c.InformProtocolVersionIncompatible(
            It.Is<InformProtocolVersionIncompatibleParameters>(p =>
                p.SessionId == sessionId &&
                p.MemberClientInstanceId == "TestClientInstanceId" &&
                p.JoinerClientInstanceId == joinerClientInstanceId &&
                p.MemberProtocolVersion == ProtocolVersion.CURRENT &&
                p.JoinerProtocolVersion == incompatibleVersion),
            It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Test]
    public async Task TrustAllMembersPublicKeys_WhenProtocolVersionIncompatibleNotificationReceived_ShouldReturnIncompatibleProtocolVersion()
    {
        var sessionId = "TestSessionId";
        
        _sessionMemberApiClient
            .Setup(c => c.GetMembersClientInstanceIds(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["MemberInstanceId"]);
        
        var startTrustCheckResult = new StartTrustCheckResult
        {
            IsOK = true,
            MembersInstanceIds = ["MemberInstanceId"]
        };
        
        _trustApiClient
            .Setup(c => c.StartTrustCheck(It.IsAny<TrustCheckParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(startTrustCheckResult);
        
        _trustProcessPublicKeysRepository
            .Setup(r => r.ResetJoinerTrustProcessData(sessionId))
            .Returns(Task.CompletedTask);
        
        _trustProcessPublicKeysRepository
            .Setup(r => r.SetExpectedPublicKeyCheckDataCount(sessionId, It.IsAny<List<string>>()))
            .Returns(Task.CompletedTask);
        
        _trustProcessPublicKeysRepository
            .Setup(r => r.WaitAsync(sessionId, It.IsAny<Func<TrustProcessPublicKeysData, EventWaitHandle>>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);
        
        _trustProcessPublicKeysRepository
            .Setup(r => r.IsProtocolVersionIncompatible(sessionId))
            .ReturnsAsync(true);
        
        var result = await _publicKeysTruster.TrustAllMembersPublicKeys(sessionId);
        
        result.Status.Should().Be(JoinSessionStatus.IncompatibleProtocolVersion);
    }
}
