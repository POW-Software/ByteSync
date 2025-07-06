using ByteSync.Common.Controls.Json;
using ByteSync.ServerCommon.Business.Announcements;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Loaders;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using ByteSync.ServerCommon.Tests.Helpers;

namespace ByteSync.ServerCommon.Tests.Loaders;

[TestFixture]
public class AnnouncementsLoaderTests
{
    private ILogger<AnnouncementsLoader> _mockLogger;
    private IOptions<AppSettings> _mockAppSettings;
    private HttpClient _httpClient;
    private AnnouncementsLoader _loader;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = A.Fake<ILogger<AnnouncementsLoader>>();
        _mockAppSettings = Options.Create(new AppSettings
        {
            AnnouncementsDefinitionUrl = "https://test.example.com/messages.json"
        });
        _httpClient = new HttpClient(new MockHttpMessageHandler(""));
        _loader = new AnnouncementsLoader(_mockAppSettings, _mockLogger, _httpClient);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient?.Dispose();
    }

    [Test]
    public async Task Load_ShouldReturnAnnouncements_WhenValidDataIsRetrieved()
    {
        // Arrange
        var messageDefinitions = new List<Announcement>
        {
            new Announcement
            {
                Id = "msg1",
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(1),
                Message = new Dictionary<string, string>
                {
                    { "en", "Test message in English" },
                    { "fr", "Message de test en français" }
                }
            },
            new Announcement
            {
                Id = "msg2",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(7),
                Message = new Dictionary<string, string>
                {
                    { "en", "Another test message" },
                    { "fr", "Un autre message de test" }
                }
            }
        };

        var jsonContent = JsonHelper.Serialize(messageDefinitions);
        _httpClient = new HttpClient(new MockHttpMessageHandler(jsonContent));
        _loader = new AnnouncementsLoader(_mockAppSettings, _mockLogger, _httpClient);

        // Act
        var result = await _loader.Load();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Id.Should().Be("msg1");
        result[0].Message["en"].Should().Be("Test message in English");
        result[0].Message["fr"].Should().Be("Message de test en français");
        result[1].Id.Should().Be("msg2");
    }

    [Test]
    public async Task Load_ShouldThrowException_WhenHttpRequestFails()
    {
        // Arrange
        _httpClient = new HttpClient(new MockHttpMessageHandler("", HttpStatusCode.NotFound));
        _loader = new AnnouncementsLoader(_mockAppSettings, _mockLogger, _httpClient);

        // Act & Assert
        await FluentActions.Awaiting(() => _loader.Load())
            .Should().ThrowAsync<Exception>();
    }

    [Test]
    public async Task Load_ShouldHandleEmptyList_AndReturnEmptyList()
    {
        // Arrange
        var messageDefinitions = new List<Announcement>();
        var jsonContent = JsonHelper.Serialize(messageDefinitions);
        _httpClient = new HttpClient(new MockHttpMessageHandler(jsonContent));
        _loader = new AnnouncementsLoader(_mockAppSettings, _mockLogger, _httpClient);

        // Act
        var result = await _loader.Load();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Test]
    public async Task Load_ShouldThrowException_WhenDeserializationReturnsNull()
    {
        // Arrange
        var malformedJson = "{ invalid json }";
        _httpClient = new HttpClient(new MockHttpMessageHandler(malformedJson));
        _loader = new AnnouncementsLoader(_mockAppSettings, _mockLogger, _httpClient);

        // Act & Assert
        await FluentActions.Awaiting(() => _loader.Load())
            .Should().ThrowAsync<Exception>();
    }

    [Test]
    public async Task Load_ShouldHandleAnnouncementsWithEmptyMessages()
    {
        // Arrange
        var messageDefinitions = new List<Announcement>
        {
            new Announcement
            {
                Id = "msg1",
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(1),
                Message = new Dictionary<string, string>() // Empty messages
            }
        };

        var jsonContent = JsonHelper.Serialize(messageDefinitions);
        _httpClient = new HttpClient(new MockHttpMessageHandler(jsonContent));
        _loader = new AnnouncementsLoader(_mockAppSettings, _mockLogger, _httpClient);

        // Act
        var result = await _loader.Load();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("msg1");
        result[0].Message.Should().BeEmpty();
    }

    [Test]
    public async Task Load_ShouldHandleAnnouncementsWithNullMessages()
    {
        // Arrange
        var messageDefinitions = new List<Announcement>
        {
            new Announcement
            {
                Id = "msg1",
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(1),
                Message = null! // Null messages
            }
        };

        var jsonContent = JsonHelper.Serialize(messageDefinitions);
        _httpClient = new HttpClient(new MockHttpMessageHandler(jsonContent));
        _loader = new AnnouncementsLoader(_mockAppSettings, _mockLogger, _httpClient);

        // Act
        var result = await _loader.Load();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("msg1");
    }
}