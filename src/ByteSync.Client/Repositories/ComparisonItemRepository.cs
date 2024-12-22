using ByteSync.Business.Inventories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Repositories;

public class ComparisonItemRepository : BaseSourceCacheRepository<ComparisonItem, PathIdentity>, IComparisonItemRepository
{
    protected override PathIdentity KeySelector(ComparisonItem element) => element.PathIdentity;
}