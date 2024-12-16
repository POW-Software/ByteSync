using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ByteSync.Common.Business.Versions;
using ByteSync.Interfaces.Updates;

namespace ByteSync.Services.Updates;

class AvailableUpdatesLister : IAvailableUpdatesLister
{
    public async Task<List<SoftwareVersion>> GetAvailableUpdates()
    {
        string contents;
        using (var httpClient = new HttpClient())
        {
            string url = BuildUrl("updates.json");
        
            contents = await httpClient.GetStringAsync(url);
        }

        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        };
        var softwareVersions = JsonSerializer.Deserialize<List<SoftwareVersion>>(contents, options);

        return softwareVersions!;
    }
        
    public string GetUrl(SoftwareVersionFile softwareFileVersion)
    {
        string result = BuildUrl(softwareFileVersion.FileName);

        return result;
    }
        
    private string BuildUrl(string fileToDownload)
    {
        string baseUrl = "https://powbytesyncupdates.blob.core.windows.net/updates/";

        string fileUrl = fileToDownload.TrimStart('/');

        string url = baseUrl + fileUrl;

        return url;
    }
}