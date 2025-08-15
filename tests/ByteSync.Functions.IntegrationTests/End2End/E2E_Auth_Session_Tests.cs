using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using FluentAssertions;

namespace ByteSync.Functions.IntegrationTests.End2End;

[SetUpFixture]
public class E2E_Auth_Session_Tests
{
    private static E2E_Auth_Session_Initializer _initializer = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        _initializer = new E2E_Auth_Session_Initializer();
        await _initializer.InitializeAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTeardown()
    {
        await _initializer.DisposeAsync();
    }

    [Test]
    public async Task Auth_Login_Then_CreateCloudSession_ShouldSucceed()
    {
        var loginA = new LoginData
        {
            ClientId = "e2e-client-A",
            ClientInstanceId = Guid.NewGuid().ToString("N"),
            Version = "9.9.9",
            OsPlatform = Common.Business.Misc.OSPlatforms.Windows
        };
        var tokenA = await _initializer.LoginAsync(loginA);
        var createParams = new CreateCloudSessionParameters
        {
            LobbyId = null,
            CreatorProfileClientId = null,
            SessionSettings = new EncryptedSessionSettings { Id = Guid.NewGuid().ToString("N"), Data = new byte[16], IV = new byte[16] },
            CreatorPublicKeyInfo = new PublicKeyInfo { ClientId = loginA.ClientId, PublicKey = new byte[32] },
            CreatorPrivateData = new EncryptedSessionMemberPrivateData { Id = Guid.NewGuid().ToString("N"), Data = new byte[16], IV = new byte[16] }
        };
        var createResult = await _initializer.PostJsonAsync<CloudSessionResult>("session", createParams, tokenA.AuthenticationTokens!.JwtToken);
        createResult.Should().NotBeNull();
        var sessionId = createResult!.SessionId;
        var creatorMembers = await _initializer.GetMembersAsync(sessionId, tokenA.AuthenticationTokens!.JwtToken);
        creatorMembers.Any(m => m.ClientInstanceId == loginA.ClientInstanceId).Should().BeTrue();
        var loginB = new LoginData
        {
            ClientId = "e2e-client-B",
            ClientInstanceId = Guid.NewGuid().ToString("N"),
            Version = "9.9.9",
            OsPlatform = Common.Business.Misc.OSPlatforms.Windows
        };
        var tokenB = await _initializer.LoginAsync(loginB);
        var askParams = new AskCloudSessionPasswordExchangeKeyParameters(sessionId, new PublicKeyInfo { ClientId = loginB.ClientId, PublicKey = new byte[32] })
        {
            LobbyId = null,
            ProfileClientId = null
        };
        var askResult = await _initializer.PostJsonAsync<JoinSessionResult>($"session/{sessionId}/askPasswordExchangeKey", askParams, tokenB.AuthenticationTokens!.JwtToken);
        askResult.Should().NotBeNull();
        askResult.Status.Should().Be(JoinSessionStatus.ProcessingNormally);
        var validateParams = new ValidateJoinCloudSessionParameters(sessionId, loginB.ClientInstanceId, loginA.ClientInstanceId, new byte[] { 1,2,3,4,5,6,7,8 });
        await _initializer.PostJsonAsync<object>($"session/{sessionId}/validateJoin", validateParams, tokenB.AuthenticationTokens!.JwtToken);
    }
}