using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Controls.Comparisons;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Repositories;
using ByteSync.Models.Comparisons.Result;
using System.Text.RegularExpressions;

namespace ByteSync.Services.Comparisons;

public class SynchronizationRuleMatcher : ISynchronizationRuleMatcher
{
    private readonly IAtomicActionConsistencyChecker _atomicActionConsistencyChecker;
    private readonly IAtomicActionRepository _atomicActionRepository;

    public SynchronizationRuleMatcher(IAtomicActionConsistencyChecker atomicActionConsistencyChecker, 
        IAtomicActionRepository atomicActionRepository)
    {
        _atomicActionConsistencyChecker = atomicActionConsistencyChecker;
        _atomicActionRepository = atomicActionRepository;
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
        
        HashSet<AtomicAction> atomicActions = GetApplicableActions(comparisonItem, synchronizationRules);
        return atomicActions;
    }

    private HashSet<AtomicAction> GetApplicableActions(ComparisonItem comparisonItem, 
        ICollection<SynchronizationRule> synchronizationRules)
    {
        HashSet<AtomicAction> result = new HashSet<AtomicAction>();

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

    private bool ConditionsMatch(SynchronizationRule synchronizationRule, ComparisonItem comparisonItem)
    {
        if (synchronizationRule.Conditions.Count == 0)
        {
            return false;
        }

        if (synchronizationRule.FileSystemType != comparisonItem.FileSystemType)
        {
            return false;
        }
            
        var areAllConditionsOK = true;
        var isOneConditionOK = false;

        foreach (var condition in synchronizationRule.Conditions)
        {
            var isConditionOK = ConditionMatches(condition, comparisonItem);

            if (!isConditionOK)
            {
                areAllConditionsOK = false;
            }
            else
            {
                isOneConditionOK = true;
            }
        }

        if (synchronizationRule.ConditionMode == ConditionModes.All)
        {
            return areAllConditionsOK;
        }
        else
        {
            return isOneConditionOK;
        }
    }

    private bool ConditionMatches(AtomicCondition condition, ComparisonItem comparisonItem)
    {
        switch (condition.ComparisonProperty)
        {
            case ComparisonProperty.Content:
                return ConditionMatchesContent(condition, comparisonItem);
            case ComparisonProperty.Size:
                return ConditionMatchesSize(condition, comparisonItem);
            case ComparisonProperty.Date:
                return ConditionMatchesDate(condition, comparisonItem);
            case ComparisonProperty.Presence:
                return ConditionMatchesPresence(condition, comparisonItem);
            case ComparisonProperty.Name:
                return ConditionMatchesName(condition, comparisonItem);
            default:
                return false;
        }
    }

    private bool ConditionMatchesContent(AtomicCondition condition, ComparisonItem comparisonItem)
    {
        bool? result = null;

        if (comparisonItem.FileSystemType == FileSystemTypes.Directory)
        {
            return false;
        }
            
        var contentIdentitySource = ExtractContentIdentity(condition.Source, comparisonItem);
        var contentIdentityDestination = ExtractContentIdentity(condition.Destination, comparisonItem);

        if (contentIdentitySource != null && contentIdentitySource.HasAnalysisError
            || contentIdentityDestination != null && contentIdentityDestination.HasAnalysisError)
        {
            return false;
        }

        switch (condition.ConditionOperator)
        {
            case ConditionOperatorTypes.Equals:
                if (contentIdentitySource == null && contentIdentityDestination != null)
                {
                    result = false;
                }
                else if (contentIdentitySource != null && contentIdentityDestination == null)
                {
                    result = false;
                }
                else
                {
                    result = Equals(contentIdentitySource?.Core!.SignatureHash, contentIdentityDestination?.Core!.SignatureHash);
                }
                    
                break;
            case ConditionOperatorTypes.NotEquals:
                if (contentIdentitySource == null && contentIdentityDestination != null)
                {
                    result = true;
                }
                else if (contentIdentitySource != null && contentIdentityDestination == null)
                {
                    result = true;
                }
                else
                {
                    result = ! Equals(contentIdentitySource?.Core!.SignatureHash, contentIdentityDestination?.Core!.SignatureHash);
                }
                break;
        }
            
        if (result == null)
        {
            throw new ArgumentOutOfRangeException("ConditionMatchesContent " + condition.ConditionOperator);
        }

        return result.Value;
    }
        
    private bool ExistsOn(DataPart? dataPart, ComparisonItem comparisonItem)
    {
        if (dataPart == null)
        {
            return false;
        }
            
        var contentIdentity = LocalizeContentIdentity(dataPart, comparisonItem);

        if (comparisonItem.FileSystemType == FileSystemTypes.File)
        {
            return contentIdentity?.Core != null;
        }
        else
        {
            return contentIdentity != null;
        }
    }

    private ContentIdentity? ExtractContentIdentity(DataPart? dataPart, ComparisonItem comparisonItem)
    {
        if (dataPart == null)
        {
            return null;
        }
            
        var contentIdentity = LocalizeContentIdentity(dataPart, comparisonItem);
        return contentIdentity;
    }
        
    private bool ConditionMatchesSize(AtomicCondition condition, ComparisonItem comparisonItem)
    {
        var sizeSource = ExtractSize(condition.Source, comparisonItem);

        long? sizeDestination;
        if (condition.Destination is { IsVirtual: false })   
        {
            sizeDestination = ExtractSize(condition.Destination, comparisonItem);
        }
        else
        {
            var size = (long)condition.Size!;
            var sizeUnitPower = (int)condition.SizeUnit! - 1;

            sizeDestination = size * (long)Math.Pow(1024, sizeUnitPower);
        }

        if (sizeSource == null || sizeDestination == null)
        {
            return false;
        }

        var result = false;
        switch (condition.ConditionOperator)
        {
            case ConditionOperatorTypes.Equals:
                result = sizeSource == sizeDestination;
                break;
            case ConditionOperatorTypes.NotEquals:
                result = sizeSource != sizeDestination;
                break;
            case ConditionOperatorTypes.IsSmallerThan:
                result = sizeSource < sizeDestination;
                break;
            case ConditionOperatorTypes.IsBiggerThan:
                result = sizeSource > sizeDestination;
                break;
        }

        return result;
    }

    private long? ExtractSize(DataPart dataPart, ComparisonItem comparisonItem)
    {
        var contentIdentity = LocalizeContentIdentity(dataPart, comparisonItem);
        return contentIdentity?.Core?.Size;
    }

    private bool ConditionMatchesDate(AtomicCondition condition, ComparisonItem comparisonItem)
    {
        var lastWriteTimeSource = ExtractDate(condition.Source, comparisonItem);

        DateTime? lastWriteTimeDestination;
        if (condition.Destination is { IsVirtual: false })
        {
            lastWriteTimeDestination = ExtractDate(condition.Destination, comparisonItem);
        }
        else
        {
            lastWriteTimeDestination = condition.DateTime!.Value.ToUniversalTime();
            
            if (lastWriteTimeSource is { Second: 0, Millisecond: 0 })
            {
                lastWriteTimeSource = lastWriteTimeSource.Value.Trim(TimeSpan.TicksPerMinute);
            }
        }

        if (lastWriteTimeSource == null)
        {
            return false;
        }

        var result = false;
        switch (condition.ConditionOperator)
        {
            case ConditionOperatorTypes.Equals:
                result = lastWriteTimeDestination != null && lastWriteTimeSource == lastWriteTimeDestination;
                break;
            case ConditionOperatorTypes.NotEquals:
                result = lastWriteTimeDestination != null && lastWriteTimeSource != lastWriteTimeDestination;
                break;
            case ConditionOperatorTypes.IsNewerThan: 
                result = (condition.Destination is { IsVirtual: false } && lastWriteTimeDestination == null) || 
                         (lastWriteTimeDestination != null && lastWriteTimeSource > lastWriteTimeDestination);
                break;
            case ConditionOperatorTypes.IsOlderThan:
                result = lastWriteTimeDestination != null && lastWriteTimeSource < lastWriteTimeDestination;
                break;
        }

        return result;
    }

    private DateTime? ExtractDate(DataPart dataPart, ComparisonItem comparisonItem)
    {
        var contentIdentity = LocalizeContentIdentity(dataPart, comparisonItem);
            
        if (contentIdentity != null)
        {
            foreach (var pair in contentIdentity.InventoryPartsByLastWriteTimes)
            {
                if (pair.Value.Contains(dataPart.GetApplicableInventoryPart()))
                {
                    return pair.Key;
                }
            }
        }

        return null;
    }
        
    private bool ConditionMatchesPresence(AtomicCondition condition, ComparisonItem comparisonItem)
    {
        bool? result = null;
            
        if (condition.ConditionOperator.In(ConditionOperatorTypes.ExistsOn, ConditionOperatorTypes.NotExistsOn))
        {
            var existsOnSource = ExistsOn(condition.Source, comparisonItem);
            var existsOnDestination = ExistsOn(condition.Destination, comparisonItem);
                
            switch (condition.ConditionOperator)
            {
                case ConditionOperatorTypes.ExistsOn:
                    result = existsOnSource && existsOnDestination;
                    break;
                case ConditionOperatorTypes.NotExistsOn:
                    result = existsOnSource && !existsOnDestination;
                    break;
            }
        }

        if (result == null)
        {
            throw new ArgumentOutOfRangeException("ConditionMatchesPresence " + condition.ConditionOperator);
        }
            
        return result.Value;
    }

    private bool ConditionMatchesName(AtomicCondition condition, ComparisonItem comparisonItem)
    {
        if (string.IsNullOrWhiteSpace(condition.NamePattern))
        {
            return false;
        }

        var name = comparisonItem.PathIdentity.FileName;
        var pattern = condition.NamePattern!;

        bool result = false;

        if (pattern.Contains("*") &&
            condition.ConditionOperator.In(ConditionOperatorTypes.Equals, ConditionOperatorTypes.NotEquals))
        {
            var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            var isMatch = Regex.IsMatch(name, regex, RegexOptions.IgnoreCase);
            result = condition.ConditionOperator == ConditionOperatorTypes.Equals ? isMatch : !isMatch;
        }
        else
        {
            switch (condition.ConditionOperator)
            {
                case ConditionOperatorTypes.Equals:
                    result = string.Equals(name, pattern, StringComparison.OrdinalIgnoreCase);
                    break;
                case ConditionOperatorTypes.NotEquals:
                    result = !string.Equals(name, pattern, StringComparison.OrdinalIgnoreCase);
                    break;
            }
        }

        return result;
    }

    private ContentIdentity? LocalizeContentIdentity(DataPart dataPart, ComparisonItem comparisonItem)
    {
        if (dataPart.Inventory != null)
        {
            foreach (var contentIdentity in comparisonItem.ContentIdentities)
            {
                if (contentIdentity.GetInventories().Contains(dataPart.Inventory))
                {
                    return contentIdentity;
                }
            }
        }
        else if (dataPart.InventoryPart != null)
        {
            foreach (var contentIdentity in comparisonItem.ContentIdentities)
            {
                var inventoryParts = contentIdentity.GetInventoryParts();

                if (inventoryParts.Contains(dataPart.InventoryPart))
                {
                    return contentIdentity;
                }
            }
        }

        return null;
    }
}