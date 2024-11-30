using ByteSync.Business.Actions.Local;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Interfaces.Controls.Synchronizations;

public interface ISynchronizationRuleMatcher
{
    void MakeMatches(ComparisonItem comparisonItem, ICollection<SynchronizationRule> synchronizationRules);

    void MakeMatches(ICollection<ComparisonItem> comparisonItems, ICollection<SynchronizationRule> synchronizationRules);
}