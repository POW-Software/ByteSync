using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.Misc;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Commands.Authentication;
using ByteSync.ServerCommon.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using ByteSync.ServerCommon.Tests.Helpers;
using FakeItEasy;
using FluentAssertions;

namespace ByteSync.ServerCommon.Tests.Security;

public class LoginIdentityBindingSecurityTests
{
    [TestCase("same", true)]
    [TestCase("different", false)]
    [TestCase("legacy", false)]
    [TestCase("new", true)]
    public async Task Login_CannotAdoptAnExistingInstanceOrLegacyClientAlias(string recordType, bool expected)
    {
        var identity = new string('A', 64);
        var login = new LoginData { ClientId = "victim-legacy-alias", Identity = identity, ClientInstanceId = identity + "_" + Guid.NewGuid().ToString("D"),
            OsPlatform = OSPlatforms.Windows, Version = "2026.1" };
        Client? existing = recordType == "new" ? null : new Client(identity, login.ClientInstanceId, "older", OSPlatforms.Windows, "127.0.0.1")
        { Identity = recordType == "legacy" ? null : recordType == "same" ? identity : new string('B', 64),
            AuthenticationEpoch = recordType == "legacy" ? 0 : AuthenticationProtocol.Epoch };
        Client? saved = null;
        var repository = A.Fake<IClientsRepository>();
        A.CallTo(() => repository.AddOrUpdate(login.ClientInstanceId, A<Func<Client?, Client?>>._))
            .ReturnsLazily((string id, Func<Client?, Client?> update) => { saved = update(existing); return UpdateResultBuilder.BuildAddOrUpdateResult(saved, false); });
        var challenges = A.Fake<IAuthenticationChallengeService>();
        A.CallTo(() => challenges.VerifyAndConsume(login)).Returns(true);
        var versions = A.Fake<IClientSoftwareVersionService>();
        A.CallTo(() => versions.IsClientVersionAllowed(login)).Returns(true);
        var tokens = A.Fake<ITokensFactory>();
        A.CallTo(() => tokens.BuildTokens(A<Client>._)).Returns(new JwtTokens("test-token", new RefreshToken { Token = "test-refresh", Expires = DateTimeOffset.UtcNow.AddMinutes(5) }, 300));
        var handler = new AuthenticateCommandHandler(tokens, A.Fake<IByteSyncEndpointFactory>(), repository, versions, challenges);

        var result = await handler.Handle(new AuthenticateRequest(login, "127.0.0.2"), CancellationToken.None);

        result.IsSuccess.Should().Be(expected);
        if (expected)
        {
            saved!.ClientId.Should().Be(login.ClientId);
            saved.Identity.Should().Be(identity);
            saved.Version.Should().Be(login.Version);
            saved.AuthenticationEpoch.Should().Be(AuthenticationProtocol.Epoch);
        }
        else A.CallTo(() => tokens.BuildTokens(A<Client>._)).MustNotHaveHappened();
    }
}
