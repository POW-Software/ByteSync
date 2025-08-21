using System.Text;
using System.Text.Json;
using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Controls.Json;
using FluentAssertions;
using ByteSync.Functions.IntegrationTests.TestHelpers;

namespace ByteSync.Functions.IntegrationTests.End2End;

[TestFixture]
public class E2E_Auth_Session_Tests
{
    private static E2E_Environment_Setup _initializer = null!;
    private static ApiTestClient _api = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        _initializer = new E2E_Environment_Setup();
        await _initializer.InitializeAsync();
        _api = new ApiTestClient(_initializer.Http);
    }

    [OneTimeTearDown]
    public async Task OneTimeTeardown()
    {
        await _initializer.Functions.DisposeAsync();
        await _initializer.Azurite.DisposeAsync();
        _initializer.Http.Dispose();
        _api.Dispose();
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
        var tokenA = await LoginAsync(loginA);
        _api.SetBearerToken(tokenA.AuthenticationTokens!.JwtToken);
        var createParams = new CreateCloudSessionParameters
        {
            LobbyId = null,
            CreatorProfileClientId = null,
            SessionSettings = new EncryptedSessionSettings
                { Id = Guid.NewGuid().ToString("N"), Data = new byte[16], IV = new byte[16] },
            CreatorPublicKeyInfo = new PublicKeyInfo { ClientId = loginA.ClientId, PublicKey = new byte[32] },
            CreatorPrivateData = new EncryptedSessionMemberPrivateData
                { Id = Guid.NewGuid().ToString("N"), Data = new byte[16], IV = new byte[16] }
        };
        var createResult = await _api.PostJsonAsync<CloudSessionResult>("session", createParams);
        createResult.Should().NotBeNull();
        var sessionId = createResult.SessionId;
        var creatorMembers = await _api.GetJsonAsync<List<SessionMemberInfoDTO>>($"session/{sessionId}/members");
        creatorMembers.Any(m => m.ClientInstanceId == loginA.ClientInstanceId).Should().BeTrue();
        var loginB = new LoginData
        {
            ClientId = "e2e-client-B",
            ClientInstanceId = Guid.NewGuid().ToString("N"),
            Version = "9.9.9",
            OsPlatform = Common.Business.Misc.OSPlatforms.Windows
        };
        var tokenB = await LoginAsync(loginB);
        var askParams = new AskCloudSessionPasswordExchangeKeyParameters(sessionId,
            new PublicKeyInfo { ClientId = loginB.ClientId, PublicKey = new byte[32] })
        {
            LobbyId = null,
            ProfileClientId = null
        };
        var askResult = await _api.PostJsonAsync<JoinSessionResult>($"session/{sessionId}/askPasswordExchangeKey",
            askParams, tokenB.AuthenticationTokens!.JwtToken);
        askResult.Should().NotBeNull();
        askResult.Status.Should().Be(JoinSessionStatus.ProcessingNormally);
        var validateParams = new ValidateJoinCloudSessionParameters(sessionId, loginB.ClientInstanceId,
            loginA.ClientInstanceId, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        await _api.PostJsonAsync<object>($"session/{sessionId}/validateJoin", validateParams,
            tokenB.AuthenticationTokens!.JwtToken);
    }

    private async Task<InitialAuthenticationResponse> LoginAsync(LoginData login)
    {
        var loginJson = JsonSerializer.Serialize(login);
        using var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");
        TestContext.Progress.WriteLine($"{DateTime.UtcNow:o} | POST /auth/login");
        var authResponse = await _initializer.Http.PostAsync("auth/login", loginContent);
        if (!authResponse.IsSuccessStatusCode)
        {
            var body = await authResponse.Content.ReadAsStringAsync();
            Assert.Fail($"auth/login failed: {(int)authResponse.StatusCode} {authResponse.ReasonPhrase}. Body: {body}");
        }

        var authJson = await authResponse.Content.ReadAsStringAsync();
        var auth = JsonSerializer.Deserialize<InitialAuthenticationResponse>(authJson,
            JsonSerializerOptionsHelper.BuildOptions())!;
        auth.IsSuccess.Should().BeTrue();
        auth.AuthenticationTokens.Should().NotBeNull();
        auth.AuthenticationTokens!.JwtToken.Should().NotBeNullOrEmpty();
        return auth;
    }
}