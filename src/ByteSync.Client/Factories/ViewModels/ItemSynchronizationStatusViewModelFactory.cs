using Autofac;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.Inventories;
using ByteSync.ViewModels.Sessions.Comparisons.Results;

namespace ByteSync.Factories.ViewModels;

public class ItemSynchronizationStatusViewModelFactory : IItemSynchronizationStatusViewModelFactory
{
    private readonly IComponentContext _context;

    public ItemSynchronizationStatusViewModelFactory(IComponentContext context)
    {
        _context = context;
    }
    
    public ItemSynchronizationStatusViewModel CreateItemSynchronizationStatusViewModel(ComparisonItem comparisonItem, List<Inventory> inventories)
    {
        var result = _context.Resolve<ItemSynchronizationStatusViewModel>(
            new TypedParameter(typeof(ComparisonItem), comparisonItem),
            new TypedParameter(typeof(List<Inventory>), inventories));

        return result;
    }
}