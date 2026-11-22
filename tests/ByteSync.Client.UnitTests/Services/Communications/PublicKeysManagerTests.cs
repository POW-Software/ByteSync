using System.Security.Cryptography;
using System.Text;
using ByteSync.Business.Communications;
using ByteSync.Business.Configurations;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Versions;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Services.Communications;
using ByteSync.TestsCommon;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Communications;

[TestFixture]
public class PublicKeysManagerTests : AbstractTester
{
    private Mock<IApplicationSettingsRepository> _applicationSettingsRepository = null!;
    private Mock<IConnectionService> _connectionService = null!;
    private Mock<ILogger<PublicKeysManager>> _logger = null!;
    private PublicKeysManager _publicKeysManager = null!;
    
    [SetUp]
    public void SetUp()
    {
        _applicationSettingsRepository = new Mock<IApplicationSettingsRepository>();
        _connectionService = new Mock<IConnectionService>();
        _logger = new Mock<ILogger<PublicKeysManager>>();
        
        var rsa = RSA.Create();
        var publicKeyBytes = rsa.ExportRSAPublicKey();
        var privateKeyBytes = rsa.ExportRSAPrivateKey();
        
        var applicationSettings = new ApplicationSettings
        {
            ClientId = "TestClientId"
        };
        applicationSettings.SetEncryptionPassword("test-password");
        applicationSettings.DecodedRsaPublicKey = publicKeyBytes;
        applicationSettings.DecodedRsaPrivateKey = privateKeyBytes;
        
        _applicationSettingsRepository
            .Setup(r => r.GetCurrentApplicationSettings())
            .Returns(applicationSettings);
        
        _connectionService
            .Setup(c => c.ClientInstanceId)
            .Returns("TestInstanceId");
        
        _publicKeysManager = new PublicKeysManager(
            _applicationSettingsRepository.Object,
            _connectionService.Object,
            _logger.Object,
            Mock.Of<ByteSync.Interfaces.Controls.Applications.IEnvironmentService>(),
            Mock.Of<IAuthenticationTokensRepository>(),
            new Lazy<ByteSync.Interfaces.Services.Sessions.ISessionService>(() => Mock.Of<ByteSync.Interfaces.Services.Sessions.ISessionService>()),
            Mock.Of<ByteSync.Interfaces.Repositories.ICloudSessionConnectionRepository>()
        );
    }
    
    [Test]
    public void RenewCommand_IsDisabledDuringSessionsAndConnectionAttempts()
    {
        var manager = new Mock<ByteSync.Interfaces.Controls.Communications.IPublicKeysManager>();
        manager.Setup(m => m.GetMyPublicKeyInfo()).Returns(new PublicKeyInfo { PublicKey = [1, 2, 3] });
        using var sessions = new System.Reactive.Subjects.BehaviorSubject<ByteSync.Common.Business.Sessions.AbstractSession?>(null);
        using var sessionStates = new System.Reactive.Subjects.BehaviorSubject<ByteSync.Business.Sessions.SessionConnectionStatus>(ByteSync.Business.Sessions.SessionConnectionStatus.NoSession);
        using var connectionStates = new System.Reactive.Subjects.BehaviorSubject<ByteSync.Business.Communications.ConnectionStatuses>(ByteSync.Business.Communications.ConnectionStatuses.Connected);
        var session = new Mock<ByteSync.Interfaces.Services.Sessions.ISessionService>();
        session.SetupGet(s => s.SessionObservable).Returns(sessions);
        var sessionConnection = new Mock<ByteSync.Interfaces.Repositories.ICloudSessionConnectionRepository>();
        sessionConnection.SetupGet(s => s.ConnectionStatusObservable).Returns(sessionStates);
        var connection = new Mock<IConnectionService>();
        connection.SetupGet(c => c.ConnectionStatus).Returns(connectionStates);
        var viewModel = new ByteSync.ViewModels.TrustedNetworks.TrustedNetworkViewModel(manager.Object,
            _applicationSettingsRepository.Object, new ByteSync.ViewModels.TrustedNetworks.TrustedPublicKeysViewModel(),
            Mock.Of<ByteSync.Interfaces.Dialogs.IMessageBoxViewModelFactory>(), session.Object, sessionConnection.Object, connection.Object);
        var enabled = false;
        using var subscription = viewModel.RenewPublicKeyCommand.CanExecute.Subscribe(v => enabled = v);

        enabled.Should().BeTrue();
        sessionStates.OnNext(ByteSync.Business.Sessions.SessionConnectionStatus.JoiningSession);
        enabled.Should().BeFalse();
        sessionStates.OnNext(ByteSync.Business.Sessions.SessionConnectionStatus.NoSession);
        sessions.OnNext(new ByteSync.Common.Business.Sessions.Cloud.CloudSession("session", "instance"));
        enabled.Should().BeFalse();
        sessions.OnNext(null);
        connectionStates.OnNext(ByteSync.Business.Communications.ConnectionStatuses.Connecting);
        enabled.Should().BeFalse();
        connectionStates.OnNext(ByteSync.Business.Communications.ConnectionStatuses.Connected);
        enabled.Should().BeTrue();
    }

