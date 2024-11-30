using ByteSync.Business.Actions.Local;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Interfaces.Repositories;

public interface IAtomicActionRepository : IBaseSourceCacheRepository<AtomicAction, string>
{
    List<AtomicAction> GetAtomicActions(ComparisonItem comparisonItem);
}