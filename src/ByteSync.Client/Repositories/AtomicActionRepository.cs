using ByteSync.Business.Actions.Local;
using ByteSync.Interfaces.Repositories;
using ByteSync.Models.Comparisons.Result;
using DynamicData;

namespace ByteSync.Repositories;

public class AtomicActionRepository : BaseSourceCacheRepository<AtomicAction, string>, IAtomicActionRepository
{
    private readonly ISessionInvalidationSourceCachePolicy<AtomicAction, string> _sessionInvalidationSourceCachePolicy;
    private readonly IIndexedCache<AtomicAction, ComparisonItem> _indexedCache;

    public AtomicActionRepository(ISessionInvalidationSourceCachePolicy<AtomicAction, string> sessionInvalidationSourceCachePolicy,
        IIndexedCache<AtomicAction, ComparisonItem> indexedCache)
    {
        _sessionInvalidationSourceCachePolicy = sessionInvalidationSourceCachePolicy;
        _sessionInvalidationSourceCachePolicy.Initialize(SourceCache, true, true);

        _indexedCache = indexedCache;
        _indexedCache.Initialize(SourceCache, atomicAction => atomicAction.ComparisonItem);
        
        // _indexedCache = new IndexedCache<AtomicAction, ComparisonItem>(atomicAction => atomicAction.ComparisonItem);

        // Synchronisation du cache indexé avec le SourceCache
        // SourceCache.Connect()
        //     .Subscribe(changes =>
        //     {
        //         foreach (var change in changes)
        //         {
        //             switch (change.Reason)
        //             {
        //                 case ChangeReason.Add:
        //                 case ChangeReason.Update:
        //                     _indexedCache.Update(change.Current);
        //                     break;
        //                 case ChangeReason.Remove:
        //                     _indexedCache.Remove(change.Current);
        //                     break;
        //             }
        //         }
        //     });
    }

    protected override string KeySelector(AtomicAction atomicAction) => atomicAction.AtomicActionId;

    public List<AtomicAction> GetAtomicActions(ComparisonItem comparisonItem)
    {
        return _indexedCache.GetByIndex(comparisonItem);
    }
}
