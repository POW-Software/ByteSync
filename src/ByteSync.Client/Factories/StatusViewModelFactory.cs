using Autofac;
using ByteSync.Interfaces.Factories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.Inventories;
using ByteSync.ViewModels.Sessions.Comparisons.Results;

namespace ByteSync.Factories;

public class StatusViewModelFactory : IStatusViewModelFactory
{
    private readonly IComponentContext _context;

    public StatusViewModelFactory(IComponentContext context)
    {
        _context = context;
    }
    
    public StatusViewModel CreateStatusViewModel(ComparisonItem comparisonItem, List<Inventory> inventories)
    {
        var result = _context.Resolve<StatusViewModel>(
            new TypedParameter(typeof(ComparisonItem), comparisonItem),
            new TypedParameter(typeof(List<Inventory>), inventories));

        return result;
    }
}