using Autofac;
using ByteSync.Business.Actions.Local;
using ByteSync.Interfaces.Factories;
using ByteSync.ViewModels.Sessions.Comparisons.Results;

namespace ByteSync.Services.Comparisons;

public class SynchronizationRuleSummaryViewModelFactory : ISynchronizationRuleSummaryViewModelFactory
{
    private readonly IComponentContext _context;

    public SynchronizationRuleSummaryViewModelFactory(IComponentContext context)
    {
        _context = context;
    }
    
    public SynchronizationRuleSummaryViewModel Create(SynchronizationRule synchronizationRule)
    {
        var result = _context.Resolve<SynchronizationRuleSummaryViewModel>(
            new TypedParameter(typeof(SynchronizationRule), synchronizationRule));

        return result;
    }
}