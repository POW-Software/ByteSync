using System.Reactive.Subjects;
using System.Threading.Tasks;
using ByteSync.Business.Inventories;
using ByteSync.Models.Comparisons.Result;
using DynamicData;

namespace ByteSync.Interfaces.Controls.Sessions;

public interface IComparisonItemsService
{
    public IObservableCache<ComparisonItem, PathIdentity> ComparisonItems { get; }
    
    public ISubject<ComparisonResult?> ComparisonResult { get; set; }

    // Task InitializeComparisonItems();
    
    Task ApplySynchronizationRules();
}