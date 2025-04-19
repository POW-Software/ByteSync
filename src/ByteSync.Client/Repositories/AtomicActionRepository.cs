using ByteSync.Business.Actions.Local;
using ByteSync.Interfaces.Repositories;
using ByteSync.Models.Comparisons.Result;
using DynamicData;

namespace ByteSync.Repositories;

public class AtomicActionRepository : BaseSourceCacheRepository<AtomicAction, string>, IAtomicActionRepository
{
    private readonly ISessionInvalidationSourceCachePolicy<AtomicAction, string> _sessionInvalidationSourceCachePolicy;

    // public AtomicActionRepository(ISessionInvalidationSourceCachePolicy<AtomicAction, string> sessionInvalidationSourceCachePolicy)
    // {
    //     _sessionInvalidationSourceCachePolicy = sessionInvalidationSourceCachePolicy;
    //     _sessionInvalidationSourceCachePolicy.Initialize(SourceCache, true, true);
    // }
    
    protected override string KeySelector(AtomicAction atomicAction) => atomicAction.AtomicActionId;

    // public List<AtomicAction> GetAtomicActions(ComparisonItem comparisonItem)
    // {
    //     var result = SourceCache.Items
    //         .Where(atomicAction => Equals(atomicAction.ComparisonItem, comparisonItem))
    //         .ToList();
    //
    //     return result;
    // }
    
    private readonly Dictionary<ComparisonItem, List<AtomicAction>> _indexedCache = new();

    public AtomicActionRepository(ISessionInvalidationSourceCachePolicy<AtomicAction, string> sessionInvalidationSourceCachePolicy)
    {
        _sessionInvalidationSourceCachePolicy = sessionInvalidationSourceCachePolicy;
        _sessionInvalidationSourceCachePolicy.Initialize(SourceCache, true, true);

        // Synchronisation du cache indexé avec le SourceCache
        SourceCache.Connect()
            .Subscribe(changes =>
            {
                foreach (var change in changes)
                {
                    switch (change.Reason)
                    {
                        case ChangeReason.Add:
                        case ChangeReason.Update:
                            UpdateIndexedCache(change.Current);
                            break;
                        case ChangeReason.Remove:
                            RemoveFromIndexedCache(change.Current);
                            break;
                    }
                }
            });
    }

    private void UpdateIndexedCache(AtomicAction atomicAction)
    {
        if (!_indexedCache.TryGetValue(atomicAction.ComparisonItem, out var actions))
        {
            actions = new List<AtomicAction>();
            _indexedCache[atomicAction.ComparisonItem] = actions;
        }

        // Mise à jour ou ajout de l'action atomique
        var existingAction = actions.FirstOrDefault(a => a.AtomicActionId == atomicAction.AtomicActionId);
        if (existingAction != null)
        {
            actions.Remove(existingAction);
        }

        actions.Add(atomicAction);
    }

    private void RemoveFromIndexedCache(AtomicAction atomicAction)
    {
        if (_indexedCache.TryGetValue(atomicAction.ComparisonItem, out var actions))
        {
            actions.RemoveAll(a => a.AtomicActionId == atomicAction.AtomicActionId);
            if (actions.Count == 0)
            {
                _indexedCache.Remove(atomicAction.ComparisonItem);
            }
        }
    }

    public List<AtomicAction> GetAtomicActions(ComparisonItem comparisonItem)
    {
        return _indexedCache.TryGetValue(comparisonItem, out var actions) ? actions : new List<AtomicAction>();
    }
}