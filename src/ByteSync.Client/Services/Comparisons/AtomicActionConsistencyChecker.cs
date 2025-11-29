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
using ByteSync.Models.Inventories;

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
        var enforceInventoryPartAccessGuard = ShouldEnforceInventoryPartAccessGuard();
        
        if (atomicAction.Operator.In(ActionOperatorTypes.SynchronizeContentAndDate, ActionOperatorTypes.SynchronizeContentOnly,
                ActionOperatorTypes.SynchronizeDate))
        {
            return ValidateSynchronize(atomicAction, comparisonItem, enforceInventoryPartAccessGuard);
        }
        
        if (atomicAction.IsSynchronizeDate || atomicAction.IsDelete)
        {
            return ValidateSynchronizeDateOrDelete(atomicAction, comparisonItem, enforceInventoryPartAccessGuard);
        }
        
        if (atomicAction.IsCreate)
        {
            return ValidateCreate(atomicAction, comparisonItem, enforceInventoryPartAccessGuard);
        }
        
        return AtomicActionValidationResult.Success();
    }
    
    private AtomicActionValidationResult ValidateSynchronize(AtomicAction atomicAction, ComparisonItem comparisonItem,
        bool enforceInventoryPartAccessGuard)
    {
        var sourceInventoryPart = atomicAction.Source!.GetApplicableInventoryPart();
        var sourceContentIdentities = comparisonItem.GetContentIdentities(sourceInventoryPart);
        
        var sourceValidation = ValidateSourceForSynchronize(sourceInventoryPart, sourceContentIdentities, enforceInventoryPartAccessGuard);
        if (sourceValidation != null)
        {
            return sourceValidation;
        }
        
        var sourceContentIdentity = sourceContentIdentities.Single();
        
        var targetInventoryPart = atomicAction.Destination!.GetApplicableInventoryPart();
        var targetContentIdentities = comparisonItem.GetContentIdentities(targetInventoryPart);
        
        var targetValidation =
            ValidateTargetPartForSynchronize(targetInventoryPart, targetContentIdentities, enforceInventoryPartAccessGuard);
        if (targetValidation != null)
        {
            return targetValidation;
        }
        
        var identitiesValidation =
            ValidateSynchronizeIdentities(atomicAction, sourceContentIdentity, targetContentIdentities, targetInventoryPart);
        if (identitiesValidation != null)
        {
            return identitiesValidation;
        }
        
        return AtomicActionValidationResult.Success();
    }
    
    private AtomicActionValidationResult? ValidateSourceForSynchronize(InventoryPart sourceInventoryPart,
        ICollection<ContentIdentity> sourceContentIdentities, bool enforceInventoryPartAccessGuard)
    {
        if (enforceInventoryPartAccessGuard && sourceInventoryPart.IsIncompleteDueToAccess)
        {
            return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.SourceNotAccessible);
        }
        
        if (sourceContentIdentities.Count != 1)
        {
            return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.InvalidSourceCount);
        }
        
        var sourceContentIdentity = sourceContentIdentities.Single();
        
        if (sourceContentIdentity.HasAnalysisError)
        {
            return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.SourceHasAnalysisError);
        }
        
        if (IsAnyFileInaccessible(sourceContentIdentity.GetFileSystemDescriptions(sourceInventoryPart)))
        {
            return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.SourceNotAccessible);
        }
        
        return null;
    }
    
    private AtomicActionValidationResult? ValidateTargetPartForSynchronize(InventoryPart targetInventoryPart,
        ICollection<ContentIdentity> targetContentIdentities, bool enforceInventoryPartAccessGuard)
    {
        if (enforceInventoryPartAccessGuard && targetInventoryPart.IsIncompleteDueToAccess)
        {
            return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.AtLeastOneTargetsNotAccessible);
        }
        
        if (targetContentIdentities.Count == 0 && targetInventoryPart.InventoryPartType == FileSystemTypes.File)
        {
            return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.TargetFileNotPresent);
        }
        
        return null;
    }
    
    private AtomicActionValidationResult? ValidateSynchronizeIdentities(AtomicAction atomicAction,
        ContentIdentity sourceContentIdentity, ICollection<ContentIdentity> targetContentIdentities, InventoryPart targetInventoryPart)
    {
        if (AreContentAndDateIdentical(sourceContentIdentity, targetContentIdentities))
        {
            return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.NothingToCopyContentAndDateIdentical);
        }
        
        if (targetContentIdentities.Count > 0 && targetContentIdentities.Any(t => t.HasAnalysisError))
        {
            return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.AtLeastOneTargetsHasAnalysisError);
        }
        
        if (targetContentIdentities.Count > 0 && targetContentIdentities
                .Any(t => IsAnyFileInaccessible(t.GetFileSystemDescriptions(targetInventoryPart))))
        {
            return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.AtLeastOneTargetsNotAccessible);
        }
        
        if (atomicAction.IsSynchronizeContentOnly && targetContentIdentities.Count != 0 &&
            sourceContentIdentity.Core != null &&
            targetContentIdentities.All(t => t.Core != null && sourceContentIdentity.Core.Equals(t.Core)))
        {
            return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.NothingToCopyContentIdentical);
        }
        
        return null;
    }
    
    private AtomicActionValidationResult ValidateSynchronizeDateOrDelete(AtomicAction atomicAction, ComparisonItem comparisonItem,
        bool enforceInventoryPartAccessGuard)
    {
        var targetInventoryPart = atomicAction.Destination!.GetApplicableInventoryPart();
        var targetContentIdentities = comparisonItem.GetContentIdentities(targetInventoryPart);
        
        var targetValidation =
            ValidateTargetPartForSynchronizeDateOrDelete(targetInventoryPart, targetContentIdentities, enforceInventoryPartAccessGuard);
        if (targetValidation != null)
        {
            return targetValidation;
        }
        
        return AtomicActionValidationResult.Success();
    }
    
    private AtomicActionValidationResult? ValidateTargetPartForSynchronizeDateOrDelete(InventoryPart targetInventoryPart,
        ICollection<ContentIdentity> targetContentIdentities, bool enforceInventoryPartAccessGuard)
    {
        if (enforceInventoryPartAccessGuard && targetInventoryPart.IsIncompleteDueToAccess)
        {
            return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.AtLeastOneTargetsNotAccessible);
        }
        
        if (targetContentIdentities.Count == 0)
        {
            return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.TargetRequiredForSynchronizeDateOrDelete);
        }
        
        if (targetContentIdentities.Any(t => IsAnyFileInaccessible(t.GetFileSystemDescriptions(targetInventoryPart))))
        {
            return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.AtLeastOneTargetsNotAccessible);
        }
        
        return null;
    }
    
    private AtomicActionValidationResult ValidateCreate(AtomicAction atomicAction, ComparisonItem comparisonItem,
        bool enforceInventoryPartAccessGuard)
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
        
        return AtomicActionValidationResult.Success();
    }
    
    private bool ShouldEnforceInventoryPartAccessGuard()
    {
        return _sessionService.CurrentSessionSettings?.MatchingMode == MatchingModes.Flat;
    }
    
    private static bool IsAnyFileInaccessible(IEnumerable<FileSystemDescription> fileSystemDescriptions)
    {
        return fileSystemDescriptions.Any(fsd => fsd is FileDescription && !fsd.IsAccessible);
    }
    
    private static bool AreContentAndDateIdentical(ContentIdentity sourceContentIdentity,
        ICollection<ContentIdentity> targetContentIdentities)
    {
        if (sourceContentIdentity.InventoryPartsByLastWriteTimes.Count != 1
            || targetContentIdentities.Count == 0
            || sourceContentIdentity.Core == null)
        {
            return false;
        }
        
        return targetContentIdentities.All(ci => ci.Core != null && ci.Core.Equals(sourceContentIdentity.Core))
               && targetContentIdentities.All(ci => ci.InventoryPartsByLastWriteTimes.Count == 1
                                                    && ci.InventoryPartsByLastWriteTimes.Keys.Single()
                                                        .Equals(sourceContentIdentity.InventoryPartsByLastWriteTimes.Keys.Single()));
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
        
        var validators = new List<Func<AtomicActionValidationResult?>>()
        {
            () => ValidateDoNothingCompatibility(atomicAction, alreadySetAtomicActions),
            () => ValidateSourceDestinationConflicts(atomicAction, alreadySetAtomicActions),
            () => ValidateDestinationReuse(atomicAction, alreadySetAtomicActions),
            () => ValidateDeleteConflicts(atomicAction, alreadySetAtomicActions),
            () => ValidateExistingDeleteConflicts(atomicAction, alreadySetAtomicActions),
            () => ValidateDuplicateActions(atomicAction, alreadySetAtomicActions)
        };
        
        foreach (var validator in validators)
        {
            var validationResult = validator();
            if (validationResult != null)
            {
                return validationResult;
            }
        }
        
        return AtomicActionValidationResult.Success();
    }
    
    private static AtomicActionValidationResult? ValidateDoNothingCompatibility(AtomicAction atomicAction,
        List<AtomicAction> alreadySetAtomicActions)
    {
        if (!atomicAction.IsTargeted && alreadySetAtomicActions.Any(a => a.IsDoNothing))
        {
            return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason
                .NonTargetedActionNotAllowedWithExistingDoNothingAction);
        }
        
        return null;
    }
    
    private static AtomicActionValidationResult? ValidateSourceDestinationConflicts(AtomicAction atomicAction,
        IEnumerable<AtomicAction> alreadySetAtomicActions)
    {
        if (alreadySetAtomicActions.Any(ma =>
                !atomicAction.IsDelete &&
                Equals(ma.Destination, atomicAction.Source)))
        {
            return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.SourceCannotBeDestinationOfAnotherAction);
        }
        
        if (alreadySetAtomicActions.Any(ma => Equals(ma.Source, atomicAction.Destination)))
        {
            return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.DestinationCannotBeSourceOfAnotherAction);
        }
        
        return null;
    }
    
    private static AtomicActionValidationResult? ValidateDestinationReuse(AtomicAction atomicAction,
        List<AtomicAction> alreadySetAtomicActions)
    {
        if (alreadySetAtomicActions.All(ma => !Equals(ma.Destination, atomicAction.Destination)))
        {
            return null;
        }
        
        if (alreadySetAtomicActions.Count == 1)
        {
            var alreadySetAtomicAction = alreadySetAtomicActions.Single();
            
            if ((!alreadySetAtomicAction.IsSynchronizeDate || !atomicAction.IsSynchronizeContentOnly)
                && (!alreadySetAtomicAction.IsSynchronizeContentOnly || !atomicAction.IsSynchronizeDate))
            {
                return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason
                    .DestinationAlreadyUsedByNonComplementaryAction);
            }
            
            return null;
        }
        
        return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason
            .DestinationAlreadyUsedByNonComplementaryAction);
    }
    
    private static AtomicActionValidationResult? ValidateDeleteConflicts(AtomicAction atomicAction,
        IEnumerable<AtomicAction> alreadySetAtomicActions)
    {
        if (atomicAction.Operator != ActionOperatorTypes.Delete)
        {
            return null;
        }
        
        if (alreadySetAtomicActions.Any(ma =>
                Equals(ma.Destination, atomicAction.Destination) || Equals(ma.Source, atomicAction.Destination)))
        {
            return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.CannotDeleteItemAlreadyUsedInAnotherAction);
        }
        
        return null;
    }
    
    private static AtomicActionValidationResult? ValidateExistingDeleteConflicts(AtomicAction atomicAction,
        IEnumerable<AtomicAction> alreadySetAtomicActions)
    {
        if (alreadySetAtomicActions.Any(ma => ma.Operator == ActionOperatorTypes.Delete &&
                                              (Equals(ma.Destination, atomicAction.Destination) ||
                                               Equals(ma.Destination, atomicAction.Source))))
        {
            return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.CannotOperateOnItemBeingDeleted);
        }
        
        return null;
    }
    
    private static AtomicActionValidationResult? ValidateDuplicateActions(AtomicAction atomicAction,
        IEnumerable<AtomicAction> alreadySetAtomicActions)
    {
        if (atomicAction.Operator != ActionOperatorTypes.DoNothing && alreadySetAtomicActions.Any(s => s.IsSimilarTo(atomicAction)))
        {
            return AtomicActionValidationResult.Failure(AtomicActionValidationFailureReason.DuplicateActionNotAllowed);
        }
        
        return null;
    }
}