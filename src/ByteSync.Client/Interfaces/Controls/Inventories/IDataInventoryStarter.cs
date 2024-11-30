using System.Threading.Tasks;
using ByteSync.Common.Business.Inventories;

namespace ByteSync.Interfaces.Controls.Inventories;

public interface IDataInventoryStarter
{
    Task<StartInventoryResult> StartDataInventory(bool isLaunchedByUser);

    IObservable<bool> CanCurrentUserStartInventory();
}