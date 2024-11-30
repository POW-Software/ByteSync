using ByteSync.ServerCommon.Business.Settings;

namespace ByteSync.ServerCommon.Interfaces.Loaders;

public interface IClientSoftwareVersionSettingsLoader
{
    Task<ClientSoftwareVersionSettings> Load();
}