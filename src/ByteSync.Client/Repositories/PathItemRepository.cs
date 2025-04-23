using ByteSync.Business.PathItems;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Repositories;
using DynamicData;

namespace ByteSync.Repositories;

public class PathItemRepository : BaseSourceCacheRepository<PathItem, string>, IPathItemRepository
{
    private readonly ISessionInvalidationCachePolicy<PathItem, string> _sessionInvalidationCachePolicy;

    public PathItemRepository(IEnvironmentService environmentService, ISessionInvalidationCachePolicy<PathItem, string> sessionInvalidationCachePolicy)
    {
        CurrentMemberPathItems = SourceCache
            .Connect()
            .Filter(pathItem => Equals(pathItem.ClientInstanceId, environmentService.ClientInstanceId!))
            .AsObservableCache();
        
        _sessionInvalidationCachePolicy = sessionInvalidationCachePolicy;
        _sessionInvalidationCachePolicy.Initialize(SourceCache, true, false);
    }

    protected override string KeySelector(PathItem pathItem) => pathItem.Key;
    
    public IObservableCache<PathItem, string> CurrentMemberPathItems { get; }
    
    public IList<PathItem> SortedCurrentMemberPathItems
    {
        get
        {
            return CurrentMemberPathItems.Items.OrderBy(pi => pi.Code).ToList();
        }
    }
}