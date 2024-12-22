using System.Text.Json;
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
        SoftwareVersion? newMandatoryVersionCandidate = null;

        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(3 * (retryAttempt + 1)));
            
        await policy.Execute(async () =>
        {
            string contents;
            using (var wc = new HttpClient())
            {
                contents = await wc.GetStringAsync(_appSettings.UpdatesDefinitionUrl);
            }

            var softwareUpdates = JsonSerializer.Deserialize<List<SoftwareVersion>>(contents)!;

            if (softwareUpdates != null)
            {
                newMandatoryVersionCandidate = softwareUpdates.FirstOrDefault(u => u.Level == PriorityLevel.Minimal);
            }
        });
        
        if (newMandatoryVersionCandidate == null)
        {
            throw new Exception("Failed to load mandatory version");
        }
        
        _logger.LogInformation("MandatoryVersion is now: {version}", newMandatoryVersionCandidate!.Version);

        ClientSoftwareVersionSettings clientSoftwareVersionSettings = new ClientSoftwareVersionSettings
        {
            MandatoryVersion = newMandatoryVersionCandidate
        };

        return clientSoftwareVersionSettings;
    }
}