using ByteSync.Business.Actions.Local;
using ByteSync.Common.Business.Actions;
using ByteSync.Interfaces.Controls.Comparisons;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Repositories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.ViewModels.Sessions.Comparisons.Results;
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

    public void ClearTargetedActions(ComparisonItemViewModel comparisonItemViewModel)
    {
        var atomicActions = comparisonItemViewModel.SynchronizationActions
            .Select(sa => sa.AtomicAction)
            .Where(a => a.IsTargeted);

        _atomicActionRepository.Remove(atomicActions);
        
        ResetActionsFromSynchronizationRules(comparisonItemViewModel);
    }

    public void RemoveTargetedAction(ComparisonItemViewModel comparisonItemViewModel, SynchronizationActionViewModel synchronizationActionViewModel)
    {
        var atomicAction = synchronizationActionViewModel.AtomicAction;
        if (atomicAction.IsTargeted)
        {
            _atomicActionRepository.Remove(atomicAction);
        }
        
        ResetActionsFromSynchronizationRules(comparisonItemViewModel);
    }

    private void ResetActionsFromSynchronizationRules(ComparisonItemViewModel comparisonItemViewModel)
    {
        _synchronizationRuleMatcher.MakeMatches(comparisonItemViewModel.ComparisonItem,
            _synchronizationRuleRepository.Elements.ToList());
    }
}