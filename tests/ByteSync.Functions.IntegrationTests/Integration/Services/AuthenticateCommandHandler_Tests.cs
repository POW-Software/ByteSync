using Autofac;
using ByteSync.Common.Business.Auth;
using ByteSync.Functions.IntegrationTests.TestHelpers.Autofac;
using ByteSync.ServerCommon.Commands.Authentication;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using FakeItEasy;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.TestsCommon.TestHelpers;
using FluentAssertions;

namespace ByteSync.Functions.IntegrationTests.Integration.Services;

[TestFixture]
public class AuthenticateCommandHandler_Tests
{
    private ILifetimeScope _scope;

    [SetUp]
    public void Setup()
    {
        var fakeVersionService = A.Fake<IClientSoftwareVersionService>();
        A.CallTo(() => fakeVersionService.IsClientVersionAllowed(A<LoginData>.Ignored)).Returns(Task.FromResult(true));

        _scope = GlobalTestSetup.Container.BeginLifetimeScope(builder =>
        {
            builder.RegisterModule(new RepositoriesModule(true));
            builder.RegisterModule(new LoadersModule(false));
            builder.RegisterType<AuthenticateCommandHandler>()
                .AsSelf()
                .InstancePerLifetimeScope();
            builder.RegisterInstance(fakeVersionService)
                .As<IClientSoftwareVersionService>()
                .SingleInstance();
        });
    }

    [TearDown]
    public void Teardown()
    {
        _scope.Dispose();
    }

    [Test]
    public async Task Authenticate_WithValidData_ReturnsSuccessResponse()
    {
        using var identity = new AuthenticationTestIdentity();
        var loginData = identity.CreateLogin();
        var challengeService = _scope.Resolve<IAuthenticationChallengeService>();
        identity.Sign(loginData, await challengeService.Create(loginData));
        var ipAddress = "127.0.0.1";

        var request = new AuthenticateRequest(loginData, ipAddress);
        var handler = _scope.Resolve<AuthenticateCommandHandler>();

        // Act
        var response = await handler.Handle(request, CancellationToken.None);

        response.IsSuccess.Should().BeTrue();
        response.InitialConnectionStatus.Should().Be(InitialConnectionStatus.Success);
        response.AuthenticationTokens.Should().NotBeNull();
        response.EndPoint.Should().NotBeNull();
        response.EndPoint!.ClientInstanceId.Should().Be(identity.ClientInstanceId);

        var replay = await handler.Handle(request, CancellationToken.None);

        replay.InitialConnectionStatus.Should().Be(InitialConnectionStatus.InvalidProof);
        replay.AuthenticationTokens.Should().BeNull();
    }
}
