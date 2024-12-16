using Autofac;
using ByteSync.Business.Actions.Local;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.ViewModels.Sessions.Comparisons.Results;

namespace ByteSync.Factories.ViewModels;

public class SynchronizationActionViewModelFactory : ISynchronizationActionViewModelFactory
{
    private readonly IComponentContext _context;

    public SynchronizationActionViewModelFactory(IComponentContext context)
    {
        _context = context;
    }
    
    public SynchronizationActionViewModel CreateSynchronizationActionViewModel(AtomicAction atomicAction)
    {
        var result = _context.Resolve<SynchronizationActionViewModel>(
            new TypedParameter(typeof(AtomicAction), atomicAction));
        
        return result;
    }
}