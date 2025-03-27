using Autofac;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.Inventories;
using ByteSync.ViewModels.Sessions.Comparisons.Results;

namespace ByteSync.Factories.ViewModels;

public class ContentRepartitionViewModelFactory : IContentRepartitionViewModelFactory
{
    private readonly IComponentContext _context;

    public ContentRepartitionViewModelFactory(IComponentContext context)
    {
        _context = context;
    }
    
    public ContentRepartitionViewModel CreateContentRepartitionViewModel(ComparisonItem comparisonItem, List<Inventory> inventories)
    {
        var result = _context.Resolve<ContentRepartitionViewModel>(
            new TypedParameter(typeof(ComparisonItem), comparisonItem),
            new TypedParameter(typeof(List<Inventory>), inventories));

        return result;
    }
}