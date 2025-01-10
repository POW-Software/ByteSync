using System.Net.Http;
using System.Threading.Tasks;
using ByteSync.Common.Business.Versions;
using ByteSync.Common.Controls.Json;
using ByteSync.Interfaces.Updates;
using Microsoft.Extensions.Configuration;

namespace ByteSync.Services.Updates;

class AvailableUpdatesLister : IAvailableUpdatesLister
{
    private readonly IConfiguration _configuration;

    public AvailableUpdatesLister(IConfiguration configuration)
    {
        _configuration = configuration;
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

        return softwareVersions!;
    }
}