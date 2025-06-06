using System.Threading.Tasks;

namespace ByteSync.Interfaces.Controls.Communications;

public interface IWebAccessor
{
    Task OpenDocumentationUrl();
    
    Task OpenDocumentationUrl(Dictionary<string, string> pathPerLanguage);

    Task OpenByteSyncWebSite();
        
    Task OpenPowSoftwareWebSite();
        
    Task OpenByteSyncRepository();
        
    Task OpenReleaseNotes();
        
    Task OpenReleaseNotes(Version version);

    Task OpenUrl(string url);
}