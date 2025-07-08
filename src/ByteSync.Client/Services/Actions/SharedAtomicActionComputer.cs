using System.Threading.Tasks;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Comparisons;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces.Controls.Actions;
using ByteSync.Interfaces.Repositories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.FileSystems;

namespace ByteSync.Services.Actions;

public class SharedAtomicActionComputer : ISharedAtomicActionComputer
{
    private readonly IAtomicActionRepository _atomicActionRepository;
    private readonly ISharedAtomicActionRepository _sharedAtomicActionRepository;

    public SharedAtomicActionComputer(IAtomicActionRepository atomicActionRepository,
        ISharedAtomicActionRepository sharedAtomicActionRepository)
    {
        _atomicActionRepository = atomicActionRepository;
        _sharedAtomicActionRepository = sharedAtomicActionRepository;
    }

    public Task<List<SharedAtomicAction>> ComputeSharedAtomicActions()
    {
        var atomicActions = _atomicActionRepository.Elements;
            
        foreach (var atomicAction in atomicActions.Where(a => !a.IsDoNothing))
        {
            CreateSharedAtomicActions(atomicAction, atomicAction.Destination, atomicAction.Source);
        }

        return Task.FromResult(_sharedAtomicActionRepository.Elements.ToList());
    }

    private void CreateSharedAtomicActions(AtomicAction atomicAction, DataPart? target, DataPart? source)
    {
        SharedDataPart? sourceSharedDataPart = null;
        HashSet<SharedDataPart>? targetSharedDataParts = null;

        SynchronizationTypes? synchronizationType = null;
        long? size = null;
        DateTime? lastWriteTimeUtc = null;
        DateTime? creationTimeUtc = null;

        if (atomicAction.IsDelete)
        {
            var targetContentIdentities = atomicAction.ComparisonItem!.GetContentIdentities(target!.GetApplicableInventoryPart());

            targetSharedDataParts = new HashSet<SharedDataPart>();
            foreach (var contentIdentity in targetContentIdentities)
            {
                targetSharedDataParts.AddAll(BuildSharedDataPart(target, contentIdentity));
            }
        }

        if (atomicAction.IsCreate)
        {
            SharedDataPart targetSharedDataPart = DoBuildSharedDataPart(target!, atomicAction.PathIdentity!.LinkingKeyValue);
                    
            targetSharedDataParts = new HashSet<SharedDataPart> { targetSharedDataPart };
        }
        else if (atomicAction.IsSynchronizeContent || atomicAction.IsSynchronizeDate)
        {
            var sourceContentIdentities = atomicAction.ComparisonItem!.GetContentIdentities(source!.GetApplicableInventoryPart());
            var targetContentIdentities = atomicAction.ComparisonItem!.GetContentIdentities(target!.GetApplicableInventoryPart());

            if (sourceContentIdentities.Count != 1)
            {
                throw new ApplicationException("sourceContentIdentityViews.Count != 1 -- " + sourceContentIdentities.Count);
            }

            var sourceContentIdentity = sourceContentIdentities.Single();

            size = sourceContentIdentity.Core?.Size;
            lastWriteTimeUtc = sourceContentIdentity.GetLastWriteTimeUtc(source.GetApplicableInventoryPart());
            creationTimeUtc = sourceContentIdentity.GetCreationTimeUtc(source.GetApplicableInventoryPart());
                
            if (targetContentIdentities.Count > 0)
            {
                synchronizationType = SynchronizationTypes.Delta;

                sourceSharedDataPart = BuildSharedDataPart(source, sourceContentIdentity).First();
                    
                targetSharedDataParts = new HashSet<SharedDataPart>();
                foreach (var targetContentIdentity in targetContentIdentities)
                {
                    if (!Equals(targetContentIdentity.Core?.SignatureHash,
                            sourceContentIdentity.Core?.SignatureHash) ||
                        targetContentIdentity.Core?.Size != size ||
                        targetContentIdentity.GetLastWriteTimeUtc(target.GetApplicableInventoryPart()) != lastWriteTimeUtc)
                    {
                        targetSharedDataParts.AddAll(BuildSharedDataPart(target, targetContentIdentity));
                    }
                }
            }
            else
            {
                synchronizationType = SynchronizationTypes.Full;

                sourceSharedDataPart = BuildSharedDataPart(source, sourceContentIdentity).First();

                SharedDataPart targetSharedDataPart = DoBuildSharedDataPart(target, 
                    sourceSharedDataPart.RelativePath);
                    
                targetSharedDataParts = new HashSet<SharedDataPart> { targetSharedDataPart };
            }
        }

        if (targetSharedDataParts == null || targetSharedDataParts.Count == 0)
        {
            CreateSharedAtomicAction(atomicAction.AtomicActionId, sourceSharedDataPart, null, 
                atomicAction.Operator, atomicAction.PathIdentity!, synchronizationType, 
                size, lastWriteTimeUtc, creationTimeUtc, atomicAction.IsFromSynchronizationRule);
        }
        else
        {
            foreach (SharedDataPart targetSharedDataPart in targetSharedDataParts)
            {
                CreateSharedAtomicAction(atomicAction.AtomicActionId, sourceSharedDataPart, targetSharedDataPart, 
                    atomicAction.Operator, atomicAction.PathIdentity!, synchronizationType, 
                    size, lastWriteTimeUtc, creationTimeUtc, atomicAction.IsFromSynchronizationRule);
            }
        }
    }

