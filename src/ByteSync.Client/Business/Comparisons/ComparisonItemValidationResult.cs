using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Comparisons;

public class ComparisonItemValidationResult
{
    public ComparisonItemValidationResult(ComparisonItem comparisonItem, bool isValid)
    {
        ComparisonItem = comparisonItem;
        IsValid = isValid;
        FailureReason = null;
    }
    
    public ComparisonItemValidationResult(ComparisonItem comparisonItem, AtomicActionValidationFailureReason failureReason)
    {
        ComparisonItem = comparisonItem;
        IsValid = false;
        FailureReason = failureReason;
    }
    
    public ComparisonItem ComparisonItem { get; }
    public bool IsValid { get; }
    public AtomicActionValidationFailureReason? FailureReason { get; }
}
