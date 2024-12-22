using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Helpers;
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
            // Si une des actions est une doNothing, on n'utilisera que celle là
            appliableActions.Add(doNothingAction);
        }
        else
        {
            // Sinon, on regarde un par une
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
                // Ces opérateurs ne peuvent pas s'appliquer à un Directory
                return false;
            }
                
            // Sur une copie de contenu et/ou de date, Source et Destination doivent être définis
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
            // Ne rien faire: toujours OK
            return true;
        }
        else if (atomicAction.Operator == ActionOperatorTypes.Delete)
        {
            // Sur une suppression, Source doit toujours être nulle, Destination doit toujours être définie
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
                // Cet opérateur ne peut pas s'appliquer à un File
                return false;
            }
                
            // Sur une création, Source doit toujours être nulle, Destination doit toujours être définie
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
                    // Pas de source ou trop de sources !
                    return false;
                }
                var contentIdentitySource = contentIdentitiesSources.Single();
                
                if (contentIdentitySource.HasAnalysisError)
                {
                    // Si la source est en erreur d'analyse, on ne peut pas
                    return false;
                }
                    
                    
                var targetInventoryPart = atomicAction.Destination.GetApplicableInventoryPart();
                var contentIdentityViewsTargets = comparisonItem.GetContentIdentities(targetInventoryPart);

                // On ne peut pas envoyer sur un InventoryPartTypes.File qui n'est pas présent
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
                    // Dans ce cas, le contenu est le même et il n'y a qu'une seule date => il n'y a rien à copier
                    return false;
                }

                if (contentIdentityViewsTargets.Count > 0 && contentIdentityViewsTargets.All(t => t.HasAnalysisError))
                {
                    // Si toutes les targets sont en erreur d'analyse, on ne peut pas
                    return false;
                }
                        
                    
                // Si CopyContentOnly et pas de cible ou si une cible dont contenu différent, c'est OK
                // On inverse la condition
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
                // Pas de destination, interdit sur une date ou une suppression 
                return false;
            }
        }
            
        if (atomicAction.IsCreate)
        {
            var targetInventoryPart = atomicAction.Destination.GetApplicableInventoryPart();
                
            // On ne peut rien faire sur une target de type InventoryPartTypes.File
            if (targetInventoryPart.InventoryPartType == FileSystemTypes.File)
            {
                return false;
            }
                
            var contentIdentitiesTargets = comparisonItem.GetContentIdentities(targetInventoryPart);

            if (contentIdentitiesTargets.Count != 0)
            {
                // Il y a une destination, interdit sur une création de répertoire
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
            
            // // 14/02/2023 : Une targetedAction est prioritaire (et écrase) sur les SynchronizationRules
            // // Du coup, on ne considère que que les targetedActions déjà en place // ou les actions similaires
            // alreadySetAtomicActions = comparisonItemViewModel.TD_SynchronizationActions
            //     .Select(sa => sa.AtomicAction)
            //     //.Where(a => a.IsTargeted || a.IsSimilarTo(atomicAction))
            //     .Where(a => a.IsTargeted)
            //     .ToList();
        }
        // else
        // {
        //     alreadySetAtomicActions = comparisonItemViewModel.TD_SynchronizationActions.Select(sa => sa.AtomicAction).ToList();
        // }

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
            // Si l'action n'est pas Targeted et qu'une action est déjà en DoNothing, on ne peut pas l'enregistrer
            return false;
        }

        // Une source ne pas être destination
        // On ne peut pas être destination plusieurs fois, mais on peut être source plusieurs fois
        // On ne peut pas être supprimé si on est source ou destination d'une autre action
        if (alreadySetAtomicActions.Any(ma =>
                !atomicAction.IsDelete && // 16/02/2023: A quoi sert ce IsDelete ?
                Equals(ma.Destination, atomicAction.Source)))
        {
            // Une source ne pas être destination d'une autre action déjà enregistrée
            return false;
        }
            
        // 16/02/2023: Règle commentée car en doublon avec la règle ci dessous
        // if (alreadySetAtomicActions.Any(ma =>
        //         !atomicAction.IsDelete &&
        //         Equals(ma.Source, atomicAction.Destination)))
        // {
        //     // Une source ne pas être destination d'une autre action déjà enregistrée
        //     return false;
        // }
            
        if (alreadySetAtomicActions.Any(ma => Equals(ma.Source, atomicAction.Destination)))
        {
            // Une destination ne peut pas être source d'une autre action déjà enregistrée
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
                    // On peut être destination plusieurs fois si une est en IsSynchronizeDate et l'autre en IsSynchronizeContentOnly
                    // Car complémentaire
                    
                    // Dans le cas contraire, c'est pas OK
                    return false;
                }
            }
            else
            {
                // On ne peut pas être destination plusieurs fois => Une destination ne peut être destination d'une autre action déjà enregistrée
                return false;
            }
        }

        if (atomicAction.Operator == ActionOperatorTypes.Delete)
        {
            if (alreadySetAtomicActions.Any(ma => 
                    Equals(ma.Destination, atomicAction.Destination) || Equals(ma.Source, atomicAction.Destination)))
            {
                // Impossible d'enregistrer l'opération de suppression si la destination est déjà source ou destination d'une autre action
                return false;
            }
        }
            
        if (alreadySetAtomicActions.Any(ma => ma.Operator == ActionOperatorTypes.Delete &&
                (Equals(ma.Destination, atomicAction.Destination) || Equals(ma.Destination, atomicAction.Source))))
        {
            // Impossible d'enregistrer une opération si la Source ou la Destination sont destination d'une suppression
            return false;
        }

        if (atomicAction.Operator != ActionOperatorTypes.DoNothing && alreadySetAtomicActions.Any(s => s.IsSimilarTo(atomicAction)))
        {
            // Impossible d'enregistrer un doublon d'une action déjà enregistrée
            return false;
        }

        return true;
    }
}