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
using ByteSync.Services.Applications;
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

    private static ByteSync.Common.Business.EndPoints.PublicKeyCheckData Peer(RSA key, string salt = "exchange-salt")
    {
        var publicKey = key.ExportRSAPublicKey();
        var identity = AuthenticationProtocol.GetIdentity(publicKey);
        return new() { IssuerPublicKeyInfo = new() { ClientId = "ABCD-EFG-HIJ", PublicKey = publicKey },
            IssuerClientInstanceId = identity + "_" + Guid.NewGuid().ToString("D"), Salt = salt };
    }

    [TestCase("valid", true)]
    [TestCase("alias-metadata", true)]
    [TestCase("wrong-identity", false)]
    [TestCase("wrong-instance", false)]
    [TestCase("old-exchange", false)]
    [TestCase("missing-instance", false)]
    [TestCase("missing-identity", false)]
    [TestCase("missing-exchange", false)]
    public async Task TrustConfirmation_RequiresExpectedInstanceIdentityAndExchange(string scenario, bool accepted)
    {
        using var key = RSA.Create(2048);
        var peer = Peer(key);
        var repository = new TrustProcessPublicKeysRepository(NullLogger<TrustProcessPublicKeysRepository>.Instance);
        await repository.Start("session");
        var pending = await repository.ResetPeerTrustProcessData("session", peer);
        var confirmation = new ByteSync.Common.Business.Sessions.Cloud.Connections.PublicKeyValidationParameters
        {
            IssuerClientId = peer.IssuerPublicKeyInfo.ClientId, IssuerClientInstanceId = peer.IssuerClientInstanceId,
            IssuerIdentity = AuthenticationProtocol.GetIdentity(peer.IssuerPublicKeyInfo.PublicKey),
            ExchangeId = peer.Salt, IsValidated = true
        };
        switch (scenario)
        {
            case "alias-metadata": confirmation.IssuerClientId = "different-readable-alias"; break;
            case "wrong-identity": confirmation.IssuerIdentity = new string('A', 64); break;
            case "wrong-instance": confirmation.IssuerClientInstanceId = confirmation.IssuerIdentity + "_" + Guid.NewGuid().ToString("D"); break;
            case "old-exchange": confirmation.ExchangeId = "old-salt"; break;
            case "missing-instance": confirmation.IssuerClientInstanceId = null!; break;
            case "missing-identity": confirmation.IssuerIdentity = null!; break;
            case "missing-exchange": confirmation.ExchangeId = null!; break;
        }

        await repository.SetOtherPartyChecked("session", confirmation);

        pending.HasOtherPartyValidatedPublicKey.Should().Be(accepted);
        pending.OtherPartyHasFinished().Should().Be(accepted);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task CollidingAliases_AndReplayedConfirmations_CannotCompleteAnotherExchange(bool sameKey)
    {
        using var firstKey = RSA.Create(2048);
        using var secondKey = RSA.Create(2048);
        var first = Peer(firstKey);
        var second = Peer(sameKey ? firstKey : secondKey);
        var repository = new TrustProcessPublicKeysRepository(NullLogger<TrustProcessPublicKeysRepository>.Instance);
        await repository.Start("session");
        var firstPending = await repository.ResetPeerTrustProcessData("session", first);
        var secondPending = await repository.ResetPeerTrustProcessData("session", second);
        var message = new ByteSync.Common.Business.Sessions.Cloud.Connections.PublicKeyValidationParameters
        {
            IssuerClientId = first.IssuerPublicKeyInfo.ClientId, IssuerClientInstanceId = first.IssuerClientInstanceId,
            IssuerIdentity = AuthenticationProtocol.GetIdentity(first.IssuerPublicKeyInfo.PublicKey), ExchangeId = first.Salt, IsValidated = false
        };

        await repository.SetOtherPartyChecked("session", message);
        var duplicate = await repository.ResetPeerTrustProcessData("session", first);
        message.IsValidated = true;
        await repository.SetOtherPartyChecked("session", message);
        firstPending.HasOtherPartyValidatedPublicKey.Should().BeFalse();
        firstPending.OtherPartyHasFinished().Should().BeTrue();
        duplicate.Should().BeSameAs(firstPending);
        secondPending.OtherPartyHasFinished().Should().BeFalse();
        first.Salt = "new-exchange";
        var replacement = await repository.ResetPeerTrustProcessData("session", first);
        await repository.SetOtherPartyChecked("session", message);
        replacement.OtherPartyHasFinished().Should().BeFalse();
        message.ExchangeId = first.Salt;
        await repository.SetOtherPartyChecked("session", message);
        replacement.HasOtherPartyValidatedPublicKey.Should().BeTrue();
        secondPending.HasOtherPartyValidatedPublicKey.Should().BeFalse();
        var wrongSession = () => repository.SetOtherPartyChecked("other-session", message);
        await wrongSession.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [TestCase("instance")]
    [TestCase("salt")]
    public async Task InvalidTrustExchange_CannotBeRegistered(string field)
    {
        using var key = RSA.Create(2048);
        var peer = Peer(key);
        if (field == "instance") peer.IssuerClientInstanceId = "ABCD-EFG-HIJ_legacy";
        else peer.Salt = "";
        var repository = new TrustProcessPublicKeysRepository(NullLogger<TrustProcessPublicKeysRepository>.Instance);
        await repository.Start("session");

        var act = () => repository.ResetPeerTrustProcessData("session", peer);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Test]
    public void EnvironmentInitialization_PreservesTheStoredReadableIdAndExistingKeys()
    {
        var clientId = _settings.ClientId;
        var publicKey = _settings.DecodedRsaPublicKey!.ToArray();
        var privateKey = _settings.DecodedRsaPrivateKey!.ToArray();
        _settings.InitializeTrustedPublicKeys();
        _settings.AddTrustedKey(new TrustedPublicKey { ClientId = "PEER-CLI-ENT", PublicKey = _settings.DecodedRsaPublicKey!,
            ValidationDate = DateTimeOffset.UtcNow });
        var trustedKeys = _settings.TrustedPublicKeys;
        var identity = AuthenticationProtocol.GetIdentity(publicKey);
        var environment = new EnvironmentService(Mock.Of<IMsixPfnParser>());

        environment.SetClientIdentity(clientId, identity);

        environment.ClientId.Should().Be(clientId).And.MatchRegex("^[A-Z]{4}-[A-Z]{3}-[A-Z]{3}$");
        environment.Identity.Should().Be(identity).And.NotBe(clientId);
        AuthenticationProtocol.IsInstanceOf(environment.ClientInstanceId, identity).Should().BeTrue();
        _settings.ClientId.Should().Be(clientId);
        _settings.DecodedRsaPublicKey.Should().Equal(publicKey);
        _settings.DecodedRsaPrivateKey.Should().Equal(privateKey);
        _settings.TrustedPublicKeys.Should().Be(trustedKeys);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("ABCD-EFG-HIJ")]
    [TestCase("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void EnvironmentInitialization_RejectsAnAliasOrInvalidFingerprintAsIdentity(string? identity)
    {
        var environment = new EnvironmentService(Mock.Of<IMsixPfnParser>());

        var act = () => environment.SetClientIdentity(_settings.ClientId, identity!);

        act.Should().Throw<ArgumentException>();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void EnvironmentInitialization_RejectsAMissingReadableId(string? clientId)
    {
        var environment = new EnvironmentService(Mock.Of<IMsixPfnParser>());
        var identity = AuthenticationProtocol.GetIdentity(_settings.DecodedRsaPublicKey!);

        var act = () => environment.SetClientIdentity(clientId!, identity);

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void TwoInstallationsWithOneKey_KeepDistinctAliasesAndInstancesOfTheSameIdentity()
    {
        var identity = AuthenticationProtocol.GetIdentity(_settings.DecodedRsaPublicKey!);
        var first = new EnvironmentService(Mock.Of<IMsixPfnParser>());
        var second = new EnvironmentService(Mock.Of<IMsixPfnParser>());

        first.SetClientIdentity("ABCD-EFG-HIJ", identity);
        second.SetClientIdentity("KLMN-OPQ-RST", identity);

        first.ClientId.Should().Be("ABCD-EFG-HIJ");
        second.ClientId.Should().Be("KLMN-OPQ-RST");
        first.Identity.Should().Be(second.Identity);
        first.ClientInstanceId.Should().NotBe(second.ClientInstanceId);
        AuthenticationProtocol.IsInstanceOf(first.ClientInstanceId, identity).Should().BeTrue();
        AuthenticationProtocol.IsInstanceOf(second.ClientInstanceId, identity).Should().BeTrue();
    }

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
        var clientId = _settings.ClientId;
        var publicKeyBeforeLogin = _settings.DecodedRsaPublicKey!.ToArray();
        var privateKeyBeforeLogin = _settings.DecodedRsaPrivateKey!.ToArray();
        _settings.InitializeTrustedPublicKeys();
        _settings.AddTrustedKey(new TrustedPublicKey { ClientId = "PEER-CLI-ENT", PublicKey = _settings.DecodedRsaPublicKey!,
            ValidationDate = DateTimeOffset.UtcNow });
        var trustedKeys = _settings.TrustedPublicKeys;
        var identity = AuthenticationProtocol.GetIdentity(_settings.DecodedRsaPublicKey!);
        var login = new LoginData { ClientId = "caller-supplied-alias", ClientInstanceId = identity + "_" + Guid.NewGuid().ToString("D"), Version = "local-test" };
        var challenge = new AuthenticationChallenge { Id = Guid.NewGuid().ToString("N"), Nonce = "local-test-nonce", ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(2) };
        var response = new InitialAuthenticationResponse(InitialConnectionStatus.Success);
        var sequence = new MockSequence();
        _api.InSequence(sequence).Setup(a => a.PostAsync<AuthenticationChallenge>("auth/challenge", login, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);
        _api.InSequence(sequence).Setup(a => a.PostAsync<InitialAuthenticationResponse>("auth/login", login, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _client.Login(login);

        result.Should().BeSameAs(response);
        login.ClientId.Should().Be(clientId).And.NotBe(identity);
        _settings.ClientId.Should().Be(clientId);
        _settings.DecodedRsaPublicKey.Should().Equal(publicKeyBeforeLogin);
        _settings.DecodedRsaPrivateKey.Should().Equal(privateKeyBeforeLogin);
        _settings.TrustedPublicKeys.Should().Be(trustedKeys);
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
