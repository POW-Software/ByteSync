using Autofac;
using ByteSync.Common.Business.Auth;
using ByteSync.ServerCommon.Repositories;
using ByteSync.Functions.IntegrationTests.TestHelpers.Autofac;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Commands.Authentication;

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
            builder.RegisterType<RefreshTokensCommandHandler>()
                    .AsSelf()
                    .InstancePerLifetimeScope();
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
        var clientInstanceId = "integration-client-instance";
        var clientId = "integration-client-id";
        var refreshToken = "valid-refresh-token";
        var version = "2.0.0";
        var osPlatform = Common.Business.Misc.OSPlatforms.Windows;
        var ipAddress = "localhost";
        var now = DateTimeOffset.UtcNow;

        await clientsRepository.AddOrUpdate(clientInstanceId, c => {
            c ??= new Client();
            c.ClientId = clientId;
            c.ClientInstanceId = clientInstanceId;
            c.Version = version;
            c.OsPlatform = osPlatform;
            c.IpAddress = ipAddress;
            c.RefreshToken = new RefreshToken {
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

        var handler = _scope.Resolve<RefreshTokensCommandHandler>();
        var request = new RefreshTokensRequest(
            new RefreshTokensData {
                Token = refreshToken,
                ClientInstanceId = clientInstanceId,
                Version = version,
                OsPlatform = osPlatform
            },
            ipAddress
        );

        // Act
        var response = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.That(response.RefreshTokensStatus, Is.EqualTo(RefreshTokensStatus.RefreshTokenOk));
        Assert.That(response.AuthenticationTokens, Is.Not.Null);

        // Assert that the refresh token was updated (rotated)
        var updatedClient = await clientsRepository.Get(clientInstanceId);
        Assert.That(updatedClient, Is.Not.Null);
        Assert.That(updatedClient.RefreshToken, Is.Not.Null);
        Assert.That(updatedClient.RefreshToken.Token, Is.Not.EqualTo(refreshToken), "Refresh token should be rotated and not equal to the old token.");
        Assert.That(updatedClient.RefreshToken.Expires, Is.GreaterThan(now), "New refresh token should have a later expiration.");
    }
}