using Autofac;
using ByteSync.Common.Business.Versions;
using ByteSync.Functions.IntegrationTests.TestHelpers.Autofac;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using ByteSync.ServerCommon.Loaders;
using FakeItEasy;

namespace ByteSync.Functions.IntegrationTests.Services;

[TestFixture]
public class AuthenticateCommandHandler_Tests
{
    private ILifetimeScope _scope;

    [SetUp]
    public void Setup()
    {
        var fakeVersionService = FakeItEasy.A.Fake<ByteSync.ServerCommon.Interfaces.Services.Clients.IClientSoftwareVersionService>();
        FakeItEasy.A.CallTo(() => fakeVersionService.IsClientVersionAllowed(FakeItEasy.A<ByteSync.Common.Business.Auth.LoginData>.Ignored)).Returns(Task.FromResult(true));

        _scope = GlobalTestSetup.Container.BeginLifetimeScope(builder =>
        {
            builder.RegisterModule(new RepositoriesModule(true));
            builder.RegisterModule(new LoadersModule(false));
            builder.RegisterType<ByteSync.ServerCommon.Commands.Authentication.AuthenticateCommandHandler>()
                .AsSelf()
                .InstancePerLifetimeScope();
            builder.RegisterInstance(fakeVersionService)
                .As<ByteSync.ServerCommon.Interfaces.Services.Clients.IClientSoftwareVersionService>()
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
        // Arrange
        var repository = _scope.Resolve<IClientSoftwareVersionSettingsRepository>();
        ClientSoftwareVersionSettings settings = new ClientSoftwareVersionSettings
        {
            MinimalVersion = new SoftwareVersion
            {
                Version = "1.0.0"
            }
        };
        await repository.SaveUnique(settings);
        
        var clientId = "integration-client-id";
        var clientInstanceId = "integration-client-instance";
        var version = "2.0.0";
        var osPlatform = ByteSync.Common.Business.Misc.OSPlatforms.Windows;
        var ipAddress = "127.0.0.1";

        var loginData = new ByteSync.Common.Business.Auth.LoginData
        {
            ClientId = clientId,
            ClientInstanceId = clientInstanceId,
            Version = version,
            OsPlatform = osPlatform
        };

        var request = new ByteSync.ServerCommon.Commands.Authentication.AuthenticateRequest(loginData, ipAddress);
        var handler = _scope.Resolve<ByteSync.ServerCommon.Commands.Authentication.AuthenticateCommandHandler>();

        // Act
        var response = await handler.Handle(request, default);

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.IsSuccess, Is.True, "Authentication should succeed with valid data");
        Assert.That(response.InitialConnectionStatus, Is.EqualTo(ByteSync.Common.Business.Auth.InitialConnectionStatus.Success));
        Assert.That(response.AuthenticationTokens, Is.Not.Null, "AuthenticationTokens should not be null");
        Assert.That(response.EndPoint, Is.Not.Null, "EndPoint should not be null");
    }
}