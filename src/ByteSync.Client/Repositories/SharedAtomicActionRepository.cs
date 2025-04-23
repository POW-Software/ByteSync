using ByteSync.Business.Actions.Shared;
using ByteSync.Interfaces.Repositories;
using DynamicData;

namespace ByteSync.Repositories;

public class SharedAtomicActionRepository : BaseSourceCacheRepository<SharedAtomicAction, string>, ISharedAtomicActionRepository
{
    private readonly ISessionInvalidationCachePolicy<SharedAtomicAction, string> _sessionInvalidationCachePolicy;

    public SharedAtomicActionRepository(ISessionInvalidationCachePolicy<SharedAtomicAction, string> sessionInvalidationCachePolicy)
    {
        _sessionInvalidationCachePolicy = sessionInvalidationCachePolicy;
        _sessionInvalidationCachePolicy.Initialize(SourceCache, true, true);
    }
    
    protected override string KeySelector(SharedAtomicAction sharedAtomicAction) => sharedAtomicAction.AtomicActionId;
    
    public void SetSharedAtomicActions(List<SharedAtomicAction> sharedAtomicActions)
    {
        SourceCache.Clear();
        SourceCache.AddOrUpdate(sharedAtomicActions);
    }
}