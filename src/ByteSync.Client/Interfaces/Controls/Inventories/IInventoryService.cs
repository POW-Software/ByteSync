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

    // public HashSet<LocalSharedFile> OtherMembersInventories { get; }
    //
    // public List<LocalSharedFile>? LocalBaseInventories { get; set; }
    //
    // public List<LocalSharedFile>? LocalFullInventories { get; set; }
    
    // public SessionMemberGeneralStatus SessionMemberGeneralStatus { get; set; }
    
    Task SetLocalInventory(ICollection<InventoryFile> inventoriesFiles, LocalInventoryModes localInventoryModes);
    
    Task OnFileIsFullyDownloaded(LocalSharedFile localSharedFile);
    

    

    
    // bool HandleLocalInventoryGlobalStatusChanged(UpdateSessionMemberGeneralStatusParameters parameters);
    
    Task AbortInventory();
}