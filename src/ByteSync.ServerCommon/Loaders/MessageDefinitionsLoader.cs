using ByteSync.Common.Controls.Json;
using ByteSync.ServerCommon.Business.Messages;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Loaders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace ByteSync.ServerCommon.Loaders;

public class MessageDefinitionsLoader : IMessageDefinitionsLoader
{
    private readonly AppSettings _appSettings;
    private readonly ILogger<MessageDefinitionsLoader> _logger;
    private readonly HttpClient _httpClient;

    public MessageDefinitionsLoader(
        IOptions<AppSettings> appSettings, 
        ILogger<MessageDefinitionsLoader> logger,
        HttpClient httpClient)
    {
        _appSettings = appSettings.Value;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<List<MessageDefinition>> Load()
    {
        List<MessageDefinition>? messageDefinitions = null;

        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(3 * (retryAttempt + 1)));

        await policy.Execute(async () =>
        {
            _logger.LogInformation("Loading messages from {url}", _appSettings.MessagesDefinitionsUrl);
            var contents = await _httpClient.GetStringAsync(_appSettings.MessagesDefinitionsUrl);

            messageDefinitions = JsonHelper.Deserialize<List<MessageDefinition>>(contents);
        });

        if (messageDefinitions == null)
        {
            throw new Exception("Failed to load messages");
        }

        return messageDefinitions!;
    }
}
