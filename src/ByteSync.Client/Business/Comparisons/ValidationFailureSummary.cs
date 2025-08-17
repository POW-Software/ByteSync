using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Comparisons;

public class ValidationFailureSummary
{
    public AtomicActionValidationFailureReason Reason { get; set; }
    public int Count { get; set; }
    public string LocalizedMessage { get; set; } = string.Empty;
    public List<ComparisonItem> AffectedItems { get; set; } = new();
    
    public string AffectedItemsTooltip => string.Join("\n", AffectedItems.Select(item => 
        item.PathIdentity.FileName ?? item.PathIdentity.LinkingKeyValue));
}
