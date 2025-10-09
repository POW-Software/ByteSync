using System.Threading;
using ByteSync.Business.DataSources;
using ByteSync.Business.Inventories;
using ByteSync.Business.Sessions;
using ByteSync.Models.Inventories;

namespace ByteSync.Interfaces.Controls.Inventories;

public interface IInventoryBuilder
{
    public Inventory Inventory { get; }
    
    public string InventoryCode { get; }
    
    public IInventoryIndexer Indexer { get; }
    
    public SessionSettings? SessionSettings { get; }
    
    InventoryPart AddInventoryPart(DataSource dataSource);
    
    InventoryPart AddInventoryPart(string fullName);
    
    Task BuildBaseInventoryAsync(string inventoryFullName, CancellationToken cancellationToken = default);
    
    Task RunAnalysisAsync(string inventoryFullName, HashSet<IndexedItem> items, CancellationToken cancellationToken);
}