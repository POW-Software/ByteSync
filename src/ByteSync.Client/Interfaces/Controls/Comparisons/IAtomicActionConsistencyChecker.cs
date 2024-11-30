using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Interfaces.Controls.Comparisons;

public interface IAtomicActionConsistencyChecker
{
    AtomicActionConsistencyCheckCanAddResult CheckCanAdd(AtomicAction atomicAction, ComparisonItem comparisonItem);
    
    AtomicActionConsistencyCheckCanAddResult CheckCanAdd(AtomicAction atomicAction, ICollection<ComparisonItem> comparisonItems);
    
    List<AtomicAction> GetAppliableActions(ICollection<SynchronizationRule> synchronizationRules);
}