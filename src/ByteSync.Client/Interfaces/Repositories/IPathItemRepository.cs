using ByteSync.Business.PathItems;
using DynamicData;

namespace ByteSync.Interfaces.Repositories;

public interface IPathItemRepository : IBaseSourceCacheRepository<PathItem, string>
{
    public IObservableCache<PathItem, string> CurrentMemberPathItems { get; }
    
    public IList<PathItem> SortedCurrentMemberPathItems { get; }
}