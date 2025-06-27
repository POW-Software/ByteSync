using ByteSync.Business.DataNodes;
using DynamicData;

namespace ByteSync.Interfaces.Repositories;

public interface IDataNodeRepository : IBaseSourceCacheRepository<DataNode, string>
{
    IObservableCache<DataNode, string> CurrentMemberDataNodes { get; }

    IList<DataNode> SortedCurrentMemberDataNodes { get; }
}
