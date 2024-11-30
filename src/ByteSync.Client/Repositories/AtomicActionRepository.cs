using ByteSync.Business.Actions.Local;
using ByteSync.Interfaces.Repositories;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Repositories;

public class AtomicActionRepository : BaseSourceCacheRepository<AtomicAction, string>, IAtomicActionRepository
{
    private readonly ISessionInvalidationSourceCachePolicy<AtomicAction, string> _sessionInvalidationSourceCachePolicy;

    public AtomicActionRepository(ISessionInvalidationSourceCachePolicy<AtomicAction, string> sessionInvalidationSourceCachePolicy)
    {
        _sessionInvalidationSourceCachePolicy = sessionInvalidationSourceCachePolicy;
        _sessionInvalidationSourceCachePolicy.Initialize(SourceCache, true, true);
    }
    
    protected override string KeySelector(AtomicAction atomicAction) => atomicAction.AtomicActionId;

    public List<AtomicAction> GetAtomicActions(ComparisonItem comparisonItem)
    {
        var result = SourceCache.Items
            .Where(atomicAction => Equals(atomicAction.ComparisonItem, comparisonItem))
            .ToList();

        return result;
    }
}