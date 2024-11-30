using ByteSync.Business.Configurations;
using ByteSync.Common.Business.Serials;

namespace ByteSync.Interfaces
{
    public interface IApplicationSettingsRepository
    {
        ApplicationSettings GetCurrentApplicationSettings();

        ApplicationSettings UpdateCurrentApplicationSettings(Action<ApplicationSettings> handler, bool saveAfter = true);

        ProductSerialDescription? ProductSerialDescription { get; }
        
        string EncryptionPassword { get; }
    }
}