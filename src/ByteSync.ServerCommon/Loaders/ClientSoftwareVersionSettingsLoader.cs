using System.Text.Json;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Loaders;
using Microsoft.Extensions.Logging;
using Polly;
using PowSoftware.Common.Business.Versions;

namespace ByteSync.ServerCommon.Loaders;

public class ClientSoftwareVersionSettingsLoader : IClientSoftwareVersionSettingsLoader
{
    private readonly ILogger<ClientSoftwareVersionSettingsLoader> _logger;

    public ClientSoftwareVersionSettingsLoader(ILogger<ClientSoftwareVersionSettingsLoader> logger)
    {
        _logger = logger;
    }

    public async Task<ClientSoftwareVersionSettings> Load()
    {
        SoftwareVersion newMandatoryVersionCandidate = null;

        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(3 * (retryAttempt + 1)));
            
        await policy.Execute(async () =>
        {
            string contents;
            using (var wc = new HttpClient())
            {
                contents = await wc.GetStringAsync("https://powgeneral1.blob.core.windows.net/pow-bytesync-pub/updates.json");
            }

            var softwareUpdates = JsonSerializer.Deserialize<List<SoftwareVersion>>(contents);

            if (softwareUpdates != null)
            {
                newMandatoryVersionCandidate = softwareUpdates.FirstOrDefault(u => u.Level == PriorityLevel.Mandatory);
            }
        });
        
        _logger.LogInformation("MandatoryVersion is now: {version}", newMandatoryVersionCandidate!.Version);

        ClientSoftwareVersionSettings clientSoftwareVersionSettings = new ClientSoftwareVersionSettings
        {
            MandatoryVersion = newMandatoryVersionCandidate
        };

        return clientSoftwareVersionSettings;
    }
}