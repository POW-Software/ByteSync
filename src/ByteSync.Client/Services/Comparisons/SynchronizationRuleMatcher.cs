using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Interfaces.Controls.Comparisons;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Repositories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Services.Comparisons.ConditionMatchers;

namespace ByteSync.Services.Comparisons;

public class SynchronizationRuleMatcher : ISynchronizationRuleMatcher
{
    private readonly IAtomicActionConsistencyChecker _atomicActionConsistencyChecker;
    private readonly IAtomicActionRepository _atomicActionRepository;
    private readonly ConditionMatcherFactory _conditionMatcherFactory;
    
    public SynchronizationRuleMatcher(IAtomicActionConsistencyChecker atomicActionConsistencyChecker,
        IAtomicActionRepository atomicActionRepository,
        ConditionMatcherFactory conditionMatcherFactory)
    {
        _atomicActionConsistencyChecker = atomicActionConsistencyChecker;
        _atomicActionRepository = atomicActionRepository;
        _conditionMatcherFactory = conditionMatcherFactory;
    }
    
    public void MakeMatches(ICollection<ComparisonItem> comparisonItems, ICollection<SynchronizationRule> synchronizationRules)
    {
        var allAtomicActions = new HashSet<AtomicAction>();
        foreach (var comparisonItem in comparisonItems)
        {
            var atomicActions = DoMakeMatches(comparisonItem, synchronizationRules);
            allAtomicActions.UnionWith(atomicActions);
        }
        
        _atomicActionRepository.AddOrUpdate(allAtomicActions);
    }
    
    public void MakeMatches(ComparisonItem comparisonItem, ICollection<SynchronizationRule> synchronizationRules)
    {
        var atomicActions = DoMakeMatches(comparisonItem, synchronizationRules);
        
        _atomicActionRepository.AddOrUpdate(atomicActions);
    }
    
    private HashSet<AtomicAction> DoMakeMatches(ComparisonItem comparisonItem, ICollection<SynchronizationRule> synchronizationRules)
    {
        var initialAtomicActions = _atomicActionRepository.GetAtomicActions(comparisonItem);
        var actionsToRemove = initialAtomicActions.Where(a => a.IsFromSynchronizationRule).ToList();
        _atomicActionRepository.Remove(actionsToRemove);
        
        var atomicActions = GetApplicableActions(comparisonItem, synchronizationRules);
        
        return atomicActions;
    }
    
    private HashSet<AtomicAction> GetApplicableActions(ComparisonItem comparisonItem,
        ICollection<SynchronizationRule> synchronizationRules)
    {
        var result = new HashSet<AtomicAction>();
        
        var matchingSynchronizationRules = synchronizationRules.Where(sr => ConditionsMatch(sr, comparisonItem)).ToList();
        
        var atomicActions = _atomicActionConsistencyChecker.GetApplicableActions(matchingSynchronizationRules);
        foreach (var atomicAction in atomicActions)
        {
            var clonedAtomicAction = atomicAction.CloneNew();
            
            var checkResult = _atomicActionConsistencyChecker.CheckCanAdd(clonedAtomicAction, comparisonItem);
            if (checkResult.IsOK)
            {
                clonedAtomicAction.ComparisonItem = comparisonItem;
                result.Add(clonedAtomicAction);
            }
        }
        
        return result;
    }
    
    public bool ConditionsMatch(SynchronizationRule synchronizationRule, ComparisonItem comparisonItem)
    {
        if (synchronizationRule.Conditions.Count == 0)
        {
            return false;
        }
        
        if (synchronizationRule.FileSystemType != comparisonItem.FileSystemType)
        {
            return false;
        }
        
        var conditionResults = synchronizationRule.Conditions
            .Select(condition => _conditionMatcherFactory.GetMatcher(condition.ComparisonProperty).Matches(condition, comparisonItem))
            .ToList();
        
        if (synchronizationRule.ConditionMode == ConditionModes.All)
        {
            return conditionResults.All(r => r);
        }
        else
        {
            return conditionResults.Any(r => r);
        }
    }
}