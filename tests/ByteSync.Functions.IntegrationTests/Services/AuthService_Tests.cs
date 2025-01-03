using Autofac;
using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.Versions;
using ByteSync.Functions.IntegrationTests.Helpers.Autofac;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Loaders;
using ByteSync.ServerCommon.Repositories;
using ByteSync.ServerCommon.Services;
using FakeItEasy;
using FluentAssertions;

namespace ByteSync.Functions.IntegrationTests.Services;

[TestFixture]
public class AuthService_Tests
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
    public async Task TestWithSettings()
    {
        // Arrange
        await DeleteCurrentClientSoftwareVersionSettings();
        await DeleteClient("clientInstanceId1");
        await MockClientSoftwareVersionSettings("2.0.0");

        LoginData loginData = new LoginData();
        loginData.Version = "2.1.0";
        loginData.OsPlatform = OSPlatforms.Windows;
        loginData.ClientInstanceId = "clientInstanceId1";
        loginData.ClientId = "clientId1";
        
        string ipAddress = "localhost";
        
        // Act
        var authService = _scope.Resolve<AuthService>();
        var response = await authService.Authenticate(loginData, ipAddress);

        // Assert
        response.IsSuccess.Should().BeTrue();
        response.InitialConnectionStatus.Should().Be(InitialConnectionStatus.Success);
        response.EndPoint.Should().NotBeNull();
        response.EndPoint!.Version.Should().Be("2.1.0");
        response.EndPoint!.ClientInstanceId.Should().Be(loginData.ClientInstanceId);
    }

    private Task MockClientSoftwareVersionSettings(string version)
    {
        var loader = _scope.Resolve<IClientSoftwareVersionSettingsLoader>();

        ClientSoftwareVersionSettings clientSoftwareVersionSettings = new ClientSoftwareVersionSettings()
        {
            MinimalVersion = new SoftwareVersion
            {
                ProductCode = "BS",
                Level = PriorityLevel.Minimal,
                Version = version
            }
        };
        
        A.CallTo(() => loader.Load())
            .Returns(clientSoftwareVersionSettings);
        
        return Task.CompletedTask;
    }

    private async Task DeleteCurrentClientSoftwareVersionSettings()
    {
        var clientSoftwareVersionSettingsRepository = _scope.Resolve<ClientSoftwareVersionSettingsRepository>();
        await clientSoftwareVersionSettingsRepository.Delete(ClientSoftwareVersionSettingsRepository.UniqueKey);
    }
    
    private async Task DeleteClient(string clientInstanceId)
    {
        var clientsRepository = _scope.Resolve<ClientsRepository>();
        await clientsRepository.Delete(clientInstanceId);
    }
}