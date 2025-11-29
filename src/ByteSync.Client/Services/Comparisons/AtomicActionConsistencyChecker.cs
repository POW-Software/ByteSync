using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Controls.Comparisons;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.FileSystems;

namespace ByteSync.Services.Comparisons;

public class AtomicActionConsistencyChecker : IAtomicActionConsistencyChecker
{
    private readonly IAtomicActionRepository _atomicActionRepository;
    private readonly ISessionService _sessionService;
    
    public AtomicActionConsistencyChecker(IAtomicActionRepository atomicActionRepository, ISessionService sessionService)
    {
        _atomicActionRepository = atomicActionRepository;
        _sessionService = sessionService;
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
            var validationResult = CanApply(atomicAction, comparisonItem);
            if (validationResult.IsValid)
            {
                result.ValidationResults.Add(new ComparisonItemValidationResult(comparisonItem, true));
            }
            else
            {
                result.ValidationResults.Add(new ComparisonItemValidationResult(comparisonItem, validationResult.FailureReason!.Value));
            }
        }
        
        return result;
    }
    
    public List<AtomicAction> GetApplicableActions(ICollection<SynchronizationRule> synchronizationRules)
    {
        var applicableActions = new List<AtomicAction>();
        
        var allActions = new List<AtomicAction>();
        foreach (var synchronizationRule in synchronizationRules)
        {
            allActions.AddAll(synchronizationRule.Actions);
        }
        
        var doNothingAction = allActions.FirstOrDefault(a => a.IsDoNothing);
        if (doNothingAction != null)
        {
            // If one of the actions is a doNothing, we will only use that one
            applicableActions.Add(doNothingAction);
        }
        else
        {
            // Otherwise, we look one by one
            foreach (var atomicAction in allActions)
            {
                if (CheckConsistencyAgainstAlreadySetActions(atomicAction, applicableActions).IsValid)
                {
                    applicableActions.Add(atomicAction);
                }
            }
        }
        
        return applicableActions;
    }
    
    private AtomicActionValidationResult CanApply(AtomicAction atomicAction, ComparisonItem comparisonItem)
    {
        var basicConsistencyResult = CheckBasicConsistency(atomicAction, comparisonItem);
        if (!basicConsistencyResult.IsValid)
        {
            return basicConsistencyResult;
        }
        
        var advancedConsistencyResult = CheckAdvancedConsistency(atomicAction, comparisonItem);
        if (!advancedConsistencyResult.IsValid)
        {
            return advancedConsistencyResult;
        }
        
        var consistencyAgainstAlreadySetActionsResult = CheckConsistencyAgainstAlreadySetActions(atomicAction, comparisonItem);
        if (!consistencyAgainstAlreadySetActionsResult.IsValid)
        {
            return consistencyAgainstAlreadySetActionsResult;
        }
        
        return AtomicActionValidationResult.Success();
    }
    
    private static AtomicActionValidationResult CheckBasicConsistency(AtomicAction atomicAction, ComparisonItem comparisonItem)
    {
        if (atomicAction.Operator.In(ActionOperatorTypes.SynchronizeContentAndDate, ActionOperatorTypes.SynchronizeContentOnly,
                ActionOperatorTypes.SynchronizeDate))
        {
            if (comparisonItem.FileSystemType == FileSystemTypes.Directory)
            {
                return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.SynchronizeOperationOnDirectoryNotAllowed);
            }
            
            if (atomicAction.Source == null)
            {
                return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.SourceRequiredForSynchronizeOperation);
            }
            
            if (atomicAction.Destination == null)
            {
                return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.DestinationRequiredForSynchronizeOperation);
            }
        }
        else if (atomicAction.Operator == ActionOperatorTypes.DoNothing)
        {
            // ReSharper disable once DuplicatedStatements
            return AtomicActionValidationResult.Success();
        }
        else if (atomicAction.Operator == ActionOperatorTypes.Delete)
        {
            if (atomicAction.Source != null)
            {
                return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.SourceNotAllowedForDeleteOperation);
            }
            
            if (atomicAction.Destination == null)
            {
                return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.DestinationRequiredForDeleteOperation);
            }
        }
        else if (atomicAction.Operator == ActionOperatorTypes.Create)
        {
            if (comparisonItem.FileSystemType == FileSystemTypes.File)
            {
                return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.CreateOperationOnFileNotAllowed);
            }
            
            if (atomicAction.Source != null)
            {
                return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.SourceNotAllowedForCreateOperation);
            }
            
            if (atomicAction.Destination == null)
            {
                return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.DestinationRequiredForCreateOperation);
            }
        }
        else
        {
            throw new ApplicationException("AtomicActionConsistencyChecker: unknown action '{synchronizationAction.Action}'");
        }
        
        return AtomicActionValidationResult.Success();
    }
    
    private AtomicActionValidationResult CheckAdvancedConsistency(AtomicAction atomicAction, ComparisonItem comparisonItem)
    {
        var enforceInventoryPartAccessGuard = _sessionService.CurrentSessionSettings?.MatchingMode == MatchingModes.Flat;
        
        if (atomicAction.Operator.In(ActionOperatorTypes.SynchronizeContentAndDate, ActionOperatorTypes.SynchronizeContentOnly,
                ActionOperatorTypes.SynchronizeDate))
        {
            if (atomicAction.Source != null)
            {
                var sourceInventoryPart = atomicAction.Source.GetApplicableInventoryPart();
                
                if (enforceInventoryPartAccessGuard && sourceInventoryPart.IsIncompleteDueToAccess)
                {
                    return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.SourceNotAccessible);
                }
                
                var contentIdentitiesSources = comparisonItem.GetContentIdentities(sourceInventoryPart);
                
                if (contentIdentitiesSources.Count != 1)
                {
                    return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.InvalidSourceCount);
                }
                
                var contentIdentitySource = contentIdentitiesSources.Single();
                
                if (contentIdentitySource.HasAnalysisError)
                {
                    return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.SourceHasAnalysisError);
                }
                
                // Block if source is present but inaccessible
                var sourceFsd = contentIdentitySource.GetFileSystemDescriptions(sourceInventoryPart);
                if (sourceFsd.Any(fsd => fsd is FileDescription && !fsd.IsAccessible))
                {
                    return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.SourceNotAccessible);
                }
                
                var targetInventoryPart = atomicAction.Destination!.GetApplicableInventoryPart();
                
                if (enforceInventoryPartAccessGuard && targetInventoryPart.IsIncompleteDueToAccess)
                {
                    return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.AtLeastOneTargetsNotAccessible);
                }
                
                var contentIdentityViewsTargets = comparisonItem.GetContentIdentities(targetInventoryPart);
                
                if (contentIdentityViewsTargets.Count == 0 && targetInventoryPart.InventoryPartType == FileSystemTypes.File)
                {
                    return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.TargetFileNotPresent);
                }
                
                if (contentIdentitySource.InventoryPartsByLastWriteTimes.Count == 1
                    && contentIdentityViewsTargets.Count > 0
                    && contentIdentitySource.Core != null
                    && contentIdentityViewsTargets.All(ci => ci.Core != null && ci.Core.Equals(contentIdentitySource.Core))
                    && contentIdentityViewsTargets.All(ci => ci.InventoryPartsByLastWriteTimes.Count == 1
                                                             && ci.InventoryPartsByLastWriteTimes.Keys.Single()
                                                                 .Equals(contentIdentitySource.InventoryPartsByLastWriteTimes.Keys
                                                                     .Single())))
                {
                    return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.NothingToCopyContentAndDateIdentical);
                }
                
                if (contentIdentityViewsTargets.Count > 0 && contentIdentityViewsTargets.Any(t => t.HasAnalysisError))
                {
                    return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.AtLeastOneTargetsHasAnalysisError);
                }
                
                // Block if at least one target is present but inaccessible
                if (contentIdentityViewsTargets.Count > 0 && contentIdentityViewsTargets
                        .Any(t => t.GetFileSystemDescriptions(targetInventoryPart).Any(fsd => fsd is FileDescription && !fsd.IsAccessible)))
                {
                    return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.AtLeastOneTargetsNotAccessible);
                }
                
                if (atomicAction.IsSynchronizeContentOnly && contentIdentityViewsTargets.Count != 0 &&
                    contentIdentitySource.Core != null &&
                    contentIdentityViewsTargets.All(t => t.Core != null && contentIdentitySource.Core.Equals(t.Core)))
                {
                    return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.NothingToCopyContentIdentical);
                }
            }
        }
        
        if (atomicAction.IsSynchronizeDate || atomicAction.IsDelete)
        {
            var targetInventoryPart = atomicAction.Destination!.GetApplicableInventoryPart();
            
            if (enforceInventoryPartAccessGuard && targetInventoryPart.IsIncompleteDueToAccess)
            {
                return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.AtLeastOneTargetsNotAccessible);
            }
            
            var contentIdentitiesTargets = comparisonItem.GetContentIdentities(targetInventoryPart);
            
            if (contentIdentitiesTargets.Count == 0)
            {
                return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.TargetRequiredForSynchronizeDateOrDelete);
            }
            
            // Block if any target is inaccessible
            if (contentIdentitiesTargets.Any(t =>
                    t.GetFileSystemDescriptions(targetInventoryPart).Any(fsd => fsd is FileDescription && !fsd.IsAccessible)))
            {
                return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.AtLeastOneTargetsNotAccessible);
            }
        }
        
        if (atomicAction.IsCreate)
        {
            var targetInventoryPart = atomicAction.Destination!.GetApplicableInventoryPart();
            
            if (targetInventoryPart.InventoryPartType == FileSystemTypes.File)
            {
                return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.CreateOperationRequiresDirectoryTarget);
            }
            
            if (enforceInventoryPartAccessGuard && targetInventoryPart.IsIncompleteDueToAccess)
            {
                return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.AtLeastOneTargetsNotAccessible);
            }
            
            var contentIdentitiesTargets = comparisonItem.GetContentIdentities(targetInventoryPart);
            
            if (contentIdentitiesTargets.Count != 0)
            {
                return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.TargetAlreadyExistsForCreateOperation);
            }
        }
        
        return AtomicActionValidationResult.Success();
    }
    
    private AtomicActionValidationResult CheckConsistencyAgainstAlreadySetActions(AtomicAction atomicAction, ComparisonItem comparisonItem)
    {
        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        var alreadySetAtomicActions = _atomicActionRepository.GetAtomicActions(comparisonItem) ?? new List<AtomicAction>();
        
        if (atomicAction.IsTargeted)
        {
            alreadySetAtomicActions = alreadySetAtomicActions
                .Where(a => a.IsTargeted)
                .ToList();
        }
        
        return CheckConsistencyAgainstAlreadySetActions(atomicAction, alreadySetAtomicActions);
    }
    
    private AtomicActionValidationResult CheckConsistencyAgainstAlreadySetActions(AtomicAction atomicAction,
        List<AtomicAction> alreadySetAtomicActions)
    {
        if (alreadySetAtomicActions.Count == 0)
        {
            return AtomicActionValidationResult.Success();
        }
        
        if (!atomicAction.IsTargeted && alreadySetAtomicActions.Any(a => a.IsDoNothing))
        {
            return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason
                .NonTargetedActionNotAllowedWithExistingDoNothingAction);
        }
        
        if (alreadySetAtomicActions.Any(ma =>
                !atomicAction.IsDelete && // 16/02/2023: What is the purpose of this IsDelete?
                Equals(ma.Destination, atomicAction.Source)))
        {
            return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.SourceCannotBeDestinationOfAnotherAction);
        }
        
        if (alreadySetAtomicActions.Any(ma => Equals(ma.Source, atomicAction.Destination)))
        {
            return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.DestinationCannotBeSourceOfAnotherAction);
        }
        
        if (alreadySetAtomicActions.Any(ma => Equals(ma.Destination, atomicAction.Destination)))
        {
            if (alreadySetAtomicActions.Count == 1)
            {
                var alreadySetAtomicAction = alreadySetAtomicActions.Single();
                
                if ((!alreadySetAtomicAction.IsSynchronizeDate || !atomicAction.IsSynchronizeContentOnly)
                    && (!alreadySetAtomicAction.IsSynchronizeContentOnly || !atomicAction.IsSynchronizeDate))
                {
                    return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason
                        .DestinationAlreadyUsedByNonComplementaryAction);
                }
            }
            else
            {
                return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason
                    .DestinationAlreadyUsedByNonComplementaryAction);
            }
        }
        
        if (atomicAction.Operator == ActionOperatorTypes.Delete)
        {
            if (alreadySetAtomicActions.Any(ma =>
                    Equals(ma.Destination, atomicAction.Destination) || Equals(ma.Source, atomicAction.Destination)))
            {
                return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.CannotDeleteItemAlreadyUsedInAnotherAction);
            }
        }
        
        if (alreadySetAtomicActions.Any(ma => ma.Operator == ActionOperatorTypes.Delete &&
                                              (Equals(ma.Destination, atomicAction.Destination) ||
                                               Equals(ma.Destination, atomicAction.Source))))
        {
            return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.CannotOperateOnItemBeingDeleted);
        }
        
        if (atomicAction.Operator != ActionOperatorTypes.DoNothing && alreadySetAtomicActions.Any(s => s.IsSimilarTo(atomicAction)))
        {
            return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.DuplicateActionNotAllowed);
        }
        
        return AtomicActionValidationResult.Success();
    }
}