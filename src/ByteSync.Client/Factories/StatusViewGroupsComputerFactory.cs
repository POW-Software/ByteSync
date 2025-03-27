using Autofac;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Factories;
using ByteSync.Models.Inventories;
using ByteSync.ViewModels.Sessions.Comparisons.Results;

namespace ByteSync.Factories;

public class StatusViewGroupsComputerFactory : IStatusViewGroupsComputerFactory
{
    private readonly IComponentContext _context;

    public StatusViewGroupsComputerFactory(IComponentContext context)
    {
        _context = context;
    }
    
    public IStatusViewGroupsComputer BuildStatusViewGroupsComputer(StatusViewModel statusViewModel)
    {
        var inventoryService = _context.Resolve<IInventoryService>();
        var allInventories = inventoryService.InventoryProcessData.Inventories!;
        
        var result = _context.Resolve<IStatusViewGroupsComputer>(
            new TypedParameter(typeof(StatusViewModel), statusViewModel),
            new TypedParameter(typeof(List<Inventory>), allInventories));
        
        return result;
    }
}