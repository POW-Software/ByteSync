using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Controls.Comparisons;
using ByteSync.Interfaces.Repositories;
using ByteSync.Models.Comparisons.Result;

namespace ByteSync.Services.Comparisons;

public class AtomicActionConsistencyChecker : IAtomicActionConsistencyChecker
{
    private readonly IAtomicActionRepository _atomicActionRepository;

    public AtomicActionConsistencyChecker(IAtomicActionRepository atomicActionRepository)
    {
        _atomicActionRepository = atomicActionRepository;
    }
    
    public AtomicActionConsistencyCheckCanAddResult CheckCanAdd(AtomicAction atomicAction, ComparisonItem comparisonItem)
    {
        return CheckCanAdd(atomicAction, new List<ComparisonItem> { comparisonItem });
    }

    public AtomicActionConsistencyCheckCanAddResult CheckCanAdd(AtomicAction atomicAction, ICollection<ComparisonItem> comparisonItems)
    {
        var result = new AtomicActionConsistencyCheckCanAddResult(comparisonItems);
        
        foreach (var comparisonItem in comparisonItems)
        {
            if (CanApply(atomicAction, comparisonItem))
            {
                result.ValidComparisons.Add(comparisonItem);
            }
            else
            {
                result.NonValidComparisons.Add(comparisonItem);
            }
        }

        return result;
    }

    public List<AtomicAction> GetApplicableActions(ICollection<SynchronizationRule> synchronizationRules)
    {
        List<AtomicAction> appliableActions = new List<AtomicAction>();

        var allActions = new List<AtomicAction>();
        foreach (var synchronizationRule in synchronizationRules)
        {
            allActions.AddAll(synchronizationRule.Actions);
        }
        
        var doNothingAction = allActions.FirstOrDefault(a => a.IsDoNothing);
        if (doNothingAction != null)
        {
            // If one of the actions is a doNothing, we will only use that one
            appliableActions.Add(doNothingAction);
        }
        else
        {
            // Otherwise, we look one by one
            foreach (var atomicAction in allActions)
            {
                if (CheckConsistencyAgainstAlreadySetActions(atomicAction, appliableActions))
                {
                    appliableActions.Add(atomicAction);
                }
            }
        }

        return appliableActions;
    }
    
    private bool CanApply(AtomicAction atomicAction, ComparisonItem comparisonItem)
    {
        var isBasicConsistencyOK = CheckBasicConsistency(atomicAction, comparisonItem);
        if (!isBasicConsistencyOK)
        {
            return false;
        }
        
        var isAdvancedConsistencyOK = CheckAdvancedConsistency(atomicAction, comparisonItem);
        if (!isAdvancedConsistencyOK)
        {
            return false;
        }

        var isConsistencyAgainstAlreadySetActionsOK = CheckConsistencyAgainstAlreadySetActions(atomicAction, comparisonItem);
        if (!isConsistencyAgainstAlreadySetActionsOK)
        {
            return false;
        }

        return true;
    }

    private bool CheckBasicConsistency(AtomicAction atomicAction, ComparisonItem comparisonItem)
    {
        if (atomicAction.Operator.In(ActionOperatorTypes.SynchronizeContentAndDate, ActionOperatorTypes.SynchronizeContentOnly,
                ActionOperatorTypes.SynchronizeDate))
        {
            if (comparisonItem.FileSystemType == FileSystemTypes.Directory)
            {
                // These operators cannot be applied to a Directory
                return false;
            }
                
            // On a content and/or date copy, Source and Destination must be defined
            if (atomicAction.Source == null)
            {
                return false;
            }
                
            if (atomicAction.Destination == null)
            {
                return false;
            }
        }
        else if (atomicAction.Operator == ActionOperatorTypes.DoNothing)
        {
            // Do nothing: always OK
            return true;
        }
        else if (atomicAction.Operator == ActionOperatorTypes.Delete)
        {
            // On a deletion, Source must always be null, Destination must always be defined
            if (atomicAction.Source != null)
            {
                return false;
            }
                
            if (atomicAction.Destination == null)
            {
                return false;
            }
        }
        else if (atomicAction.Operator == ActionOperatorTypes.Create)
        {
            if (comparisonItem.FileSystemType == FileSystemTypes.File)
            {
                // This operator cannot be applied to a File
                return false;
            }
                
            // On a creation, Source must always be null, Destination must always be defined
            if (atomicAction.Source != null)
            {
                return false;
            }
                
            if (atomicAction.Destination == null)
            {
                return false;
            }
        }
        else
        {
            throw new ApplicationException("AtomicActionConsistencyChecker: unknown action '{synchronizationAction.Action}'");
        }

        return true;
    }
    