    private void CreateSharedAtomicAction(string atomicActionId, SharedDataPart? sourceSharedDataPart, 
        SharedDataPart? targetSharedDataPart, ActionOperatorTypes operatorType, PathIdentity pathIdentity, 
        SynchronizationTypes? synchronizationType, long? size, DateTime? lastWriteTimeUtc, DateTime? creationTimeUtc,
        bool isFromSynchronizationRule)
    {
        var sharedAtomicAction = new SharedAtomicAction(atomicActionId);

        sharedAtomicAction.Operator = operatorType;
        sharedAtomicAction.PathIdentity = pathIdentity;

        sharedAtomicAction.Source = sourceSharedDataPart;
        sharedAtomicAction.Target = targetSharedDataPart;

        sharedAtomicAction.SynchronizationType = synchronizationType;
        sharedAtomicAction.Size = size;
        sharedAtomicAction.CreationTimeUtc = creationTimeUtc;
        sharedAtomicAction.LastWriteTimeUtc = lastWriteTimeUtc;

        sharedAtomicAction.IsFromSynchronizationRule = isFromSynchronizationRule;
        
        _sharedAtomicActionRepository.AddOrUpdate(sharedAtomicAction);
    }

    private HashSet<SharedDataPart> BuildSharedDataPart(DataPart dataPart, ContentIdentity contentIdentity)
    {
        var result = new HashSet<SharedDataPart>();
            
        var fileSystemDescriptions = contentIdentity.GetFileSystemDescriptions(dataPart.GetApplicableInventoryPart());

        foreach (var fileSystemDescription in fileSystemDescriptions)
        {
            // var signatureInfo = BuildSignatureInfo(fileDescription);

            string? signatureGuid = null;
            string? signatureHash = null;
            bool hasAnalysisError = false;

            if (fileSystemDescription is FileDescription fileDescription)
            {
                signatureGuid = fileDescription.SignatureGuid;
                signatureHash = contentIdentity.Core?.SignatureHash;
                hasAnalysisError = fileDescription.HasAnalysisError;
            }
                    
            SharedDataPart sharedDataPart = DoBuildSharedDataPart(dataPart, 
                fileSystemDescription.RelativePath, signatureGuid, signatureHash, hasAnalysisError);

            result.Add(sharedDataPart);
        }

        return result;
    }
        
    private SharedDataPart DoBuildSharedDataPart(DataPart dataPart, string? relativePath = null, string? signatureGuid = null, 
        string? signatureHash = null, bool hasAnalysisError = false)
    {
        var inventory = dataPart.GetAppliableInventory();
        var inventoryPart = dataPart.GetApplicableInventoryPart();
            
        SharedDataPart sharedDataPart = new SharedDataPart(
            dataPart.Name,
            dataPart.GetApplicableInventoryPart().InventoryPartType,
            inventory.Endpoint.ClientInstanceId,
            inventory.Code,
            inventoryPart.RootPath,
            relativePath,
            signatureGuid, signatureHash, hasAnalysisError);

        return sharedDataPart;
    }
}