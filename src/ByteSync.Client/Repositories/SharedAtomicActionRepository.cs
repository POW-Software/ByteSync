using ByteSync.Business.Actions.Shared;
using ByteSync.Interfaces.Repositories;
using DynamicData;

namespace ByteSync.Repositories;

public class SharedAtomicActionRepository : BaseSourceCacheRepository<SharedAtomicAction, string>, ISharedAtomicActionRepository
{
    private readonly ISessionInvalidationSourceCachePolicy<SharedAtomicAction, string> _sessionInvalidationSourceCachePolicy;

    public SharedAtomicActionRepository(ISessionInvalidationSourceCachePolicy<SharedAtomicAction, string> sessionInvalidationSourceCachePolicy)
    {
        _sessionInvalidationSourceCachePolicy = sessionInvalidationSourceCachePolicy;
        _sessionInvalidationSourceCachePolicy.Initialize(SourceCache, true, true);
    }
    
    protected override string KeySelector(SharedAtomicAction sharedAtomicAction) => sharedAtomicAction.AtomicActionId;
    
    public void SetSharedAtomicActions(List<SharedAtomicAction> sharedAtomicActions)
    {
        SourceCache.Clear();
        SourceCache.AddOrUpdate(sharedAtomicActions);
    }
}