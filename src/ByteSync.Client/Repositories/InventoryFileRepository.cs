using ByteSync.Business;
using ByteSync.Business.Inventories;
using ByteSync.Interfaces.Repositories;

namespace ByteSync.Repositories;

public class InventoryFileRepository : BaseSourceCacheRepository<InventoryFile, string>, IInventoryFileRepository
{
    private readonly ISessionInvalidationCachePolicy<InventoryFile, string> _sessionInvalidationCachePolicy;

    public InventoryFileRepository(ISessionInvalidationCachePolicy<InventoryFile, string> sessionInvalidationCachePolicy)
    {
        _sessionInvalidationCachePolicy = sessionInvalidationCachePolicy;
        _sessionInvalidationCachePolicy.Initialize(SourceCache, true, true);
    }
    
    protected override string KeySelector(InventoryFile inventoryFile) => inventoryFile.FullName;
    
    public List<InventoryFile> GetAllInventoriesFiles(LocalInventoryModes localInventoryMode)
    {
        var result = Elements
            .Where(inventoryFile => inventoryFile.LocalInventoryMode == localInventoryMode)
            .ToList();

        return result;
    }
}