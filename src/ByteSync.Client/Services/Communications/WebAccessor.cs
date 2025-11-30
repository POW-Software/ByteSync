using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Services.Localizations;

namespace ByteSync.Services.Communications;

class WebAccessor : IWebAccessor
{
    private readonly ILocalizationService _localizationService;
    
    public WebAccessor(ILocalizationService localizationManager)
    {
        _localizationService = localizationManager;
    }
    
    public async Task OpenDocumentationUrl()
    {
        var url = GetDocumentationUrl();
        
        await DoOpenUrlAsync(url);
    }
    
    public async Task OpenDocumentationUrl(Dictionary<string, string> pathPerLanguage)
    {
        var url = GetDocumentationUrl();
        
        if (pathPerLanguage.TryGetValue(_localizationService.CurrentCultureDefinition?.Code ?? "en", out var path))
        {
            url += path.TrimStart('/');
        }
        
        await DoOpenUrlAsync(url);
    }
    
    private string GetDocumentationUrl()
    {
        var languageCode = "";
        
        if (Equals(_localizationService.CurrentCultureDefinition?.Code, "fr"))
        {
            languageCode = "fr/";
        }
        
        var url = $"https://www.bytesyncapp.com/{languageCode}documentation/";
        
        return url;
    }
    
    public async Task OpenByteSyncWebSite()
    {
        var url = "https://www.bytesyncapp.com";
        
        if (Equals(_localizationService.CurrentCultureDefinition?.Code, "fr"))
        {
            url = "https://www.bytesyncapp.com/fr/";
        }
        
        await DoOpenUrlAsync(url);
    }
    
    public async Task OpenPowSoftwareWebSite()
    {
        var url = "https://www.pow-software.com";
        
        if (Equals(_localizationService.CurrentCultureDefinition?.Code, "fr"))
        {
            url = "https://www.pow-software.com/fr/";
        }
        
        await DoOpenUrlAsync(url);
    }
    
    public async Task OpenByteSyncRepository()
    {
        var url = "https://github.com/POW-Software/ByteSync";
        
        await DoOpenUrlAsync(url);
    }
    
    public async Task OpenReleaseNotes()
    {
        var url = "https://www.bytesyncapp.com/documentation/release-notes/";
        
        if (Equals(_localizationService.CurrentCultureDefinition?.Code, "fr"))
        {
            url = "https://www.bytesyncapp.com/fr/documentation/notes-de-version/";
        }
        
        await DoOpenUrlAsync(url);
    }
    
    public async Task OpenReleaseNotes(Version version)
    {
        var url = GetReleaseNotesUrl();
        
        var versionPart = "bytesync-";
        
        versionPart += "version-"; // ici, gérer la langue si besoin
        
        versionPart += version.Major + "-" + version.Minor;
        
        versionPart += "/";
        
        await DoOpenUrlAsync(url + versionPart);
    }
    
    private string GetReleaseNotesUrl()
    {
        var url = "https://www.bytesyncapp.com/documentation/release-notes/";
        
        if (Equals(_localizationService.CurrentCultureDefinition?.Code, "fr"))
        {
            url = "https://www.bytesyncapp.com/fr/documentation/notes-de-version/";
        }
        
        return url;
    }
    
    public async Task OpenUrl(string url)
    {
        if (Equals(_localizationService.CurrentCultureDefinition?.Code, "fr"))
        {
            if (Equals(url, "https://www.bytesyncapp.com/documentation/"))
            {
                url = "https://www.bytesyncapp.com/fr/documentation/";
            }
        }
        
        await DoOpenUrlAsync(url);
    }
    
    private async Task DoOpenUrlAsync(string url)
    {
        await Task.Run(() => DoOpenUrl(url));
    }
    
    private static void DoOpenUrl(string url)
    {
        try
        {
            Process.Start(url);
        }
        catch
        {
            // https://stackoverflow.com/questions/4580263/how-to-open-in-default-browser-in-c-sharp
            // https://brockallen.com/2016/09/24/process-start-for-urls-on-net-core/
            
            // hack because of this: https://github.com/dotnet/corefx/issues/10361
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                var cmdTrustedPath = Path.Combine(Environment.SystemDirectory, "cmd.exe");
                Process.Start(new ProcessStartInfo(cmdTrustedPath, $"/c start {url}")
                    { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var xdgTrustedPath = "/usr/bin/xdg-open";
                Process.Start(xdgTrustedPath, url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var openTrustedPath = "/usr/bin/open";
                Process.Start(openTrustedPath, url);
            }
            else
            {
                throw;
            }
        }
    }
}