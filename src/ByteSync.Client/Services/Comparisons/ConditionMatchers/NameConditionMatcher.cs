using System.Text.RegularExpressions;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Services.Comparisons.ConditionMatchers;

public class NameConditionMatcher : IConditionMatcher
{
    public ComparisonProperty SupportedProperty => ComparisonProperty.Name;
    
    public bool Matches(AtomicCondition condition, ComparisonItem comparisonItem)
    {
        if (string.IsNullOrWhiteSpace(condition.NamePattern))
        {
            return false;
        }
        
        var name = comparisonItem.PathIdentity.FileName;
        var pattern = condition.NamePattern!;
        
        var result = false;
        
        if (pattern.Contains("*") &&
            condition.ConditionOperator.In(ConditionOperatorTypes.Equals, ConditionOperatorTypes.NotEquals))
        {
            var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            var safeRegex = new Regex(regex, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));
            var isMatch = safeRegex.IsMatch(name);
            result = condition.ConditionOperator == ConditionOperatorTypes.Equals ? isMatch : !isMatch;
        }
        else
        {
            switch (condition.ConditionOperator)
            {
                case ConditionOperatorTypes.Equals:
                    result = string.Equals(name, pattern, StringComparison.OrdinalIgnoreCase);
                    
                    break;
                case ConditionOperatorTypes.NotEquals:
                    result = !string.Equals(name, pattern, StringComparison.OrdinalIgnoreCase);
                    
                    break;
            }
        }
        
        return result;
    }
}