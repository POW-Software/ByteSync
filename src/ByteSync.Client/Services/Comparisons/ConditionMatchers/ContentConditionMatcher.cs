using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Common.Business.Inventories;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Services.Comparisons.ConditionMatchers;

public class ContentConditionMatcher : IConditionMatcher
{
    private readonly ContentIdentityExtractor _extractor;
    
    public ContentConditionMatcher(ContentIdentityExtractor extractor)
    {
        _extractor = extractor;
    }
    
    public ComparisonProperty SupportedProperty => ComparisonProperty.Content;
    
    public bool Matches(AtomicCondition condition, ComparisonItem comparisonItem)
    {
        bool? result = null;
        
        if (comparisonItem.FileSystemType == FileSystemTypes.Directory)
        {
            return false;
        }
        
        var contentIdentitySource = _extractor.ExtractContentIdentity(condition.Source, comparisonItem);
        var contentIdentityDestination = _extractor.ExtractContentIdentity(condition.Destination, comparisonItem);
        
        if ((contentIdentitySource != null && (contentIdentitySource.HasAnalysisError || contentIdentitySource.HasAccessIssue))
            || (contentIdentityDestination != null &&
                (contentIdentityDestination.HasAnalysisError || contentIdentityDestination.HasAccessIssue)))
        {
            return false;
        }
        
        switch (condition.ConditionOperator)
        {
            case ConditionOperatorTypes.Equals:
                if (contentIdentitySource == null && contentIdentityDestination != null)
                {
                    result = false;
                }
                else if (contentIdentitySource != null && contentIdentityDestination == null)
                {
                    result = false;
                }
                else
                {
                    result = Equals(contentIdentitySource?.Core!.SignatureHash, contentIdentityDestination?.Core!.SignatureHash);
                }
                
                break;
            case ConditionOperatorTypes.NotEquals:
                if (contentIdentitySource == null && contentIdentityDestination != null)
                {
                    result = true;
                }
                else if (contentIdentitySource != null && contentIdentityDestination == null)
                {
                    result = true;
                }
                else
                {
                    result = !Equals(contentIdentitySource?.Core!.SignatureHash, contentIdentityDestination?.Core!.SignatureHash);
                }
                
                break;
        }
        
        if (result == null)
        {
            throw new ArgumentOutOfRangeException("ConditionMatchesContent " + condition.ConditionOperator);
        }
        
        return result.Value;
    }
}