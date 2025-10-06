using Autofac;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.ViewModels.Sessions.Managing;

namespace ByteSync.Factories.ViewModels;

public class MatchingModeViewModelFactory : IMatchingModeViewModelFactory
{
    private readonly IComponentContext _context;
    
    public MatchingModeViewModelFactory(IComponentContext context)
    {
        _context = context;
    }
    
    public MatchingModeViewModel CreateMatchingModeViewModel(MatchingModes matchingMode)
    {
        var result = _context.Resolve<MatchingModeViewModel>(
            new TypedParameter(typeof(MatchingModes), matchingMode));
        
        return result;
    }
}