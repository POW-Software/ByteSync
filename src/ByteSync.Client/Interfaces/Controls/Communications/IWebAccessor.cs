using System.Threading.Tasks;

namespace ByteSync.Interfaces.Controls.Communications
{
    public interface IWebAccessor
    {
        Task OpenDocumentationUrl();

        Task OpenByteSyncWebSite();
        
        Task OpenByteSyncRepository();

        Task OpenPrivacy();

        Task OpenTermsOfUse();
        
        Task OpenReleaseNotes();
        
        Task OpenReleaseNotes(Version version);

        Task OpenUrl(string url);
        
        Task OpenPricing();
        
        Task OpenJoinBeta();
        
        Task OpenAboutOpenBeta();
        
        Task OpenPowSoftwareWebSite();
    }
}