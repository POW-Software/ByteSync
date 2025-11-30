using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using ByteSync.Business.Communications.PublicKeysTrusting;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
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
                ProtocolVersion = ProtocolVersion.Current
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
                ProtocolVersion = ProtocolVersion.Current
            },
            Salt = "TestSalt123",
            OtherPartyPublicKeyInfo = new PublicKeyInfo
            {
                ClientId = "JoinerClientId",
                PublicKey = Encoding.UTF8.GetBytes("JoinerPublicKey"),
                ProtocolVersion = ProtocolVersion.Current
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
                ProtocolVersion = ProtocolVersion.Current
            },
            Salt = "TestSalt123",
            OtherPartyPublicKeyInfo = new PublicKeyInfo
            {
                ClientId = "JoinerClientId",
                PublicKey = Encoding.UTF8.GetBytes("JoinerPublicKey"),
                ProtocolVersion = ProtocolVersion.Current
            }
        };

        var joinerPublicKeyCheckData = new PublicKeyCheckData
        {
            IssuerPublicKeyInfo = new PublicKeyInfo
            {
                ClientId = "JoinerClientId",
                PublicKey = Encoding.UTF8.GetBytes("JoinerPublicKey"),
                ProtocolVersion = ProtocolVersion.Current
            },
            IssuerClientInstanceId = joinerInstanceId,
            Salt = "TestSalt123",
            ProtocolVersion = ProtocolVersion.Current
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
}

