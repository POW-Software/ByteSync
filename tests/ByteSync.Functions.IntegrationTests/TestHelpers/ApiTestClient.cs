using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ByteSync.Common.Business.Auth;
using ByteSync.Common.Controls.Json;
using FluentAssertions;

namespace ByteSync.Functions.IntegrationTests.TestHelpers;

public sealed class ApiTestClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _disposeHttpClient;
    private string? _bearerToken;

    public ApiTestClient(HttpClient httpClient, bool disposeHttpClient = false)
    {
        _httpClient = httpClient;
        _disposeHttpClient = disposeHttpClient;
    }

    public void SetBearerToken(string? jwtToken)
    {
        _bearerToken = jwtToken;
    }

    public async Task<T?> PostJsonAsync<T>(string relativeUrl, object body, string? jwtToken = null)
    {
        var json = JsonSerializer.Serialize(body);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, relativeUrl)
        {
            Content = content
        };

        var tokenToUse = jwtToken ?? _bearerToken;
        if (!string.IsNullOrWhiteSpace(tokenToUse))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenToUse);
        }

        TestContext.Progress.WriteLine($"{DateTime.UtcNow:o} | POST {relativeUrl}");
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            Assert.Fail($"POST {relativeUrl} failed: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {responseBody}");
        }

        if (typeof(T) == typeof(object))
        {
            return default;
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(responseJson, JsonSerializerOptionsHelper.BuildOptions());
    }

    public async Task<T?> GetJsonAsync<T>(string relativeUrl, string? jwtToken = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);

        var tokenToUse = jwtToken ?? _bearerToken;
        if (!string.IsNullOrWhiteSpace(tokenToUse))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenToUse);
        }

        TestContext.Progress.WriteLine($"{DateTime.UtcNow:o} | GET {relativeUrl}");
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            Assert.Fail($"GET {relativeUrl} failed: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {responseBody}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(responseJson, JsonSerializerOptionsHelper.BuildOptions());
    }

    public async Task<InitialAuthenticationResponse> LoginAsync(LoginData login)
    {
        var loginJson = JsonSerializer.Serialize(login);
        using var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");
        TestContext.Progress.WriteLine($"{DateTime.UtcNow:o} | POST /auth/login");
        var authResponse = await _httpClient.PostAsync("auth/login", loginContent);
        if (!authResponse.IsSuccessStatusCode)
        {
            var body = await authResponse.Content.ReadAsStringAsync();
            Assert.Fail($"auth/login failed: {(int)authResponse.StatusCode} {authResponse.ReasonPhrase}. Body: {body}");
        }

        var authJson = await authResponse.Content.ReadAsStringAsync();
        var auth = JsonSerializer.Deserialize<InitialAuthenticationResponse>(authJson, JsonSerializerOptionsHelper.BuildOptions())!;
        auth.IsSuccess.Should().BeTrue();
        auth.AuthenticationTokens.Should().NotBeNull();
        auth.AuthenticationTokens!.JwtToken.Should().NotBeNullOrEmpty();
        return auth;
    }

    public void Dispose()
    {
        if (_disposeHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}


