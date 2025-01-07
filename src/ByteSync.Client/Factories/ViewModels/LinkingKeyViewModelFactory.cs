using Autofac;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.ViewModels.Sessions.Cloud.Managing;

namespace ByteSync.Factories.ViewModels;

public class LinkingKeyViewModelFactory : ILinkingKeyViewModelFactory
{
    private readonly IComponentContext _context;

    public LinkingKeyViewModelFactory(IComponentContext context)
    {
        _context = context;
    }
    
    public LinkingKeyViewModel CreateLinkingKeyViewModel(LinkingKeys linkingKey)
    {
        var result = _context.Resolve<LinkingKeyViewModel>(
            new TypedParameter(typeof(LinkingKeys), linkingKey));

        return result;
    }
}