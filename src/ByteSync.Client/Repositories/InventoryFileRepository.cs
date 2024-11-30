using ByteSync.Business;
using ByteSync.Business.Inventories;
using ByteSync.Interfaces.Repositories;

namespace ByteSync.Repositories;

public class InventoryFileRepository : BaseSourceCacheRepository<InventoryFile, string>, IInventoryFileRepository
{
    private readonly ISessionInvalidationSourceCachePolicy<InventoryFile, string> _sessionInvalidationSourceCachePolicy;

    public InventoryFileRepository(ISessionInvalidationSourceCachePolicy<InventoryFile, string> sessionInvalidationSourceCachePolicy)
    {
        _sessionInvalidationSourceCachePolicy = sessionInvalidationSourceCachePolicy;
        _sessionInvalidationSourceCachePolicy.Initialize(SourceCache, true, true);
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