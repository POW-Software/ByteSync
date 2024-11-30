using ByteSync.Business.PathItems;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Repositories;
using DynamicData;

namespace ByteSync.Repositories;

public class PathItemRepository : BaseSourceCacheRepository<PathItem, string>, IPathItemRepository
{
    private readonly ISessionInvalidationSourceCachePolicy<PathItem, string> _sessionInvalidationSourceCachePolicy;

    public PathItemRepository(IEnvironmentService environmentService, ISessionInvalidationSourceCachePolicy<PathItem, string> sessionInvalidationSourceCachePolicy)
    {
        CurrentMemberPathItems = SourceCache
            .Connect()
            .Filter(pathItem => Equals(pathItem.ClientInstanceId, environmentService.ClientInstanceId!))
            .AsObservableCache();
        
        _sessionInvalidationSourceCachePolicy = sessionInvalidationSourceCachePolicy;
        _sessionInvalidationSourceCachePolicy.Initialize(SourceCache, true, false);
    }

    protected override string KeySelector(PathItem pathItem) => pathItem.Key;
    
    public IObservableCache<PathItem, string> CurrentMemberPathItems { get; }
}