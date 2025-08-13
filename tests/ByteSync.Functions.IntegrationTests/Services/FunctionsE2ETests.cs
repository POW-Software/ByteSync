using System.Diagnostics;
using System.Text;
using System.Text.Json;
using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Controls.Json;
using DotNet.Testcontainers.Builders;
using ContainerBuilder = DotNet.Testcontainers.Builders.ContainerBuilder;
using IContainer = DotNet.Testcontainers.Containers.IContainer;
using FluentAssertions;

namespace ByteSync.Functions.IntegrationTests.Services;

[TestFixture]
public class FunctionsE2ETests
{
    
    private IContainer _azurite = null!;
    private IContainer _functions = null!;
    private HttpClient _http = null!;
    
    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        _azurite = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/azure-storage/azurite")
            .WithName($"bytesync-azurite-e2e-{Guid.NewGuid():N}")
            .WithPortBinding(10000, 10000)
            .WithPortBinding(10001, 10001)
            .WithPortBinding(10002, 10002)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(10000))
            .Build();

        using (var azuriteCts = new CancellationTokenSource(TimeSpan.FromSeconds(90)))
        {
            await _azurite.StartAsync(azuriteCts.Token);
        }

        string ResolveFunctionsProjectRoot()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ByteSync.sln")))
            {
                dir = dir.Parent;
            }
            if (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "src", "ByteSync.Functions");
                if (Directory.Exists(candidate)) return candidate;
            }
            return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "src", "ByteSync.Functions"));
        }

        var projectRoot = ResolveFunctionsProjectRoot();

        var publishDir = Path.Combine(Path.GetTempPath(), $"bytesync-func-pub-{Guid.NewGuid():N}");
        Directory.CreateDirectory(publishDir);
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"publish \"{projectRoot}\" -c Release -o \"{publishDir}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var publish = new Process { StartInfo = startInfo };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        publish.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        publish.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

        publish.Start();
        publish.BeginOutputReadLine();
        publish.BeginErrorReadLine();
        publish.WaitForExit();
        
        publish.ExitCode.Should().Be(0);
        
        var cfg = GlobalTestSetup.Configuration;
        string redisConnectionString = cfg["Redis:ConnectionString"]!;
        string redisPrefix = cfg["Redis:Prefix"] ?? "e2e";
        string signalRConnectionString = cfg["SignalR:ConnectionString"]!;
        string appSecret = cfg["AppSettings:Secret"] ?? "IntegrationTestSecret";

        var env = new Dictionary<string, string>
        {
            ["AzureWebJobsStorage"] = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tHQVh9+3JHB+F/2j3xYZx3d/==;BlobEndpoint=http://host.docker.internal:10000/devstoreaccount1;QueueEndpoint=http://host.docker.internal:10001/devstoreaccount1;TableEndpoint=http://host.docker.internal:10002/devstoreaccount1",
            ["FUNCTIONS_WORKER_RUNTIME"] = "dotnet-isolated",
            ["ASPNETCORE_URLS"] = "http://+:80",
            ["AzureSignalRConnectionString"] = signalRConnectionString,
            ["AppSettings__JwtDurationInSeconds"] = "3600",
            ["AppSettings__SkipClientsVersionCheck"] = "true",
            ["Redis__ConnectionString"] = redisConnectionString,
            ["Redis__Prefix"] = redisPrefix,
            ["SignalR__ConnectionString"] = signalRConnectionString,
            ["AppSettings__Secret"] = appSecret,
            ["AppSettings__DefaultStorageProvider"] = "AzureBlobStorage",
            ["AzureBlobStorage__Endpoint"] = "http://host.docker.internal:10000/devstoreaccount1",
            ["AzureBlobStorage__AccountName"] = "devstoreaccount1",
            ["AzureBlobStorage__AccountKey"] = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tHQVh9+3JHB+F/2j3xYZx3d/==",
            ["AzureBlobStorage__Container"] = "bytesync-test"
        };


        var functionsBuilder = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0")
            .WithName($"bytesync-functions-e2e-{Guid.NewGuid():N}")
            .WithPortBinding(7071, 80)
            .WithBindMount(publishDir, "/home/site/wwwroot")
            .WithEnvironment(env)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(80));
        
        _functions = functionsBuilder.Build();

        using var funcCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await _functions.StartAsync(funcCts.Token);
        
        _http = new HttpClient { BaseAddress = new Uri("http://localhost:7071/api/") };
        _http.DefaultRequestHeaders.Add("User-Agent", "ByteSync-E2E-Test");
        for (var i = 0; i < 20; i++)
		{

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		var resp = await _http.GetAsync("announcements", cts.Token);
		if (resp.IsSuccessStatusCode)
		{
			break;
		}
	    await Task.Delay(1000);
		}
    }

    [OneTimeTearDown]
    public async Task OneTimeTeardown()
    {
        await _functions.DisposeAsync();
        await _azurite.DisposeAsync();
        _http?.Dispose();
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
        askResult!.Status.Should().Be(JoinSessionStatus.ProcessingNormally);

        var validateParams = new ValidateJoinCloudSessionParameters(sessionId, loginB.ClientInstanceId, loginA.ClientInstanceId, new byte[] { 1,2,3,4,5,6,7,8 });
        await PostJsonAsync<object>($"session/{sessionId}/validateJoin", validateParams, tokenB.AuthenticationTokens!.JwtToken);
    }

    private async Task<List<SessionMemberInfoDTO>> GetMembersAsync(string sessionId, string jwtToken)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"session/{sessionId}/members");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
        var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            Assert.Fail($"GET session/{sessionId}/members failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {body}");
        }
        var json = await resp.Content.ReadAsStringAsync();
        var list = JsonSerializer.Deserialize<List<SessionMemberInfoDTO>>(json, JsonSerializerOptionsHelper.BuildOptions());
        return list ?? new List<SessionMemberInfoDTO>();
    }

    private async Task<InitialAuthenticationResponse> LoginAsync(LoginData login)
    {
        var loginJson = JsonSerializer.Serialize(login);
        using var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");
        loginContent.Headers.ContentLength = Encoding.UTF8.GetByteCount(loginJson);
        TestContext.Progress.WriteLine($"{DateTime.UtcNow:o} | POST /auth/login");
        var authResp = await _http.PostAsync("auth/login", loginContent);
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

    private async Task<T?> PostJsonAsync<T>(string relativeUrl, object body, string jwtToken)
    {
        var json = JsonSerializer.Serialize(body);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        content.Headers.ContentLength = Encoding.UTF8.GetByteCount(json);
        using var req = new HttpRequestMessage(HttpMethod.Post, relativeUrl);
        req.Content = content;
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
        TestContext.Progress.WriteLine($"{DateTime.UtcNow:o} | POST {relativeUrl}");
        var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            var respBody = await resp.Content.ReadAsStringAsync();
            Assert.Fail($"POST {relativeUrl} failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {respBody}");
        }
        if (typeof(T) == typeof(object)) return default;
        var respJson = await resp.Content.ReadAsStringAsync();
        TestContext.Progress.WriteLine($"{DateTime.UtcNow:o} | Response {relativeUrl}: {respJson}");
        return JsonSerializer.Deserialize<T>(respJson, JsonSerializerOptionsHelper.BuildOptions());
    }

}