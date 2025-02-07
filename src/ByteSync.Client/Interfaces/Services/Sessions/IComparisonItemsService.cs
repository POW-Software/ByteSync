using System.Reactive.Subjects;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Interfaces.Services.Sessions;

public interface IComparisonItemsService
{
    public ISubject<ComparisonResult?> ComparisonResult { get; set; }
    
    Task ApplySynchronizationRules();
}