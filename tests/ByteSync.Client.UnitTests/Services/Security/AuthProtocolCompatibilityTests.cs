using System.Net;
using System.Security.Cryptography;
using ByteSync.Business.Configurations;
using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.Versions;
using ByteSync.Exceptions;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Services.Communications.Api;
using FluentAssertions;
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

    [Test]
    public async Task OlderRelayWithoutChallengeEndpoint_IsRejectedWithoutCallingLegacyLogin()
    {
        var error = new ApiException("Challenge request failed", new ApiException("Not Found", HttpStatusCode.NotFound));
        _api.Setup(a => a.PostAsync<AuthenticationChallenge>("auth/challenge", It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(error);

        var act = () => _client.Login(new LoginData());

        (await act.Should().ThrowAsync<ApiException>()).Which.Should().BeSameAs(error);
        _api.Verify(a => a.PostAsync<InitialAuthenticationResponse>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
        _api.Verify(a => a.PostAsync<AuthenticationChallenge>("auth/challenge", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        _api.VerifyNoOtherCalls();
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
}
