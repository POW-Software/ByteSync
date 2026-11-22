using System.Net;
using System.Net.Http;
using ByteSync.Common.Business.Auth;
using ByteSync.Exceptions;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Services.Communications.Api;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Communications.Api;

public class ApiInvokerTests
{
    private Mock<IAuthenticationTokensRepository> _tokens = null!;
    private HttpClient _httpClient = null!;
    private Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _send = null!;
    private ApiInvoker _api = null!;

    [SetUp]
    public void SetUp()
    {
        _tokens = new Mock<IAuthenticationTokensRepository>();
        _httpClient = new HttpClient(new StubHttpHandler((request, token) => _send(request, token)));
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("ApiClient")).Returns(_httpClient);
        var constants = new Mock<IConnectionConstantsService>();
        constants.Setup(c => c.GetApiUrl()).ReturnsAsync("https://relay.invalid/api/");
        _api = new ApiInvoker(factory.Object, _tokens.Object, constants.Object, NullLogger<ApiInvoker>.Instance);
    }

    [TearDown]
    public void TearDown() => _httpClient.Dispose();

    [TestCase("")]
    [TestCase("<html>Not Found</html>")]
    [TestCase("{\"error\":\"Not Found\"}")]
    public async Task NotFound_PreservesStatusRegardlessOfResponseBody(string body)
    {
        _send = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent(body) });

        var act = () => _api.PostAsync<AuthenticationChallenge>("auth/challenge", new LoginData());

        var error = (await act.Should().ThrowAsync<ApiException>()).Which;
        error.HttpStatusCode.Should().Be(HttpStatusCode.NotFound);
        error.Message.Should().Contain("auth/challenge");
    }

    [TestCase("")]
    [TestCase("<html>Unavailable</html>")]
    public async Task ServerFailure_PreservesStatusWhenResponseCannotBeDeserialized(string body)
    {
        _send = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent(body) });

        var act = () => _api.GetAsync<AuthenticationChallenge>("auth/challenge");

        (await act.Should().ThrowAsync<ApiException>()).Which.HttpStatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Test]
    public async Task NetworkFailure_PreservesCauseWithoutInventingAnHttpStatus()
    {
        var failure = new HttpRequestException("Connection failed");
        _send = (_, _) => throw failure;

        var act = () => _api.PostAsync<AuthenticationChallenge>("auth/challenge", new LoginData());

        var error = (await act.Should().ThrowAsync<ApiException>()).Which;
        error.HttpStatusCode.Should().BeNull();
        error.InnerException.Should().BeSameAs(failure);
    }

    [TestCase("GET")]
    [TestCase("POST")]
    [TestCase("DELETE")]
    public async Task SuccessfulRequest_PreservesTypedResponseAndRequest(string method)
    {
        _tokens.Setup(t => t.GetTokens()).ReturnsAsync(new AuthenticationTokens { JwtToken = "local-test-token" });
        _send = async (request, token) =>
        {
            request.Method.Method.Should().Be(method);
            request.RequestUri!.AbsoluteUri.Should().Be("https://relay.invalid/api/test");
            request.Headers.GetValues("authorization").Should().Equal("local-test-token");
            token.CanBeCanceled.Should().BeTrue();
            if (method == "GET")
            {
                request.Content.Should().BeNull();
            }
            else
            {
                (await request.Content!.ReadAsStringAsync(token)).Should().Contain("request");
            }
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"Id\":\"challenge\"}") };
        };
        using var cancellation = new CancellationTokenSource();

        var result = method switch
        {
            "GET" => await _api.GetAsync<AuthenticationChallenge>("test", cancellation.Token),
            "POST" => await _api.PostAsync<AuthenticationChallenge>("test", new AuthenticationChallenge { Id = "request" }, cancellation.Token),
            _ => await _api.DeleteAsync<AuthenticationChallenge>("test", new AuthenticationChallenge { Id = "request" }, cancellation.Token)
        };

        result.Id.Should().Be("challenge");
    }

    [Test]
    public async Task BusinessErrorResponse_RemainsDeserializable()
    {
        _send = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("{\"InitialConnectionStatus\":\"InvalidProof\"}")
        });

        var response = await _api.PostAsync<InitialAuthenticationResponse>("auth/login", new LoginData());

        response.InitialConnectionStatus.Should().Be(InitialConnectionStatus.InvalidProof);
    }

    [Test]
    public async Task PostWithoutResponse_DoesNotRequireJson()
    {
        _send = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));

        var act = () => _api.PostAsync("test", null);

        await act.Should().NotThrowAsync();
    }

    [TestCase("")]
    [TestCase("null")]
    [TestCase("<html>Invalid JSON</html>")]
    public async Task InvalidSuccessfulResponse_IsRejected(string body)
    {
        _send = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });

        var act = () => _api.GetAsync<AuthenticationChallenge>("test");

        await act.Should().ThrowAsync<ApiException>();
    }

    private sealed class StubHttpHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}
