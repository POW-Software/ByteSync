using System.Text.Json;
using ByteSync.Common.Business.Versions;
using ByteSync.Interfaces.Updates;

namespace ByteSync.Services.Updates;

class AvailableUpdatesLister : IAvailableUpdatesLister
{
    public List<SoftwareVersion> GetAvailableUpdates()
    {
        string contents;
        using (var wc = new System.Net.WebClient())
        {
            string url = BuildUrl("updates.json");
                
            contents = wc.DownloadString(url);
        }

        var softwareVersions = JsonSerializer.Deserialize<List<SoftwareVersion>>(contents);

        return softwareVersions;
    }
        
    public string GetUrl(SoftwareVersionFile softwareFileVersion)
    {
        string result = BuildUrl(softwareFileVersion.FileName);

        return result;
    }
        
    private string BuildUrl(string fileToDownload)
    {
        string baseUrl = "https://powgeneral1.blob.core.windows.net/pow-bytesync-pub/";

        string fileUrl = fileToDownload.TrimStart('/');

        string url = baseUrl + fileUrl;

        return url;
    }
}