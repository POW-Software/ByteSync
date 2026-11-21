using Autofac;
using ByteSync.Common.Business.Auth;
using ByteSync.ServerCommon.Repositories;
using ByteSync.Functions.IntegrationTests.TestHelpers.Autofac;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Commands.Authentication;
using ByteSync.TestsCommon.TestHelpers;
using FluentAssertions;

namespace ByteSync.Functions.IntegrationTests.Integration.Services;

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
        using var identity = new AuthenticationTestIdentity();
        var clientInstanceId = identity.ClientInstanceId;
        var clientId = identity.Identity;
        var refreshToken = "valid-refresh-token";
        var version = "2.0.0";
        var osPlatform = Common.Business.Misc.OSPlatforms.Windows;
        var ipAddress = "localhost";
        var now = DateTimeOffset.UtcNow;

        await clientsRepository.AddOrUpdate(clientInstanceId, c => {
            c ??= new Client();
            c.Identity = identity.Identity;
            c.AuthenticationEpoch = AuthenticationProtocol.Epoch;
            c.ClientId = clientId;
            c.ClientInstanceId = clientInstanceId;
            c.Version = version;
            c.OsPlatform = osPlatform;
            c.IpAddress = ipAddress;
            c.RefreshToken = new RefreshToken {
                AuthenticationEpoch = AuthenticationProtocol.Epoch,
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

        response.RefreshTokensStatus.Should().Be(RefreshTokensStatus.RefreshTokenOk);
        response.AuthenticationTokens.Should().NotBeNull();

        var updatedClient = await clientsRepository.Get(clientInstanceId);
        updatedClient.Should().NotBeNull();
        updatedClient!.Identity.Should().Be(identity.Identity);
        updatedClient.AuthenticationEpoch.Should().Be(AuthenticationProtocol.Epoch);
        updatedClient.RefreshToken.Should().NotBeNull();
        updatedClient.RefreshToken!.Token.Should().NotBe(refreshToken);
        updatedClient.RefreshToken.Expires.Should().BeAfter(now);
        updatedClient.RefreshToken.AuthenticationEpoch.Should().Be(AuthenticationProtocol.Epoch);
        response.AuthenticationTokens!.RefreshToken.Should().Be(updatedClient.RefreshToken.Token);
    }
}
