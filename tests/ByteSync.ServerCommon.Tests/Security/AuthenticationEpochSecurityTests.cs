using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using ByteSync.Common.Business.Auth;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Commands.Authentication;
using ByteSync.ServerCommon.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Security;
using ByteSync.ServerCommon.Tests.Helpers;
using FakeItEasy;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;

namespace ByteSync.ServerCommon.Tests.Security;

public class AuthenticationEpochSecurityTests
{
    private static Client Client() => new() { Identity = new string('A', 64), AuthenticationEpoch = AuthenticationProtocol.Epoch,
        ClientInstanceId = new string('A', 64) + "_" + Guid.NewGuid().ToString("D") };

    [TestCase(0)]
    [TestCase(-1)]
    public void OldJwtClaims_CannotBecomeVerifiedByLookingUpAMigratedClient(int epoch)
    {
        var client = Client();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(AuthConstants.CLAIM_CLIENT_INSTANCE_ID, client.ClientInstanceId),
            new Claim(AuthConstants.CLAIM_IDENTITY, client.Identity!), new Claim(AuthConstants.CLAIM_AUTHENTICATION_EPOCH, epoch.ToString()) }));

        var act = () => ClientAuthentication.ValidateClaims(client, principal);

        act.Should().Throw<SecurityTokenValidationException>();
    }

    [Test]
    public void VerifiedClaims_MustMatchTheRegisteredInstanceAndIdentity()
    {
        var client = Client();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(AuthConstants.CLAIM_CLIENT_INSTANCE_ID, client.ClientInstanceId),
            new Claim(AuthConstants.CLAIM_IDENTITY, client.Identity!), new Claim(AuthConstants.CLAIM_AUTHENTICATION_EPOCH, AuthenticationProtocol.Epoch.ToString()) }));
        ClientAuthentication.ValidateClaims(client, principal);
        client.Identity = new string('B', 64);

        var act = () => ClientAuthentication.ValidateClaims(client, principal);

        act.Should().Throw<SecurityTokenValidationException>();
    }

    [Test]
    public async Task OldRefreshToken_CannotMintAVerifiedToken()
    {
        var client = Client();
        client.RefreshToken = new RefreshToken { Token = "old-refresh", AuthenticationEpoch = 0, Expires = DateTimeOffset.UtcNow.AddHours(1) };
        var repository = A.Fake<IClientsRepository>();
        A.CallTo(() => repository.AddOrUpdate(A<string>._, A<Func<Client?, Client?>>._))
            .ReturnsLazily((string id, Func<Client?, Client?> update) => UpdateResultBuilder.BuildAddOrUpdateResult(update(client), false));
        var tokens = A.Fake<ITokensFactory>();
        var handler = new RefreshTokensCommandHandler(tokens, repository);

        var result = await handler.Handle(new RefreshTokensRequest(new RefreshTokensData { ClientInstanceId = client.ClientInstanceId, Token = "old-refresh" }, "127.0.0.1"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        A.CallTo(() => tokens.BuildTokens(A<Client>._)).MustNotHaveHappened();
    }
    [TestCase(0, false)]
    [TestCase(1, true)]
    public async Task SignedJwt_ThroughRegisteredBearerAuthentication_RequiresCurrentEpoch(int epoch, bool allowed)
    {
        var client = Client();
        var repository = A.Fake<IClientsRepository>();
        A.CallTo(() => repository.Get(client.ClientInstanceId)).Returns(client);
        const string secret = "local-unit-test-signing-key-with-at-least-32-bytes";
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(repository);
        ByteSync.ServerCommon.Helpers.JwtHelper.AddJwtAuthentication(services, secret);
        await using var provider = services.BuildServiceProvider();
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(AuthConstants.ISSUER, AuthConstants.AUDIENCE,
            new[] { new Claim(AuthConstants.CLAIM_CLIENT_INSTANCE_ID, client.ClientInstanceId),
                new Claim(AuthConstants.CLAIM_IDENTITY, client.Identity!), new Claim(AuthConstants.CLAIM_AUTHENTICATION_EPOCH, epoch.ToString()) },
            expires: DateTime.UtcNow.AddMinutes(1), signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(System.Text.Encoding.ASCII.GetBytes(secret)), SecurityAlgorithms.HmacSha256));
        var context = new Microsoft.AspNetCore.Http.DefaultHttpContext { RequestServices = provider };
        context.Request.Headers.Authorization = "Bearer " + new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);

        var result = await context.AuthenticateAsync(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme);

        result.Succeeded.Should().Be(allowed);
    }

}
