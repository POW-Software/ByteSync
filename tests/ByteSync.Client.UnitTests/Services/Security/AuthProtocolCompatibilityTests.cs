using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using ByteSync.Business.Configurations;
using ByteSync.Business.Communications;
using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.Versions;
using ByteSync.Exceptions;
using ByteSync.Factories;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Services.Communications;
using ByteSync.Services.Communications.Api;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Security;

public class AuthProtocolCompatibilityTests
{
    private ApplicationSettings _settings = null!;
    private Mock<IApiInvoker> _api = null!;
    private AuthApiClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _settings = new ApplicationSettings { InstallationId = "local-protocol-test" };
        _settings.SetEncryptionPassword("local-test-only-password");
        _settings.InitializeRsa();
        var repository = new Mock<IApplicationSettingsRepository>();
        repository.Setup(r => r.GetCurrentApplicationSettings()).Returns(_settings);
        _api = new Mock<IApiInvoker>(MockBehavior.Strict);
        _client = new AuthApiClient(_api.Object, repository.Object);
    }

    [TearDown]
    public void TearDown() => _settings.PrivateRsa.Dispose();

    [TestCase(false)]
    [TestCase(true)]
    public async Task OlderRelayWithoutChallengeEndpoint_IsRejectedWithoutCallingLegacyLogin(bool wrapped)
    {
        var notFound = new ApiException("Not Found", HttpStatusCode.NotFound);
        var error = wrapped ? new ApiException("Challenge request failed", notFound) : notFound;
        _api.Setup(a => a.PostAsync<AuthenticationChallenge>("auth/challenge", It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(error);

        var act = () => _client.Login(new LoginData());

        var exception = (await act.Should().ThrowAsync<RelayIncompatibleException>()).Which;
        exception.InnerException.Should().BeSameAs(error);
        exception.Message.Should().Contain("auth/challenge").And.Contain("HTTP 404")
            .And.Contain("Check the relay address").And.Contain("update the relay server").And.Contain("Automatic reconnection has stopped");
        _api.Verify(a => a.PostAsync<InitialAuthenticationResponse>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
        _api.Verify(a => a.PostAsync<AuthenticationChallenge>("auth/challenge", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        _api.VerifyNoOtherCalls();
    }

    [TestCase("")]
    [TestCase("<html><body>Not Found</body></html>")]
    [TestCase("{\"message\":\"Not Found\"}")]
    [TestCase("{\"Id\":\"unexpected-challenge\",\"Nonce\":\"unexpected-nonce\"}")]
    public async Task MissingChallengeEndpoint_StopsConnectionAfterOneHttpRequest(string body)
    {
        using var handler = new MissingChallengeHandler(body);
        using var httpClient = new HttpClient(handler);
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient("ApiClient")).Returns(httpClient);
        var tokens = new Mock<IAuthenticationTokensRepository>(MockBehavior.Strict);
        tokens.Setup(t => t.GetTokens()).ReturnsAsync((AuthenticationTokens?)null);
        var constants = new Mock<IConnectionConstantsService>();
        constants.Setup(c => c.GetApiUrl()).ReturnsAsync("https://relay.invalid/api/");
        var api = new ApiInvoker(httpClientFactory.Object, tokens.Object, constants.Object, NullLogger<ApiInvoker>.Instance);
        var settings = new Mock<IApplicationSettingsRepository>();
        settings.Setup(s => s.GetCurrentApplicationSettings()).Returns(_settings);
        var auth = new AuthApiClient(api, settings.Object);
        var environment = new Mock<IEnvironmentService>();
        environment.SetupGet(e => e.ApplicationVersion).Returns(new Version(1, 0, 0));
        var hubFactory = new Mock<IHubConnectionFactory>(MockBehavior.Strict);
        var factory = new ConnectionFactory(tokens.Object, environment.Object, auth, hubFactory.Object, NullLogger<ConnectionFactory>.Instance);
        using var connection = new ConnectionService(factory, tokens.Object, NullLogger<ConnectionService>.Instance)
        {
            RetryDelaySleepDurationProvider = _ => throw new InvalidOperationException("Unexpected reconnection attempt")
        };

        var act = () => connection.StartConnectionAsync();

        await act.Should().ThrowAsync<RelayIncompatibleException>();
        connection.CurrentConnectionStatus.Should().Be(ConnectionStatuses.RelayIncompatible);
        handler.Requests.Should().Equal("/api/auth/challenge");
        tokens.Verify(t => t.Store(It.IsAny<AuthenticationTokens>()), Times.Never);
        hubFactory.VerifyNoOtherCalls();
    }

    [TestCase(HttpStatusCode.BadRequest)]
    [TestCase(HttpStatusCode.Unauthorized)]
    [TestCase(HttpStatusCode.InternalServerError)]
    [TestCase(HttpStatusCode.ServiceUnavailable)]
    public async Task OtherChallengeFailures_KeepTheirOriginalErrorWithoutCallingLogin(HttpStatusCode status)
    {
        var error = new ApiException("Challenge request failed", status);
        _api.Setup(a => a.PostAsync<AuthenticationChallenge>("auth/challenge", It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(error);

        var act = () => _client.Login(new LoginData());

        (await act.Should().ThrowAsync<ApiException>()).Which.Should().BeSameAs(error);
        _api.Verify(a => a.PostAsync<AuthenticationChallenge>("auth/challenge", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        _api.VerifyNoOtherCalls();
    }

    [Test]
    public async Task LoginEndpointNotFound_IsNotReportedAsAMissingChallengeEndpoint()
    {
        var error = new ApiException("Login not found", HttpStatusCode.NotFound);
        _api.Setup(a => a.PostAsync<AuthenticationChallenge>("auth/challenge", It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticationChallenge { Id = "challenge", Nonce = "nonce" });
        _api.Setup(a => a.PostAsync<InitialAuthenticationResponse>("auth/login", It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(error);

        var act = () => _client.Login(new LoginData { ClientInstanceId = "local-test-instance", Version = "local-test" });

        (await act.Should().ThrowAsync<ApiException>()).Which.Should().BeSameAs(error);
        _api.Verify(a => a.PostAsync<InitialAuthenticationResponse>("auth/login", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CurrentRelay_ReceivesAValidProofWithTheCurrentEpochAndProtocol()
    {
        var identity = AuthenticationProtocol.GetIdentity(_settings.DecodedRsaPublicKey!);
        var login = new LoginData { ClientInstanceId = identity + "_" + Guid.NewGuid().ToString("D"), Version = "local-test" };
        var challenge = new AuthenticationChallenge { Id = Guid.NewGuid().ToString("N"), Nonce = "local-test-nonce", ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(2) };
        var response = new InitialAuthenticationResponse(InitialConnectionStatus.Success);
        var sequence = new MockSequence();
        _api.InSequence(sequence).Setup(a => a.PostAsync<AuthenticationChallenge>("auth/challenge", login, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);
        _api.InSequence(sequence).Setup(a => a.PostAsync<InitialAuthenticationResponse>("auth/login", login, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _client.Login(login);

        result.Should().BeSameAs(response);
        login.Identity.Should().Be(identity);
        login.ProtocolVersion.Should().Be(ProtocolVersion.CURRENT);
        login.AuthenticationEpoch.Should().Be(AuthenticationProtocol.Epoch);
        using var publicKey = RSA.Create();
        publicKey.ImportRSAPublicKey(login.PublicKey, out _);
        publicKey.VerifyData(AuthenticationProtocol.GetMessage(login, challenge), login.Signature,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pss).Should().BeTrue();
    }

    [Test]
    public async Task Refresh_UsesTheDedicatedRefreshEndpoint()
    {
        var request = new RefreshTokensData();
        var response = new RefreshTokensResponse(RefreshTokensStatus.RefreshTokenOk);
        _api.Setup(a => a.PostAsync<RefreshTokensResponse>("auth/refreshTokens", request, It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var result = await _client.RefreshAuthenticationTokens(request);

        result.Should().BeSameAs(response);
    }

    [Test]
    public async Task RefreshFailure_DoesNotFallBackToLogin()
    {
        var error = new ApiException("Refresh failed");
        _api.Setup(a => a.PostAsync<RefreshTokensResponse>("auth/refreshTokens", It.IsAny<object>(), It.IsAny<CancellationToken>())).ThrowsAsync(error);

        var act = () => _client.RefreshAuthenticationTokens(new RefreshTokensData());

        (await act.Should().ThrowAsync<ApiException>()).Which.Should().BeSameAs(error);
        _api.Verify(a => a.PostAsync<RefreshTokensResponse>("auth/refreshTokens", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        _api.VerifyNoOtherCalls();
    }

    private sealed class MissingChallengeHandler(string body) : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent(body) });
        }
    }
}
