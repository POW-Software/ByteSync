using ByteSync.Business.DataSources;
using DynamicData;

namespace ByteSync.Interfaces.Repositories;

public interface IDataSourceRepository : IBaseSourceCacheRepository<DataSource, string>
{
    public IObservableCache<DataSource, string> CurrentMemberDataSources { get; }
    
    public IList<DataSource> SortedCurrentMemberDataSources { get; }
}