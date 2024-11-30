using ByteSync.ServerCommon.Business.Settings;

namespace ByteSync.ServerCommon.Interfaces.Repositories;

public interface IClientSoftwareVersionSettingsRepository : IRepository<ClientSoftwareVersionSettings>
{
    public Task<ClientSoftwareVersionSettings?> GetUnique();
    
    public Task SaveUnique(ClientSoftwareVersionSettings clientSoftwareVersionSettings);
}