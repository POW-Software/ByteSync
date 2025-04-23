using ByteSync.Business.Actions.Local;
using ByteSync.Interfaces.Repositories;

namespace ByteSync.Repositories;

public class SynchronizationRuleRepository : BaseSourceCacheRepository<SynchronizationRule, string>, ISynchronizationRuleRepository
{
    private readonly ISessionInvalidationCachePolicy<SynchronizationRule, string> _sessionInvalidationCachePolicy;

    public SynchronizationRuleRepository(ISessionInvalidationCachePolicy<SynchronizationRule, string> sessionInvalidationCachePolicy)
    {
        _sessionInvalidationCachePolicy = sessionInvalidationCachePolicy;
        _sessionInvalidationCachePolicy.Initialize(SourceCache, true, true);
    }
    
    protected override string KeySelector(SynchronizationRule synchronizationRule) => synchronizationRule.SynchronizationRuleId;
}