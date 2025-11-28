using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Common.Business.Inventories;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Services.Comparisons.ConditionMatchers;

public class SizeConditionMatcher : IConditionMatcher
{
    private readonly ContentIdentityExtractor _extractor;
    
    public SizeConditionMatcher(ContentIdentityExtractor extractor)
    {
        _extractor = extractor;
    }
    
    public ComparisonProperty SupportedProperty => ComparisonProperty.Size;
    
    public bool Matches(AtomicCondition condition, ComparisonItem comparisonItem)
    {
        if (comparisonItem.FileSystemType == FileSystemTypes.Directory)
        {
            return false;
        }
        
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
        
        var sizeSource = _extractor.ExtractSize(condition.Source, comparisonItem);
        
        long? sizeDestination;
        if (condition.Destination is { IsVirtual: false })
        {
            sizeDestination = _extractor.ExtractSize(condition.Destination, comparisonItem);
        }
        else
        {
            var size = (long)condition.Size!;
            var sizeUnitPower = (int)condition.SizeUnit! - 1;
            
            sizeDestination = size * (long)Math.Pow(1024, sizeUnitPower);
        }
        
        if (sizeSource == null || sizeDestination == null)
        {
            return false;
        }
        
        var result = false;
        switch (condition.ConditionOperator)
        {
            case ConditionOperatorTypes.Equals:
                result = sizeSource == sizeDestination;
                
                break;
            case ConditionOperatorTypes.NotEquals:
                result = sizeSource != sizeDestination;
                
                break;
            case ConditionOperatorTypes.IsSmallerThan:
                result = sizeSource < sizeDestination;
                
                break;
            case ConditionOperatorTypes.IsBiggerThan:
                result = sizeSource > sizeDestination;
                
                break;
        }
        
        return result;
    }
}