    [Test]
    public void GetMyPublicKeyInfo_ShouldIncludeProtocolVersion()
    {
        var result = _publicKeysManager.GetMyPublicKeyInfo();
        
        result.ProtocolVersion.Should().Be(ProtocolVersion.CURRENT);
    }
    
    [Test]
    public void GetMyPublicKeyInfo_ShouldIncludeClientId()
    {
        var result = _publicKeysManager.GetMyPublicKeyInfo();
        
        result.ClientId.Should().Be("TestClientId");
    }
    
    [Test]
    public void GetMyPublicKeyInfo_ShouldIncludePublicKey()
    {
        var applicationSettings = _applicationSettingsRepository.Object.GetCurrentApplicationSettings();
        var result = _publicKeysManager.GetMyPublicKeyInfo();
        
        result.PublicKey.Should().BeEquivalentTo(applicationSettings.DecodedRsaPublicKey);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ExistingReadableAliasTrust_RequiresThePreviouslyTrustedKey(bool differentKey)
    {
        var settings = _applicationSettingsRepository.Object.GetCurrentApplicationSettings();
        var trustedKey = settings.DecodedRsaPublicKey!;
        settings.InitializeTrustedPublicKeys();
        settings.AddTrustedKey(new TrustedPublicKey { ClientId = "ABCD-EFG-HIJ", PublicKey = trustedKey,
            ValidationDate = DateTimeOffset.UtcNow });
        using var otherKey = RSA.Create(2048);
        var peer = new PublicKeyInfo { ClientId = "ABCD-EFG-HIJ",
            PublicKey = differentKey ? otherKey.ExportRSAPublicKey() : trustedKey };

        var trusted = _publicKeysManager.IsTrusted(peer);

        trusted.Should().Be(!differentKey);
        settings.DecodedTrustedPublicKeys.Should().ContainSingle().Which.PublicKey.Should().Equal(trustedKey);
    }
    
    [Test]
    public void BuildJoinerPublicKeyCheckData_ShouldIncludeProtocolVersion()
    {
        var memberPublicKeyCheckData = new PublicKeyCheckData
        {
            IssuerPublicKeyInfo = new PublicKeyInfo
            {
                ClientId = "MemberClient",
                PublicKey = Encoding.UTF8.GetBytes("MemberPublicKey"),
                ProtocolVersion = ProtocolVersion.CURRENT
            },
            Salt = "TestSalt123",
            ProtocolVersion = ProtocolVersion.CURRENT
        };
        
        var result = _publicKeysManager.BuildJoinerPublicKeyCheckData(memberPublicKeyCheckData);
        
        result.ProtocolVersion.Should().Be(ProtocolVersion.CURRENT);
    }
    
    [Test]
    public void BuildMemberPublicKeyCheckData_ShouldIncludeProtocolVersion()
    {
        var joinerPublicKeyInfo = new PublicKeyInfo
        {
            ClientId = "JoinerClient",
            PublicKey = Encoding.UTF8.GetBytes("JoinerPublicKey"),
            ProtocolVersion = ProtocolVersion.CURRENT
        };
        
        var result = _publicKeysManager.BuildMemberPublicKeyCheckData(joinerPublicKeyInfo, true);
        
        result.ProtocolVersion.Should().Be(ProtocolVersion.CURRENT);
    }
}
