using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Business.Comparisons;

public class AtomicActionConsistencyCheckCanAddResult
{
    public AtomicActionConsistencyCheckCanAddResult(ICollection<ComparisonItem> comparisonItems)
    {
        ComparisonItems = comparisonItems;

        ValidComparisons = new HashSet<ComparisonItem>();
        
        NonValidComparisons = new HashSet<ComparisonItem>();
    }

    public ICollection<ComparisonItem> ComparisonItems { get; set; }
    
    public HashSet<ComparisonItem> ValidComparisons { get; set; }
    
    public HashSet<ComparisonItem> NonValidComparisons { get; set; }
    
    public bool IsOK
    {
        get
        {
            bool isOK = ValidComparisons.Count == ComparisonItems.Count 
                        && NonValidComparisons.Count == 0
                        && ComparisonItems.Count != 0;
            
            return isOK;
        }
    }
}