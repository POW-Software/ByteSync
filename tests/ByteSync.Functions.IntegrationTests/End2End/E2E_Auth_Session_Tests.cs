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
using ByteSync.TestsCommon.TestHelpers;

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
        using var identityA = new AuthenticationTestIdentity();
        var loginA = identityA.CreateLogin();
        var tokenA = await LoginAsync(loginA, identityA);
        _api.SetBearerToken(tokenA.AuthenticationTokens!.JwtToken);
        var createParams = new CreateCloudSessionParameters
        {
            LobbyId = null,
            CreatorProfileClientId = null,
            SessionSettings = new EncryptedSessionSettings
                { Id = Guid.NewGuid().ToString("N"), Data = new byte[16], IV = new byte[16] },
            CreatorPublicKeyInfo = identityA.CreatePublicKeyInfo(),
            CreatorPrivateData = new EncryptedSessionMemberPrivateData
                { Id = Guid.NewGuid().ToString("N"), Data = new byte[16], IV = new byte[16] }
        };
        var createResult = await _api.PostJsonAsync<CloudSessionResult>("session", createParams);
        createResult.Should().NotBeNull();
        var sessionId = createResult.SessionId;
        var creatorMembers = await _api.GetJsonAsync<List<SessionMemberInfoDTO>>($"session/{sessionId}/members");
        creatorMembers.Any(m => m.ClientInstanceId == loginA.ClientInstanceId).Should().BeTrue();
        using var identityB = new AuthenticationTestIdentity();
        var loginB = identityB.CreateLogin();
        var tokenB = await LoginAsync(loginB, identityB);
        var askParams = new AskCloudSessionPasswordExchangeKeyParameters(sessionId,
            identityB.CreatePublicKeyInfo())
        {
            LobbyId = null,
            ProfileClientId = null
        };
        var members = await _api.PostJsonAsync<List<string>>($"session/{sessionId}/beginJoin",
            askParams, tokenB.AuthenticationTokens!.JwtToken);
        members.Should().Contain(loginA.ClientInstanceId);
        var askResult = await _api.PostJsonAsync<JoinSessionResult>($"session/{sessionId}/askPasswordExchangeKey",
            askParams, tokenB.AuthenticationTokens!.JwtToken);
        askResult.Should().NotBeNull();
        askResult.Status.Should().Be(JoinSessionStatus.ProcessingNormally);
        var validateParams = new ValidateJoinCloudSessionParameters(sessionId, loginB.ClientInstanceId,
            loginA.ClientInstanceId, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        await _api.PostJsonAsync<object>($"session/{sessionId}/validateJoin", validateParams,
            tokenA.AuthenticationTokens!.JwtToken);
    }

    private async Task<InitialAuthenticationResponse> LoginAsync(LoginData login, AuthenticationTestIdentity identity)
    {
        var challenge = await _api.PostJsonAsync<AuthenticationChallenge>("auth/challenge", login);
        challenge.Should().NotBeNull();
        identity.Sign(login, challenge!);
        var loginJson = JsonSerializer.Serialize(login);
        using var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");
        TestContext.Progress.WriteLine($"{DateTime.UtcNow:o} | POST /auth/login");
        var authResponse = await _initializer.Http.PostAsync("auth/login", loginContent);
        if (!authResponse.IsSuccessStatusCode)
        {
            var body = await authResponse.Content.ReadAsStringAsync();
            throw new HttpRequestException($"auth/login failed: {(int)authResponse.StatusCode} {authResponse.ReasonPhrase}. Body: {body}");
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