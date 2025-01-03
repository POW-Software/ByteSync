using System.Text.Json;
using System.Text.Json.Serialization;
using ByteSync.Common.Business.Versions;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Loaders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace ByteSync.ServerCommon.Loaders;

public class ClientSoftwareVersionSettingsLoader : IClientSoftwareVersionSettingsLoader
{
    private readonly AppSettings _appSettings;
    private readonly ILogger<ClientSoftwareVersionSettingsLoader> _logger;

    public ClientSoftwareVersionSettingsLoader(IOptions<AppSettings> appSettings, ILogger<ClientSoftwareVersionSettingsLoader> logger)
    {
        _appSettings = appSettings.Value;
        _logger = logger;
    }

    public async Task<ClientSoftwareVersionSettings> Load()
    {
        SoftwareVersion? newMinimalVersionCandidate = null;

        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(3 * (retryAttempt + 1)));
            
        await policy.Execute(async () =>
        {
            string contents;
            using (var wc = new HttpClient())
            {
                _logger.LogInformation("Loading minimal version from {url}", _appSettings.UpdatesDefinitionUrl);
                contents = await wc.GetStringAsync(_appSettings.UpdatesDefinitionUrl);
            }

            var options = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            };
            var softwareUpdates = JsonSerializer.Deserialize<List<SoftwareVersion>>(contents, options)!;

            if (softwareUpdates != null)
            {
                newMinimalVersionCandidate = softwareUpdates.FirstOrDefault(u => u.Level == PriorityLevel.Minimal);
            }
        });
        
        if (newMinimalVersionCandidate == null)
        {
            throw new Exception("Failed to load minimal version");
        }
        
        _logger.LogInformation("Minimal version is now: {version}", newMinimalVersionCandidate!.Version);

        ClientSoftwareVersionSettings clientSoftwareVersionSettings = new ClientSoftwareVersionSettings
        {
            MinimalVersion = newMinimalVersionCandidate
        };

        return clientSoftwareVersionSettings;
    }
}