    private bool CheckAdvancedConsistency(AtomicAction atomicAction, ComparisonItem comparisonItem)
    {
        if (atomicAction.Operator.In(ActionOperatorTypes.SynchronizeContentAndDate, ActionOperatorTypes.SynchronizeContentOnly,
        ActionOperatorTypes.SynchronizeDate))
        {
            if (atomicAction.Source != null)
            {
                // var contentIdentityViewsSource =
                //     comparisonItemViewModel.GetContentIdentityViews(synchronizationAction.Source.GetAppliableInventory());

                var sourceInventoryPart = atomicAction.Source.GetApplicableInventoryPart();
                    
                var contentIdentitiesSources = comparisonItem.GetContentIdentities(sourceInventoryPart);

                if (contentIdentitiesSources.Count != 1)
                {
                    // No source or too many sources!
                    return false;
                }
                var contentIdentitySource = contentIdentitiesSources.Single();
                
                if (contentIdentitySource.HasAnalysisError)
                {
                    // If the source has an analysis error, we cannot proceed
                    return false;
                }
                    
                    
                var targetInventoryPart = atomicAction.Destination.GetApplicableInventoryPart();
                var contentIdentityViewsTargets = comparisonItem.GetContentIdentities(targetInventoryPart);

                // We cannot send to an InventoryPartTypes.File that is not present
                if (contentIdentityViewsTargets.Count == 0 && targetInventoryPart.InventoryPartType == FileSystemTypes.File)
                {
                    return false;
                }
                    
                if (contentIdentitySource.InventoryPartsByLastWriteTimes.Count == 1 
                    && contentIdentityViewsTargets.Count > 0 
                    && contentIdentityViewsTargets.All(ci => ci.Core!.Equals(contentIdentitySource.Core!))
                    && contentIdentityViewsTargets.All(ci => ci.InventoryPartsByLastWriteTimes.Count == 1 
                                                             && ci.InventoryPartsByLastWriteTimes.Keys.Single()
                                                                 .Equals(contentIdentitySource.InventoryPartsByLastWriteTimes.Keys.Single())))
                {
                    // In this case, the content is the same and there is only one date => there is nothing to copy
                    return false;
                }

                if (contentIdentityViewsTargets.Count > 0 && contentIdentityViewsTargets.All(t => t.HasAnalysisError))
                {
                    // If all targets have an analysis error, we cannot proceed
                    return false;
                }
                
                // If CopyContentOnly and no target or if a target with different content, it's OK
                // We invert the condition
                if (atomicAction.IsSynchronizeContentOnly && contentIdentityViewsTargets.Count != 0 &&
                    contentIdentityViewsTargets.All(t => contentIdentitySource.Core!.Equals(t.Core!)))
                {
                    return false;
                }
            }
        }

        if (atomicAction.IsSynchronizeDate || atomicAction.IsDelete)
        {
            var targetInventoryPart = atomicAction.Destination.GetApplicableInventoryPart();
            var contentIdentitiesTargets = comparisonItem.GetContentIdentities(targetInventoryPart);
                
            if (contentIdentitiesTargets.Count == 0)
            {
                // No destination, forbidden on a date or a deletion 
                return false;
            }
        }
            
        if (atomicAction.IsCreate)
        {
            var targetInventoryPart = atomicAction.Destination.GetApplicableInventoryPart();
                
            // We cannot do anything on a target of type InventoryPartTypes.File
            if (targetInventoryPart.InventoryPartType == FileSystemTypes.File)
            {
                return false;
            }
                
            var contentIdentitiesTargets = comparisonItem.GetContentIdentities(targetInventoryPart);

            if (contentIdentitiesTargets.Count != 0)
            {
                // There is a destination, forbidden on a directory creation
                return false;
            }
        }

        return true;
    }

