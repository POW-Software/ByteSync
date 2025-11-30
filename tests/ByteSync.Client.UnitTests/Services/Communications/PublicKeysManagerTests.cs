using System.Security.Cryptography;
using System.Text;
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
            _logger.Object
        );
    }

    [Test]
    public void GetMyPublicKeyInfo_ShouldIncludeProtocolVersion()
    {
        var result = _publicKeysManager.GetMyPublicKeyInfo();

        result.ProtocolVersion.Should().Be(ProtocolVersion.Current);
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

    [Test]
    public void BuildJoinerPublicKeyCheckData_ShouldIncludeProtocolVersion()
    {
        var memberPublicKeyCheckData = new PublicKeyCheckData
        {
            IssuerPublicKeyInfo = new PublicKeyInfo
            {
                ClientId = "MemberClient",
                PublicKey = Encoding.UTF8.GetBytes("MemberPublicKey"),
                ProtocolVersion = ProtocolVersion.Current
            },
            Salt = "TestSalt123",
            ProtocolVersion = ProtocolVersion.Current
        };

        var result = _publicKeysManager.BuildJoinerPublicKeyCheckData(memberPublicKeyCheckData);

        result.ProtocolVersion.Should().Be(ProtocolVersion.Current);
    }

    [Test]
    public void BuildMemberPublicKeyCheckData_ShouldIncludeProtocolVersion()
    {
        var joinerPublicKeyInfo = new PublicKeyInfo
        {
            ClientId = "JoinerClient",
            PublicKey = Encoding.UTF8.GetBytes("JoinerPublicKey"),
            ProtocolVersion = ProtocolVersion.Current
        };

        var result = _publicKeysManager.BuildMemberPublicKeyCheckData(joinerPublicKeyInfo, true);

        result.ProtocolVersion.Should().Be(ProtocolVersion.Current);
    }
}

