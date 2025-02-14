using System.Threading;
using ByteSync.Business.PathItems;
using ByteSync.Business.Sessions;
using ByteSync.Models.Inventories;
using ByteSync.Services.Inventories;

namespace ByteSync.Interfaces.Controls.Inventories;

public interface IInventoryBuilder
{
    public Inventory Inventory { get; }
    
    public string InventoryLetter { get; }
    
    public InventoryIndexer Indexer { get; }
    
    public SessionSettings? SessionSettings { get; }
    
    InventoryPart AddInventoryPart(PathItem pathItem);

    InventoryPart AddInventoryPart(string fullName);
    
    Task BuildBaseInventoryAsync(string inventoryFullName, CancellationToken cancellationToken = default);

    Task RunAnalysisAsync(string inventoryFullName, HashSet<ByteSync.Business.Inventories.IndexedItem> items, CancellationToken cancellationToken);
}