using ByteSync.Common.Controls.Json;
using ByteSync.ServerCommon.Business.Messages;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Loaders;
using ByteSync.ServerCommon.Loaders;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using Moq;

namespace ByteSync.ServerCommon.Tests.Loaders;

[TestFixture]
public class MessageDefinitionsLoaderTests
{
    private ILogger<MessageDefinitionsLoader> _mockLogger;
    private IOptions<AppSettings> _mockAppSettings;
    private HttpClient _httpClient;
    private MessageDefinitionsLoader _loader;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = A.Fake<ILogger<MessageDefinitionsLoader>>();
        _mockAppSettings = Options.Create(new AppSettings 
        { 
            MessagesDefinitionsUrl = "https://test.example.com/messages.json" 
        });
        _httpClient = new HttpClient(new MockHttpMessageHandler(""));
        _loader = new MessageDefinitionsLoader(_mockAppSettings, _mockLogger, _httpClient);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient?.Dispose();
    }

    [Test]
    public async Task Load_ShouldReturnMessageDefinitions_WhenValidDataIsRetrieved()
    {
        // Arrange
        var messageDefinitions = new List<MessageDefinition>
        {
            new MessageDefinition
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
            new MessageDefinition
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
        _loader = new MessageDefinitionsLoader(_mockAppSettings, _mockLogger, _httpClient);

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
        _loader = new MessageDefinitionsLoader(_mockAppSettings, _mockLogger, _httpClient);

        // Act & Assert
        await FluentActions.Awaiting(() => _loader.Load())
            .Should().ThrowAsync<Exception>();
    }

    [Test]
    public async Task Load_ShouldHandleEmptyList_AndReturnEmptyList()
    {
        // Arrange
        var messageDefinitions = new List<MessageDefinition>();
        var jsonContent = JsonHelper.Serialize(messageDefinitions);
        _httpClient = new HttpClient(new MockHttpMessageHandler(jsonContent));
        _loader = new MessageDefinitionsLoader(_mockAppSettings, _mockLogger, _httpClient);

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
        _loader = new MessageDefinitionsLoader(_mockAppSettings, _mockLogger, _httpClient);

        // Act & Assert
        await FluentActions.Awaiting(() => _loader.Load())
            .Should().ThrowAsync<Exception>();
    }

    [Test]
    public async Task Load_ShouldHandleMessageDefinitionsWithEmptyMessages()
    {
        // Arrange
        var messageDefinitions = new List<MessageDefinition>
        {
            new MessageDefinition
            {
                Id = "msg1",
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(1),
                Message = new Dictionary<string, string>() // Empty messages
            }
        };

        var jsonContent = JsonHelper.Serialize(messageDefinitions);
        _httpClient = new HttpClient(new MockHttpMessageHandler(jsonContent));
        _loader = new MessageDefinitionsLoader(_mockAppSettings, _mockLogger, _httpClient);

        // Act
        var result = await _loader.Load();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("msg1");
        result[0].Message.Should().BeEmpty();
    }

    [Test]
    public async Task Load_ShouldHandleMessageDefinitionsWithNullMessages()
    {
        // Arrange
        var messageDefinitions = new List<MessageDefinition>
        {
            new MessageDefinition
            {
                Id = "msg1",
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(1),
                Message = null! // Null messages
            }
        };

        var jsonContent = JsonHelper.Serialize(messageDefinitions);
        _httpClient = new HttpClient(new MockHttpMessageHandler(jsonContent));
        _loader = new MessageDefinitionsLoader(_mockAppSettings, _mockLogger, _httpClient);

        // Act
        var result = await _loader.Load();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("msg1");
    }
}

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly string _responseContent;
    private readonly HttpStatusCode _statusCode;

    public MockHttpMessageHandler(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responseContent = responseContent;
        _statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseContent, Encoding.UTF8, "application/json")
        };

        return Task.FromResult(response);
    }
} 