using Autofac;
using ByteSync.Business;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Inventories;

namespace ByteSync.Factories;

public class InventoryComparerFactory : IInventoryComparerFactory
{
    private readonly IComponentContext _context;
    
    public InventoryComparerFactory(IComponentContext context)
    {
        _context = context;
    }
    
    public IInventoryComparer CreateInventoryComparer(LocalInventoryModes localInventoryMode, IInventoryIndexer? inventoryIndexer = null)
    {
        var sessionService = _context.Resolve<ISessionService>();
        var cloudSessionSettings = sessionService.CurrentSessionSettings!;
        
        var inventoryFileRepository = _context.Resolve<IInventoryFileRepository>();
        var inventoriesFiles = inventoryFileRepository.GetAllInventoriesFiles(localInventoryMode);
        
        // inventoryIndexer is required for full inventory analysis to identify files needing checksum.
        // It's null when comparing already-complete inventories (e.g., from ComparisonItemsService).
        var inventoryComparer = _context.Resolve<IInventoryComparer>(
            new TypedParameter(typeof(SessionSettings), cloudSessionSettings),
            new TypedParameter(typeof(InventoryIndexer), inventoryIndexer));
        
        inventoryComparer.AddInventories(inventoriesFiles);
        
        return inventoryComparer;
    }
}