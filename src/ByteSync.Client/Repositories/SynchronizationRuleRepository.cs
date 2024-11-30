using ByteSync.Business.Actions.Local;
using ByteSync.Interfaces.Repositories;

namespace ByteSync.Repositories;

public class SynchronizationRuleRepository : BaseSourceCacheRepository<SynchronizationRule, string>, ISynchronizationRuleRepository
{
    private readonly ISessionInvalidationSourceCachePolicy<SynchronizationRule, string> _sessionInvalidationSourceCachePolicy;

    public SynchronizationRuleRepository(ISessionInvalidationSourceCachePolicy<SynchronizationRule, string> sessionInvalidationSourceCachePolicy)
    {
        _sessionInvalidationSourceCachePolicy = sessionInvalidationSourceCachePolicy;
        _sessionInvalidationSourceCachePolicy.Initialize(SourceCache, true, true);
    }
    
    protected override string KeySelector(SynchronizationRule synchronizationRule) => synchronizationRule.SynchronizationRuleId;
}