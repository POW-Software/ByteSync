using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ByteSync.Common.Business.Versions;
using ByteSync.Common.Controls.Json;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Updates;
using Microsoft.Extensions.Configuration;

namespace ByteSync.Services.Updates;

class AvailableUpdatesLister : IAvailableUpdatesLister
{
    private readonly IConfiguration _configuration;
    private readonly IEnvironmentService _environmentService;

    public AvailableUpdatesLister(IConfiguration configuration, IEnvironmentService environmentService)
    {
        _configuration = configuration;
        _environmentService = environmentService;
    }
    
    public async Task<List<SoftwareVersion>> GetAvailableUpdates()
    {
        string contents;
        using (var httpClient = new HttpClient())
        {
            string url = _configuration["UpdatesDefinitionUrl"]!;
        
            contents = await httpClient.GetStringAsync(url);
        }
        
        var softwareVersions = JsonHelper.Deserialize<List<SoftwareVersion>>(contents);
        
        KeepOnlyRelevantVersions(softwareVersions);

        return softwareVersions!;
    }

    private void KeepOnlyRelevantVersions(List<SoftwareVersion> softwareVersions)
    {
        if (!_environmentService.IsPortableApplication)
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                softwareVersions.RemoveAll(v => v.Level != PriorityLevel.Recommended);
            }
        }
    }
}