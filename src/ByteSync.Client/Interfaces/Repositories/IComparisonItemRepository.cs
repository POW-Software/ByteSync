using ByteSync.Business.Inventories;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Interfaces.Repositories;

public interface IComparisonItemRepository : IBaseSourceCacheRepository<ComparisonItem, PathIdentity>
{
    
}