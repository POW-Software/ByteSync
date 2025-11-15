using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Services.Comparisons.ConditionMatchers;

public class ConditionMatcherFactory
{
    private readonly Dictionary<ComparisonProperty, IConditionMatcher> _matchers;
    
    public ConditionMatcherFactory(IEnumerable<IConditionMatcher> matchers)
    {
        _matchers = matchers
            .GroupBy(m => m.SupportedProperty)
            .ToDictionary(g => g.Key, g => g.First());
    }
    
    public IConditionMatcher GetMatcher(ComparisonProperty property)
    {
        if (_matchers.TryGetValue(property, out var matcher))
        {
            return matcher;
        }
        
        return new NullConditionMatcher();
    }
    
    private class NullConditionMatcher : IConditionMatcher
    {
        public ComparisonProperty SupportedProperty => (ComparisonProperty)0;
        
        public bool Matches(AtomicCondition condition, ComparisonItem comparisonItem) => false;
    }
}