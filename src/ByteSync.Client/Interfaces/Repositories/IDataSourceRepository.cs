using ByteSync.Business.DataSources;
using DynamicData;

namespace ByteSync.Interfaces.Repositories;

public interface IDataSourceRepository : IBaseSourceCacheRepository<DataSource, string>
{
    public IObservableCache<DataSource, string> CurrentMemberPathItems { get; }
    
    public IList<DataSource> SortedCurrentMemberPathItems { get; }
}