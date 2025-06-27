using ByteSync.Business.DataSources;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Repositories;
using DynamicData;

namespace ByteSync.Repositories;

public class DataSourceRepository : BaseSourceCacheRepository<DataSource, string>, IDataSourceRepository
{
    private readonly ISessionInvalidationCachePolicy<DataSource, string> _sessionInvalidationCachePolicy;

    public DataSourceRepository(IEnvironmentService environmentService, ISessionInvalidationCachePolicy<DataSource, string> sessionInvalidationCachePolicy)
    {
        CurrentMemberPathItems = SourceCache
            .Connect()
            .Filter(pathItem => Equals(pathItem.ClientInstanceId, environmentService.ClientInstanceId!))
            .AsObservableCache();
        
        _sessionInvalidationCachePolicy = sessionInvalidationCachePolicy;
        _sessionInvalidationCachePolicy.Initialize(SourceCache, true, false);
    }

    protected override string KeySelector(DataSource dataSource) => dataSource.Key;
    
    public IObservableCache<DataSource, string> CurrentMemberPathItems { get; }
    
    public IList<DataSource> SortedCurrentMemberPathItems
    {
        get
        {
            return CurrentMemberPathItems.Items.OrderBy(pi => pi.Code).ToList();
        }
    }
}