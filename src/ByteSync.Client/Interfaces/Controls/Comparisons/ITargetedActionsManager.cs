using ByteSync.Business.Actions.Local;
using ByteSync.Models.Comparisons.Result;
using ByteSync.ViewModels.Sessions.Comparisons.Results;
using ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;

namespace ByteSync.Interfaces.Controls.Comparisons;

public interface ITargetedActionsManager
{
    public void AddTargetedAction(AtomicAction atomicAction, ComparisonItem comparisonItem);
    
    public void AddTargetedAction(AtomicAction atomicAction, ICollection<ComparisonItem> comparisonItems);
    
    void ClearTargetedActions(ComparisonItemViewModel comparisonItemViewModel);
    
    void RemoveTargetedAction(ComparisonItemViewModel comparisonItemViewModel, SynchronizationActionViewModel synchronizationActionViewModel);
}