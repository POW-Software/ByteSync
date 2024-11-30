using System.Threading.Tasks;

namespace ByteSync.Interfaces.Controls.Communications
{
    public interface IWebAccessor
    {
        Task OpenSupportUrl();

        Task OpenByteSyncWebSite();

        Task OpenPrivacy();

        Task OpenTermsOfUse();
        
        Task OpenReleaseNotes();
        
        Task OpenReleaseNotes(Version version);

        Task OpenUrl(string url);
        
        Task OpenPricing();
        
        Task OpenJoinBeta();
        
        Task OpenAboutOpenBeta();
    }
}