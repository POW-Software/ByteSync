using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;

namespace ByteSync.ServerCommon.Repositories;

public class ClientSoftwareVersionSettingsRepository : BaseRepository<ClientSoftwareVersionSettings>, IClientSoftwareVersionSettingsRepository
{
    public ClientSoftwareVersionSettingsRepository(ICacheService cacheService) : base(cacheService)
    {
    }

    public override EntityType EntityType => EntityType.ClientSoftwareVersionSettings;
    
    public const string UniqueKey = "Unique";
    
    public Task<ClientSoftwareVersionSettings?> GetUnique()
    {
        return Get(UniqueKey);
    }

    public Task SaveUnique(ClientSoftwareVersionSettings clientSoftwareVersionSettings)
    {
        return Save(UniqueKey, clientSoftwareVersionSettings);
    }
}