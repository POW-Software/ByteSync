using System.Threading.Tasks;
using ByteSync.Business;
using ByteSync.Business.Communications;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;

namespace ByteSync.Interfaces.Controls.Inventories;

public interface IInventoryService
{
    public InventoryProcessData InventoryProcessData { get; }
    
    Task SetLocalInventory(ICollection<InventoryFile> inventoriesFiles, LocalInventoryModes localInventoryModes);
    
    Task OnFileIsFullyDownloaded(LocalSharedFile localSharedFile);
    

    
    
    Task AbortInventory();
}