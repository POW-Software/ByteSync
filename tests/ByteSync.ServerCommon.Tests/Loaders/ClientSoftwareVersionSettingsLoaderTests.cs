using ByteSync.Common.Business.Versions;
using ByteSync.Common.Controls.Json;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Loaders;
using ByteSync.ServerCommon.Loaders;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;

namespace ByteSync.ServerCommon.Tests.Loaders;

[TestFixture]
public class ClientSoftwareVersionSettingsLoaderTests
{
    private ILogger<ClientSoftwareVersionSettingsLoader> _mockLogger;
    private IOptions<AppSettings> _mockAppSettings;
    private HttpClient _httpClient;
    private ClientSoftwareVersionSettingsLoader _loader;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = A.Fake<ILogger<ClientSoftwareVersionSettingsLoader>>();
        _mockAppSettings = Options.Create(new AppSettings 
        { 
            UpdatesDefinitionUrl = "https://test.example.com/updates.json" 
        });
        _httpClient = new HttpClient(new MockHttpMessageHandler(""));
        _loader = new ClientSoftwareVersionSettingsLoader(_mockAppSettings, _mockLogger, _httpClient);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient?.Dispose();
    }

    [Test]
    public async Task Load_ShouldReturnClientSoftwareVersionSettings_WhenValidDataIsRetrieved()
    {
        // Arrange
        var softwareVersions = new List<SoftwareVersion>
        {
            new SoftwareVersion 
            { 
                ProductCode = "ByteSync", 
                Version = "1.0.0", 
                Level = PriorityLevel.Minimal 
            },
            new SoftwareVersion 
            { 
                ProductCode = "ByteSync", 
                Version = "1.1.0", 
                Level = PriorityLevel.Recommended 
            }
        };

        var jsonContent = JsonHelper.Serialize(softwareVersions);
        _httpClient = new HttpClient(new MockHttpMessageHandler(jsonContent));
        _loader = new ClientSoftwareVersionSettingsLoader(_mockAppSettings, _mockLogger, _httpClient);

        // Act
        var result = await _loader.Load();

        // Assert
        result.Should().NotBeNull();
        result.MinimalVersion.Should().NotBeNull();
        result.MinimalVersion!.Version.Should().Be("1.0.0");
        result.MinimalVersion.Level.Should().Be(PriorityLevel.Minimal);
        result.MinimalVersion.ProductCode.Should().Be("ByteSync");
    }

    [Test]
    public async Task Load_ShouldThrowException_WhenNoMinimalVersionFound()
    {
        // Arrange
        var softwareVersions = new List<SoftwareVersion>
        {
            new SoftwareVersion 
            { 
                ProductCode = "ByteSync", 
                Version = "1.1.0", 
                Level = PriorityLevel.Recommended 
            },
            new SoftwareVersion 
            { 
                ProductCode = "ByteSync", 
                Version = "1.2.0", 
                Level = PriorityLevel.Optional 
            }
        };

        var jsonContent = JsonHelper.Serialize(softwareVersions);
        _httpClient = new HttpClient(new MockHttpMessageHandler(jsonContent));
        _loader = new ClientSoftwareVersionSettingsLoader(_mockAppSettings, _mockLogger, _httpClient);

        // Act & Assert
        var exception = await FluentActions.Awaiting(() => _loader.Load())
            .Should().ThrowAsync<Exception>();
        exception.WithMessage("Failed to load minimal version");
    }

    [Test]
    public async Task Load_ShouldThrowException_WhenHttpRequestFails()
    {
        // Arrange
        _httpClient = new HttpClient(new MockHttpMessageHandler("", HttpStatusCode.NotFound));
        _loader = new ClientSoftwareVersionSettingsLoader(_mockAppSettings, _mockLogger, _httpClient);

        // Act & Assert
        await FluentActions.Awaiting(() => _loader.Load())
            .Should().ThrowAsync<Exception>();
    }

    [Test]
    public async Task Load_ShouldHandleEmptyList_AndThrowException()
    {
        // Arrange
        var softwareVersions = new List<SoftwareVersion>();
        var jsonContent = JsonHelper.Serialize(softwareVersions);
        _httpClient = new HttpClient(new MockHttpMessageHandler(jsonContent));
        _loader = new ClientSoftwareVersionSettingsLoader(_mockAppSettings, _mockLogger, _httpClient);

        // Act & Assert
        var exception = await FluentActions.Awaiting(() => _loader.Load())
            .Should().ThrowAsync<Exception>();
        exception.WithMessage("Failed to load minimal version");
    }

    [Test]
    public async Task Load_ShouldHandleNullList_AndThrowException()
    {
        // Arrange
        List<SoftwareVersion>? softwareVersions = null;
        var jsonContent = JsonHelper.Serialize(softwareVersions);
        _httpClient = new HttpClient(new MockHttpMessageHandler(jsonContent));
        _loader = new ClientSoftwareVersionSettingsLoader(_mockAppSettings, _mockLogger, _httpClient);

        // Act & Assert
        var exception = await FluentActions.Awaiting(() => _loader.Load())
            .Should().ThrowAsync<Exception>();
        exception.WithMessage("Failed to deserialize JSON.");
    }

    [Test]
    public async Task Load_ShouldHandleMalformedJson_AndThrowException()
    {
        // Arrange
        var malformedJson = "{ invalid json }";
        _httpClient = new HttpClient(new MockHttpMessageHandler(malformedJson));
        _loader = new ClientSoftwareVersionSettingsLoader(_mockAppSettings, _mockLogger, _httpClient);

        // Act & Assert
        await FluentActions.Awaiting(() => _loader.Load())
            .Should().ThrowAsync<Exception>();
    }
}