using ByteSync.Business.Actions.Local;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces.Controls.Comparisons;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Repositories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;

namespace ByteSync.Services.Comparisons;

public class ComparisonItemActionsManager : IComparisonItemActionsManager
{
    private readonly ISynchronizationRuleMatcher _synchronizationRuleMatcher;
    private readonly IAtomicActionRepository _atomicActionRepository;
    private readonly ISynchronizationRuleRepository _synchronizationRuleRepository;

    public ComparisonItemActionsManager(ISynchronizationRuleMatcher synchronizationRuleMatcher, IAtomicActionRepository atomicActionRepository,
        ISynchronizationRuleRepository synchronizationRuleRepository)
    {
        _synchronizationRuleMatcher = synchronizationRuleMatcher;
        _atomicActionRepository = atomicActionRepository;
        _synchronizationRuleRepository = synchronizationRuleRepository;
    }
    
    public void AddTargetedAction(AtomicAction atomicAction, ComparisonItem comparisonItem)
    {
        var currentAtomicActions = _atomicActionRepository.GetAtomicActions(comparisonItem);
        var currentTargetedAtomicActions = currentAtomicActions.Where(aa => aa.IsTargeted).ToList();
        
        atomicAction = atomicAction.CloneNew();

        var clearBefore = false;
        if (atomicAction.Operator == ActionOperatorTypes.DoNothing)
        {
            clearBefore = true;
        }
        else
        {
            if (currentTargetedAtomicActions.Count == 1 && 
                currentTargetedAtomicActions.First().Operator == ActionOperatorTypes.DoNothing)
            {
                clearBefore = true;
            }
        }

        if (clearBefore)
        {
            _atomicActionRepository.Remove(currentTargetedAtomicActions);
        }

        atomicAction.ComparisonItem = comparisonItem;

        _atomicActionRepository.AddOrUpdate(atomicAction);
    }

    public void AddTargetedAction(AtomicAction atomicAction, ICollection<ComparisonItem> comparisonItems)
    {
        foreach (var comparisonItem in comparisonItems)
        {
            AddTargetedAction(atomicAction, comparisonItem);
        }
    }

    public void RemoveTargetedAction(AtomicAction atomicAction, ComparisonItemViewModel comparisonItemViewModel)
    {
        if (atomicAction.IsTargeted)
        {
            comparisonItemViewModel.TargetedActions.Remove(atomicAction);
            
            comparisonItemViewModel.TD_SynchronizationActions.RemoveAll(savm => savm.IsTargeted && savm.AtomicAction.Equals(atomicAction));
            
            ResetActionsFromSynchronizationRules(comparisonItemViewModel);
        }
    }

    public void ClearTargetedActions(ComparisonItemViewModel comparisonItemViewModel)
    {
        comparisonItemViewModel.TargetedActions.Clear();

        // WHAT !!!
        comparisonItemViewModel.TD_SynchronizationActions.RemoveAll(savm => savm.IsTargeted);
        
        ResetActionsFromSynchronizationRules(comparisonItemViewModel);
    }
    
    private void ResetActionsFromSynchronizationRules(ComparisonItemViewModel comparisonItemViewModel)
    {
        comparisonItemViewModel.TD_SynchronizationActions.RemoveAll(savm => savm.IsFromSynchronizationRule);
        
        _synchronizationRuleMatcher.MakeMatches(comparisonItemViewModel.ComparisonItem,
            _synchronizationRuleRepository.Elements.ToList());
    }
}