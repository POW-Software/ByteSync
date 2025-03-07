using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.Versions;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Loaders;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Services;
using ByteSync.ServerCommon.Services.Clients;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Tests.Services;

[TestFixture]
public class ClientSoftwareVersionServiceTests
{
    private IClientSoftwareVersionSettingsRepository _mockClientSoftwareVersionSettingsRepository;
    private IClientSoftwareVersionSettingsLoader _mockClientSoftwareVersionSettingsLoader;
    private ILogger<ClientSoftwareVersionService> _mockLogger;
    private IOptions<AppSettings> _mockAppSettings;
    private ClientSoftwareVersionService _service;

    [SetUp]
    public void SetUp()
    {
        _mockClientSoftwareVersionSettingsRepository = A.Fake<IClientSoftwareVersionSettingsRepository>();
        _mockClientSoftwareVersionSettingsLoader = A.Fake<IClientSoftwareVersionSettingsLoader>();
        _mockLogger = A.Fake<ILogger<ClientSoftwareVersionService>>();
        _mockAppSettings = Options.Create(new AppSettings { SkipClientsVersionCheck = false });

        _service = new ClientSoftwareVersionService(
            _mockClientSoftwareVersionSettingsRepository,
            _mockClientSoftwareVersionSettingsLoader,
            _mockLogger,
            _mockAppSettings);
    }

    [Test]
    public async Task IsClientVersionAllowed_ShouldReturnTrue_WhenSkipClientsVersionCheckIsTrue()
    {
        // Arrange
        _mockAppSettings.Value.SkipClientsVersionCheck = true;
        var loginData = new LoginData { Version = "1.0.0" };

        // Act
        var result = await _service.IsClientVersionAllowed(loginData);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task IsClientVersionAllowed_ShouldReturnFalse_WhenMandatoryVersionIsNull()
    {
        // Arrange
        A.CallTo(() => _mockClientSoftwareVersionSettingsRepository.GetUnique())
            .Returns(Task.FromResult<ClientSoftwareVersionSettings?>(null));
        var loginData = new LoginData { Version = "1.0.0" };

        // Act
        var result = await _service.IsClientVersionAllowed(loginData);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task IsClientVersionAllowed_ShouldReturnTrue_WhenClientVersionIsAllowed()
    {
        // Arrange
        var settings = new ClientSoftwareVersionSettings
        {
            MinimalVersion = new SoftwareVersion { Version = "1.0.0" }
        };
        A.CallTo(() => _mockClientSoftwareVersionSettingsRepository.GetUnique())
            .Returns(Task.FromResult<ClientSoftwareVersionSettings?>(settings));
        var loginData = new LoginData { Version = "1.0.1" };

        // Act
        var result = await _service.IsClientVersionAllowed(loginData);

        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public async Task IsClientVersionAllowed_ShouldReturnTrue_WhenDifferentFieldsCountAndClientVersionIsAllowed()
    {
        // Arrange
        var settings = new ClientSoftwareVersionSettings
        {
            MinimalVersion = new SoftwareVersion { Version = "2023.1.1.0" }
        };
        A.CallTo(() => _mockClientSoftwareVersionSettingsRepository.GetUnique())
            .Returns(Task.FromResult<ClientSoftwareVersionSettings?>(settings));
        var loginData = new LoginData { Version = "2023.1.1" };

        // Act
        var result = await _service.IsClientVersionAllowed(loginData);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task IsClientVersionAllowed_ShouldReturnFalse_WhenClientVersionIsNotAllowed()
    {
        // Arrange
        var settings = new ClientSoftwareVersionSettings
        {
            MinimalVersion = new SoftwareVersion { Version = "1.0.1" }
        };
        A.CallTo(() => _mockClientSoftwareVersionSettingsRepository.GetUnique())
            .Returns(Task.FromResult<ClientSoftwareVersionSettings?>(settings));
        var loginData = new LoginData { Version = "1.0.0" };

        // Act
        var result = await _service.IsClientVersionAllowed(loginData);

        // Assert
        result.Should().BeFalse();
    }
    
    [Test]
    public async Task IsClientVersionAllowed_ShouldReturnFalse_WhenDifferentFieldsCountAndClientVersionIsNotAllowed()
    {
        // Arrange
        var settings = new ClientSoftwareVersionSettings
        {
            MinimalVersion = new SoftwareVersion { Version = "1.0.1.0" }
        };
        A.CallTo(() => _mockClientSoftwareVersionSettingsRepository.GetUnique())
            .Returns(Task.FromResult<ClientSoftwareVersionSettings?>(settings));
        var loginData = new LoginData { Version = "1.0.0" };

        // Act
        var result = await _service.IsClientVersionAllowed(loginData);

        // Assert
        result.Should().BeFalse();
    }
}