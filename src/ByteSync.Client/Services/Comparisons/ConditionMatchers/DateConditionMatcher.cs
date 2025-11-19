using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Services.Comparisons.ConditionMatchers;

public class DateConditionMatcher : IConditionMatcher
{
    private readonly ContentIdentityExtractor _extractor;
    
    public DateConditionMatcher(ContentIdentityExtractor extractor)
    {
        _extractor = extractor;
    }
    
    public ComparisonProperty SupportedProperty => ComparisonProperty.Date;
    
    public bool Matches(AtomicCondition condition, ComparisonItem comparisonItem)
    {
        var lastWriteTimeSource = _extractor.ExtractDate(condition.Source, comparisonItem);
        
        DateTime? lastWriteTimeDestination;
        if (condition.Destination is { IsVirtual: false })
        {
            lastWriteTimeDestination = _extractor.ExtractDate(condition.Destination, comparisonItem);
        }
        else
        {
            lastWriteTimeDestination = condition.DateTime!.Value.ToUniversalTime();
            
            if (lastWriteTimeSource is { Second: 0, Millisecond: 0 })
            {
                lastWriteTimeSource = lastWriteTimeSource.Value.Trim(TimeSpan.TicksPerMinute);
            }
        }
        
        if (lastWriteTimeSource == null)
        {
            return false;
        }
        
        var result = false;
        switch (condition.ConditionOperator)
        {
            case ConditionOperatorTypes.Equals:
                result = lastWriteTimeDestination != null && lastWriteTimeSource == lastWriteTimeDestination;
                
                break;
            case ConditionOperatorTypes.NotEquals:
                result = lastWriteTimeDestination != null && lastWriteTimeSource != lastWriteTimeDestination;
                
                break;
            case ConditionOperatorTypes.IsNewerThan:
                result = (condition.Destination is { IsVirtual: false } && lastWriteTimeDestination == null) ||
                         (lastWriteTimeDestination != null && lastWriteTimeSource > lastWriteTimeDestination);
                
                break;
            case ConditionOperatorTypes.IsOlderThan:
                result = lastWriteTimeDestination != null && lastWriteTimeSource < lastWriteTimeDestination;
                
                break;
        }
        
        return result;
    }
}