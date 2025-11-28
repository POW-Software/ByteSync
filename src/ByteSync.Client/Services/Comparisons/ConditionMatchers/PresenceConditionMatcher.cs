using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Common.Business.Inventories;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Services.Comparisons.ConditionMatchers;

public class PresenceConditionMatcher : IConditionMatcher
{
    private readonly ContentIdentityExtractor _extractor;
    
    public PresenceConditionMatcher(ContentIdentityExtractor extractor)
    {
        _extractor = extractor;
    }
    
    public ComparisonProperty SupportedProperty => ComparisonProperty.Presence;
    
    public bool Matches(AtomicCondition condition, ComparisonItem comparisonItem)
    {
        bool? result = null;
        
        var sourcePart = condition.Source?.GetApplicableInventoryPart();
        if (sourcePart != null && sourcePart.IsIncompleteDueToAccess)
        {
            return false;
        }
        
        var destinationPart = condition.Destination?.GetApplicableInventoryPart();
        if (destinationPart != null && destinationPart.IsIncompleteDueToAccess)
        {
            return false;
        }
        
        if (condition.ConditionOperator.In(ConditionOperatorTypes.ExistsOn, ConditionOperatorTypes.NotExistsOn))
        {
            var existsOnSource = _extractor.ExistsOn(condition.Source, comparisonItem);
            var existsOnDestination = _extractor.ExistsOn(condition.Destination, comparisonItem);
            
            switch (condition.ConditionOperator)
            {
                case ConditionOperatorTypes.ExistsOn:
                    result = existsOnSource && existsOnDestination;
                    
                    break;
                case ConditionOperatorTypes.NotExistsOn:
                    result = existsOnSource && !existsOnDestination;
                    
                    break;
            }
        }
        
        if (result == null)
        {
            throw new ArgumentOutOfRangeException("ConditionMatchesPresence " + condition.ConditionOperator);
        }
        
        return result.Value;
    }
}
