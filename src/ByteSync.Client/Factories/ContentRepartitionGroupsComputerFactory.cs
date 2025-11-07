using Autofac;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Factories;
using ByteSync.Models.Inventories;
using ByteSync.ViewModels.Sessions.Comparisons.Results;

namespace ByteSync.Factories;

public class ContentRepartitionGroupsComputerFactory : IContentRepartitionGroupsComputerFactory
{
    private readonly IComponentContext _context;
    
    public ContentRepartitionGroupsComputerFactory(IComponentContext context)
    {
        _context = context;
    }
    
    public IContentRepartitionGroupsComputer Build(ContentRepartitionViewModel contentRepartitionViewModel)
    {
        var allInventories = contentRepartitionViewModel.AllInventories;
        var result = _context.Resolve<IContentRepartitionGroupsComputer>(
            new TypedParameter(typeof(ContentRepartitionViewModel), contentRepartitionViewModel),
            new TypedParameter(typeof(List<Inventory>), allInventories));
        
        return result;
    }
}