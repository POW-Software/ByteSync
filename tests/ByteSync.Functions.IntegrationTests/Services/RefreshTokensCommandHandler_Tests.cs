using Autofac;
using ByteSync.ServerCommon.Repositories;
using ByteSync.Functions.IntegrationTests.TestHelpers.Autofac;

namespace ByteSync.Functions.IntegrationTests.Services;

[TestFixture]
public class RefreshTokensCommandHandler_Tests
{
    private ILifetimeScope _scope;

    [SetUp]
    public void Setup()
    {
        _scope = GlobalTestSetup.Container.BeginLifetimeScope(builder =>
        {
            builder.RegisterModule(new RepositoriesModule(true));
            builder.RegisterModule(new LoadersModule(false));
        });
    }

    [TearDown]
    public void Teardown()
    {
        _scope.Dispose();
    }

    [Test]
    public async Task RefreshTokens_WithValidToken_UpdatesTokenAndReturnsSuccess()
    {
        // Arrange
        var clientsRepository = _scope.Resolve<ClientsRepository>();
        var clientInstanceId = "test-client-instance";
        var clientId = "test-client-id";
        var refreshToken = "valid-refresh-token";
        var version = "1.0.0";
        var osPlatform = ByteSync.Common.Business.Misc.OSPlatforms.Windows;
        var ipAddress = "localhost";
        var now = DateTimeOffset.UtcNow;

        await clientsRepository.AddOrUpdate(clientInstanceId, c => {
            if (c == null) c = new ByteSync.ServerCommon.Business.Auth.Client();
            c.ClientId = clientId;
            c.ClientInstanceId = clientInstanceId;
            c.Version = version;
            c.OsPlatform = osPlatform;
            c.IpAddress = ipAddress;
            c.RefreshToken = new ByteSync.ServerCommon.Business.Auth.RefreshToken {
                Token = refreshToken,
                Expires = now.AddMinutes(10),
                Created = now,
                CreatedByIp = ipAddress,
                Revoked = null,
                RevokedByIp = null,
                ReplacedByToken = null
            };
            return c;
        });

        var handler = _scope.Resolve<ByteSync.ServerCommon.Commands.Authentication.RefreshTokensCommandHandler>();
        var request = new ByteSync.ServerCommon.Commands.Authentication.RefreshTokensRequest(
            new ByteSync.Common.Business.Auth.RefreshTokensData {
                Token = refreshToken,
                ClientInstanceId = clientInstanceId,
                Version = version,
                OsPlatform = osPlatform
            },
            ipAddress
        );

        // Act
        var response = await handler.Handle(request, default);

        // Assert
        Assert.That(response.RefreshTokensStatus, Is.EqualTo(ByteSync.Common.Business.Auth.RefreshTokensStatus.RefreshTokenOk));
        Assert.That(response.AuthenticationTokens, Is.Not.Null);


        // Assert that the refresh token was updated (rotated)
        var updatedClient = await clientsRepository.Get(clientInstanceId);
        Assert.That(updatedClient, Is.Not.Null);
        Assert.That(updatedClient.RefreshToken, Is.Not.Null);
        Assert.That(updatedClient.RefreshToken.Token, Is.Not.EqualTo(refreshToken), "Refresh token should be rotated and not equal to the old token.");
        Assert.That(updatedClient.RefreshToken.Expires, Is.GreaterThan(now), "New refresh token should have a later expiration.");

    }
}