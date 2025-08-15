using Autofac;
using ByteSync.Common.Business.Auth;
using ByteSync.Functions.IntegrationTests.TestHelpers.Autofac;
using ByteSync.ServerCommon.Commands.Authentication;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using FakeItEasy;

namespace ByteSync.Functions.IntegrationTests.Services;

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
        var clientId = "integration-client-id";
        var clientInstanceId = "integration-client-instance";
        var version = "2.0.0";
        var osPlatform = Common.Business.Misc.OSPlatforms.Windows;
        var ipAddress = "127.0.0.1";

        var loginData = new LoginData
        {
            ClientId = clientId,
            ClientInstanceId = clientInstanceId,
            Version = version,
            OsPlatform = osPlatform
        };

        var request = new AuthenticateRequest(loginData, ipAddress);
        var handler = _scope.Resolve<AuthenticateCommandHandler>();

        // Act
        var response = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.IsSuccess, Is.True, "Authentication should succeed with valid data");
        Assert.That(response.InitialConnectionStatus, Is.EqualTo(InitialConnectionStatus.Success));
        Assert.That(response.AuthenticationTokens, Is.Not.Null, "AuthenticationTokens should not be null");
        Assert.That(response.EndPoint, Is.Not.Null, "EndPoint should not be null");
    }
}