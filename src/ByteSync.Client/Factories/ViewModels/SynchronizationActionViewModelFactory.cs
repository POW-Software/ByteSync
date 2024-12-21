using Autofac;
using ByteSync.Business.Actions.Local;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.ViewModels.Sessions.Comparisons.Results;
using ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;

namespace ByteSync.Factories.ViewModels;

public class SynchronizationActionViewModelFactory : ISynchronizationActionViewModelFactory
{
    private readonly IComponentContext _context;

    public SynchronizationActionViewModelFactory(IComponentContext context)
    {
        _context = context;
    }
    
    public SynchronizationActionViewModel CreateSynchronizationActionViewModel(AtomicAction atomicAction, ComparisonItemViewModel comparisonItemViewModel)
    {
        var result = _context.Resolve<SynchronizationActionViewModel>(
            new TypedParameter(typeof(AtomicAction), atomicAction),
            new TypedParameter(typeof(ComparisonItemViewModel), comparisonItemViewModel));
        
        return result;
    }
}