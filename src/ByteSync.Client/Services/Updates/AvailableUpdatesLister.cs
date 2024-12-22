using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ByteSync.Common.Business.Versions;
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

        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        };
        var softwareVersions = JsonSerializer.Deserialize<List<SoftwareVersion>>(contents, options);

        return softwareVersions!;
    }
}