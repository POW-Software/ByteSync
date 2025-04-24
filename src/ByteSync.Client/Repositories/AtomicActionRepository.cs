using ByteSync.Business.Actions.Local;
using ByteSync.Interfaces.Repositories;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Repositories;

public class AtomicActionRepository : BaseSourceCacheRepository<AtomicAction, string>, IAtomicActionRepository
{
    private readonly ISessionInvalidationCachePolicy<AtomicAction, string> _sessionInvalidationCachePolicy;
    private readonly IPropertyIndexer<AtomicAction, ComparisonItem> _propertyIndexer;

    public AtomicActionRepository(ISessionInvalidationCachePolicy<AtomicAction, string> sessionInvalidationCachePolicy,
        IPropertyIndexer<AtomicAction, ComparisonItem> propertyIndexer)
    {
        _sessionInvalidationCachePolicy = sessionInvalidationCachePolicy;
        _sessionInvalidationCachePolicy.Initialize(SourceCache, true, true);

        _propertyIndexer = propertyIndexer;
        _propertyIndexer.Initialize(SourceCache, atomicAction => atomicAction.ComparisonItem!);
    }

    protected override string KeySelector(AtomicAction atomicAction) => atomicAction.AtomicActionId;

    public List<AtomicAction> GetAtomicActions(ComparisonItem comparisonItem)
    {
        return _propertyIndexer.GetByIndex(comparisonItem);
    }
}
