using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Services.Comparisons.ConditionMatchers;

public interface IConditionMatcher
{
    ComparisonProperty SupportedProperty { get; }
    
    bool Matches(AtomicCondition condition, ComparisonItem comparisonItem);
}