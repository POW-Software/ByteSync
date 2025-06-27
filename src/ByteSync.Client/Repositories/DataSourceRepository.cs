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
        CurrentMemberDataSources = SourceCache
            .Connect()
            .Filter(dataSource => Equals(dataSource.ClientInstanceId, environmentService.ClientInstanceId!))
            .AsObservableCache();
        
        _sessionInvalidationCachePolicy = sessionInvalidationCachePolicy;
        _sessionInvalidationCachePolicy.Initialize(SourceCache, true, false);
    }

    protected override string KeySelector(DataSource dataSource) => dataSource.Key;
    
    public IObservableCache<DataSource, string> CurrentMemberDataSources { get; }
    
    public IList<DataSource> SortedCurrentMemberDataSources
    {
        get
        {
            return CurrentMemberDataSources.Items.OrderBy(ds => ds.Code).ToList();
        }
    }
}