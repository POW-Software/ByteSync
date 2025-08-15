using System.Text;
using System.Text.Json;
using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Controls.Json;
using FluentAssertions;

namespace ByteSync.Functions.IntegrationTests.End2End;

[TestFixture]
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
        await _initializer.Functions.DisposeAsync();
        await _initializer.Azurite.DisposeAsync();
        _initializer.Http.Dispose();
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
        var createParams = new CreateCloudSessionParameters
        {
            LobbyId = null,
            CreatorProfileClientId = null,
            SessionSettings = new EncryptedSessionSettings { Id = Guid.NewGuid().ToString("N"), Data = new byte[16], IV = new byte[16] },
            CreatorPublicKeyInfo = new PublicKeyInfo { ClientId = loginA.ClientId, PublicKey = new byte[32] },
            CreatorPrivateData = new EncryptedSessionMemberPrivateData { Id = Guid.NewGuid().ToString("N"), Data = new byte[16], IV = new byte[16] }
        };
        var createResult = await PostJsonAsync<CloudSessionResult>("session", createParams, tokenA.AuthenticationTokens!.JwtToken);
        createResult.Should().NotBeNull();
        var sessionId = createResult!.SessionId;
        var creatorMembers = await GetMembersAsync(sessionId, tokenA.AuthenticationTokens!.JwtToken);
        creatorMembers.Any(m => m.ClientInstanceId == loginA.ClientInstanceId).Should().BeTrue();
        var loginB = new LoginData
        {
            ClientId = "e2e-client-B",
            ClientInstanceId = Guid.NewGuid().ToString("N"),
            Version = "9.9.9",
            OsPlatform = Common.Business.Misc.OSPlatforms.Windows
        };
        var tokenB = await LoginAsync(loginB);
        var askParams = new AskCloudSessionPasswordExchangeKeyParameters(sessionId, new PublicKeyInfo { ClientId = loginB.ClientId, PublicKey = new byte[32] })
        {
            LobbyId = null,
            ProfileClientId = null
        };
        var askResult = await PostJsonAsync<JoinSessionResult>($"session/{sessionId}/askPasswordExchangeKey", askParams, tokenB.AuthenticationTokens!.JwtToken);
        askResult.Should().NotBeNull();
        askResult.Status.Should().Be(JoinSessionStatus.ProcessingNormally);
        var validateParams = new ValidateJoinCloudSessionParameters(sessionId, loginB.ClientInstanceId, loginA.ClientInstanceId, new byte[] { 1,2,3,4,5,6,7,8 });
        await PostJsonAsync<object>($"session/{sessionId}/validateJoin", validateParams, tokenB.AuthenticationTokens!.JwtToken);
    }

    public async Task<List<SessionMemberInfoDTO>> GetMembersAsync(string sessionId, string jwtToken)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"session/{sessionId}/members");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
        var resp = await _initializer.Http.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            Assert.Fail($"GET session/{sessionId}/members failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {body}");
        }
        var json = await resp.Content.ReadAsStringAsync();
        var list = JsonSerializer.Deserialize<List<SessionMemberInfoDTO>>(json, JsonSerializerOptionsHelper.BuildOptions());
        return list ?? new List<SessionMemberInfoDTO>();
    }

    public async Task<InitialAuthenticationResponse> LoginAsync(LoginData login)
    {
        var loginJson = JsonSerializer.Serialize(login);
        using var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");
        TestContext.Progress.WriteLine($"{DateTime.UtcNow:o} | POST /auth/login");
        var authResp = await _initializer.Http.PostAsync("auth/login", loginContent);
        if (!authResp.IsSuccessStatusCode)
        {
            var body = await authResp.Content.ReadAsStringAsync();
            Assert.Fail($"auth/login failed: {(int)authResp.StatusCode} {authResp.ReasonPhrase}. Body: {body}");
        }
        var authJson = await authResp.Content.ReadAsStringAsync();
        var auth = JsonSerializer.Deserialize<InitialAuthenticationResponse>(authJson, JsonSerializerOptionsHelper.BuildOptions())!;
        auth.IsSuccess.Should().BeTrue();
        auth.AuthenticationTokens.Should().NotBeNull();
        auth.AuthenticationTokens!.JwtToken.Should().NotBeNullOrEmpty();
        return auth;
    }

    public async Task<T?> PostJsonAsync<T>(string relativeUrl, object body, string jwtToken)
    {
        var json = JsonSerializer.Serialize(body);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var req = new HttpRequestMessage(HttpMethod.Post, relativeUrl);
        req.Content = content;
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
        TestContext.Progress.WriteLine($"{DateTime.UtcNow:o} | POST {relativeUrl}");
        var resp = await _initializer.Http.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            var respBody = await resp.Content.ReadAsStringAsync();
            Assert.Fail($"POST {relativeUrl} failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {respBody}");
        }
        if (typeof(T) == typeof(object)) return default;
        var respJson = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(respJson, JsonSerializerOptionsHelper.BuildOptions());
    }
}