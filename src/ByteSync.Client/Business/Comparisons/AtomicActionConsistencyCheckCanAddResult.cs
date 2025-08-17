using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Comparisons;

public class AtomicActionConsistencyCheckCanAddResult
{
    public AtomicActionConsistencyCheckCanAddResult(ICollection<ComparisonItem> comparisonItems)
    {
        ComparisonItems = comparisonItems;
        ValidationResults = new List<ComparisonItemValidationResult>();
    }

    public ICollection<ComparisonItem> ComparisonItems { get; set; }
    
    public List<ComparisonItemValidationResult> ValidationResults { get; set; }
    
    public HashSet<ComparisonItem> ValidComparisons 
    { 
        get => ValidationResults.Where(r => r.IsValid).Select(r => r.ComparisonItem).ToHashSet();
    }
    
    public HashSet<ComparisonItem> NonValidComparisons 
    { 
        get => ValidationResults.Where(r => !r.IsValid).Select(r => r.ComparisonItem).ToHashSet();
    }
    
    public List<ComparisonItemValidationResult> ValidValidations
    {
        get => ValidationResults.Where(r => r.IsValid).ToList();
    }
    
    public List<ComparisonItemValidationResult> FailedValidations
    {
        get => ValidationResults.Where(r => !r.IsValid).ToList();
    }
    
    public bool IsOK
    {
        get
        {
            bool isOK = ValidationResults.All(r => r.IsValid)
                        && ComparisonItems.Count != 0;
            
            return isOK;
        }
    }
}