using Autofac;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.Inventories;
using ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;

namespace ByteSync.Factories.ViewModels;

public class ComparisonItemViewModelFactory : IComparisonItemViewModelFactory
{
    private readonly IComponentContext _context;

    public ComparisonItemViewModelFactory(IComponentContext context)
    {
        _context = context;
    }
    
    public ComparisonItemViewModel Create(ComparisonItem comparisonItem)
    {
        var result = _context.Resolve<ComparisonItemViewModel>(
            new TypedParameter(typeof(ComparisonItem), comparisonItem),
            new TypedParameter(typeof(List<Inventory>), comparisonItem.ComparisonResult.Inventories));

        return result;
    }
}