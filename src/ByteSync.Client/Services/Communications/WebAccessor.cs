using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications;
using Splat;

namespace ByteSync.Services.Communications;

class WebAccessor : IWebAccessor
{
    private readonly ILocalizationService _localizationService;

    public WebAccessor(ILocalizationService? localizationManager = null)
    {
        _localizationService = localizationManager ?? Locator.Current.GetService<ILocalizationService>();
    }

    public async Task OpenDocumentationUrl()
    {
        var languageCode = "";

        if (Equals(_localizationService.CurrentCultureDefinition?.Code, "fr"))
        {
            languageCode = "fr/";
        }

        var url = $"https://www.bytesyncapp.com/{languageCode}documentation/";

        await DoOpenUrlAsync(url);
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

    public async Task OpenByteSyncRepository()
    {
        var url = "https://github.com/POW-Software/ByteSync";

        await DoOpenUrlAsync(url);
    }

    public async Task OpenPrivacy()
    {
        var url = "https://www.bytesyncapp.com/support/manual/getting-started/privacy-policy/";

        if (Equals(_localizationService.CurrentCultureDefinition?.Code, "fr"))
        {
            url = "https://www.bytesyncapp.com/fr/support/manuel/demarrage/politique-de-confidentialite/";
        }

        await DoOpenUrlAsync(url);
    }

    public async Task OpenTermsOfUse()
    {
        var url = "https://www.bytesyncapp.com/support/manual/getting-started/terms-of-use/";

        if (Equals(_localizationService.CurrentCultureDefinition?.Code, "fr"))
        {
            url = "https://www.bytesyncapp.com/fr/support/manuel/demarrage/conditions-dutilisation/";
        }

        await DoOpenUrlAsync(url);
    }

    public async Task OpenReleaseNotes()
    {
        var url = "https://www.bytesyncapp.com/support/manual/release-notes/";

        if (Equals(_localizationService.CurrentCultureDefinition?.Code, "fr"))
        {
            url = "https://www.bytesyncapp.com/fr/support/manuel/notes-de-version/";
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
        var url = "https://www.bytesyncapp.com/support/manual/release-notes/";

        if (Equals(_localizationService.CurrentCultureDefinition?.Code, "fr"))
        {
            url = "https://www.bytesyncapp.com/fr/support/manuel/notes-de-version/";
        }

        return url;
    }

    public async Task OpenUrl(string url)
    {
        if (Equals(_localizationService.CurrentCultureDefinition?.Code, "fr"))
        {
            if (Equals(url, "https://www.bytesyncapp.com/#pricing"))
            {
                url = "www.bytesyncapp.com/fr/#tarifs";
            }

            if (Equals(url, "https://www.bytesyncapp.com/support/"))
            {
                url = "https://www.bytesyncapp.com/fr/support/";
            }

            if (Equals(url, "https://www.bytesyncapp.com/support/manual/getting-started/privacy-policy/"))
            {
                url = "https://www.bytesyncapp.com/fr/support/manuel/demarrage/politique-de-confidentialite/";
            }

            if (Equals(url, "https://www.bytesyncapp.com/support/manual/getting-started/terms-of-use/"))
            {
                url = "https://www.bytesyncapp.com/fr/support/manuel/demarrage/conditions-dutilisation/";
            }
        }

        await DoOpenUrlAsync(url);
    }

    public async Task OpenPricing()
    {
        var url = "https://www.bytesyncapp.com/#pricing";

        if (Equals(_localizationService.CurrentCultureDefinition?.Code, "fr"))
        {
            url = "https://www.bytesyncapp.com/fr/#tarifs";
        }

        await DoOpenUrlAsync(url);
    }

    public async Task OpenJoinBeta()
    {
        var url = "https://www.bytesyncapp.com/#beta";

        if (Equals(_localizationService.CurrentCultureDefinition?.Code, "fr"))
        {
            url = "https://www.bytesyncapp.com/fr/#beta";
        }

        await DoOpenUrlAsync(url);
    }

    public async Task OpenAboutOpenBeta()
    {
        var url = "https://www.bytesyncapp.com/blog/bytesync-open-beta/";

        if (Equals(_localizationService.CurrentCultureDefinition?.Code, "fr"))
        {
            url = "https://www.bytesyncapp.com/fr/blog/beta-ouverte-de-bytesync/";
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
            System.Diagnostics.Process.Start(url);
        }
        catch
        {
            // https://stackoverflow.com/questions/4580263/how-to-open-in-default-browser-in-c-sharp
            // https://brockallen.com/2016/09/24/process-start-for-urls-on-net-core/

            // hack because of this: https://github.com/dotnet/corefx/issues/10361
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd", $"/c start {url}")
                    { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                System.Diagnostics.Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                System.Diagnostics.Process.Start("open", url);
            }
            else
            {
                throw;
            }
        }
    }
}