    private bool CheckConsistencyAgainstAlreadySetActions(AtomicAction atomicAction, ComparisonItem comparisonItem)
    {
        List<AtomicAction> alreadySetAtomicActions = _atomicActionRepository.GetAtomicActions(comparisonItem);

        if (atomicAction.IsTargeted)
        {
            alreadySetAtomicActions = alreadySetAtomicActions
                .Where(a => a.IsTargeted)
                .ToList();
        }

        return CheckConsistencyAgainstAlreadySetActions(atomicAction, alreadySetAtomicActions);
    }

    private bool CheckConsistencyAgainstAlreadySetActions(AtomicAction atomicAction, List<AtomicAction> alreadySetAtomicActions)
    {
        if (alreadySetAtomicActions.Count == 0)
        {
            return true;
        }
        
        if (!atomicAction.IsTargeted && alreadySetAtomicActions.Any(a => a.IsDoNothing))
        {
            // If the action is not Targeted and an action is already in DoNothing, we cannot register it
            return false;
        }

        // A source cannot be a destination
        // We cannot be a destination multiple times, but we can be a source multiple times
        // We cannot be deleted if we are the source or destination of another action
        if (alreadySetAtomicActions.Any(ma =>
                !atomicAction.IsDelete && // 16/02/2023: What is the purpose of this IsDelete?
                Equals(ma.Destination, atomicAction.Source)))
        {
            // A source cannot be the destination of another already registered action
            return false;
        }
            
        if (alreadySetAtomicActions.Any(ma => Equals(ma.Source, atomicAction.Destination)))
        {
            // A destination cannot be the source of another already registered action
            return false;
        }
        
        if (alreadySetAtomicActions.Any(ma => Equals(ma.Destination, atomicAction.Destination)))
        {
            if (alreadySetAtomicActions.Count == 1)
            {
                var alreadySetAtomicAction = alreadySetAtomicActions.Single();

                if ((!alreadySetAtomicAction.IsSynchronizeDate || !atomicAction.IsSynchronizeContentOnly)
                    && (!alreadySetAtomicAction.IsSynchronizeContentOnly || !atomicAction.IsSynchronizeDate))
                {
                    // We can be a destination multiple times if one is in IsSynchronizeDate and the other in IsSynchronizeContentOnly
                    // Because they are complementary
                    
                    // Otherwise, it's not OK
                    return false;
                }
            }
            else
            {
                // We cannot be a destination multiple times => A destination cannot be the destination of another already registered action
                return false;
            }
        }

        if (atomicAction.Operator == ActionOperatorTypes.Delete)
        {
            if (alreadySetAtomicActions.Any(ma => 
                    Equals(ma.Destination, atomicAction.Destination) || Equals(ma.Source, atomicAction.Destination)))
            {
                // Impossible to register the deletion operation if the destination is already the source or destination of another action
                return false;
            }
        }
            
        if (alreadySetAtomicActions.Any(ma => ma.Operator == ActionOperatorTypes.Delete &&
                (Equals(ma.Destination, atomicAction.Destination) || Equals(ma.Destination, atomicAction.Source))))
        {
            // Impossible to register an operation if the Source or Destination are the destination of a deletion
            return false;
        }

        if (atomicAction.Operator != ActionOperatorTypes.DoNothing && alreadySetAtomicActions.Any(s => s.IsSimilarTo(atomicAction)))
        {
            // Impossible to register a duplicate of an already registered action
            return false;
        }

        return true;
    }
}