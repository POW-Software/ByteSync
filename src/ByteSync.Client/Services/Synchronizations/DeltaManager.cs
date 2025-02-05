using System.IO;
using ByteSync.Business;
using ByteSync.Business.Actions.Shared;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Factories;
using ByteSync.Services.Inventories;
using FastRsync.Core;
using FastRsync.Delta;
using FastRsync.Signature;

namespace ByteSync.Services.Synchronizations;

public class DeltaManager : IDeltaManager
{
    private readonly ICloudSessionLocalDataManager _cloudSessionLocalDataManager;
    private readonly ITemporaryFileManagerFactory _temporaryFileManagerFactory;
    private readonly ILogger<DeltaManager> _logger;

    public DeltaManager(ICloudSessionLocalDataManager cloudSessionLocalDataManager, ITemporaryFileManagerFactory temporaryFileManagerFactory,
        ILogger<DeltaManager> logger)
    {
        _cloudSessionLocalDataManager = cloudSessionLocalDataManager;
        _temporaryFileManagerFactory = temporaryFileManagerFactory;
        _logger = logger;
    }
        
    public async Task<string> BuildDelta(SharedActionsGroup sharedActionsGroup, SharedDataPart sharedDataPart, string sourceFullName)
    {
        var deltaFullName = _cloudSessionLocalDataManager.GetTempDeltaFullName(sharedActionsGroup.Source!, sharedDataPart);
            
        var targetInventoryPath = _cloudSessionLocalDataManager.GetInventoryPath(sharedDataPart.ClientInstanceId, 
            sharedDataPart.InventoryLetter, LocalInventoryModes.Full);

        using var targetInventoryLoader = new InventoryLoader(targetInventoryPath);
        
        var deltaBuilder = new DeltaBuilder();

        await using var targetMemoryStream = targetInventoryLoader.GetSignature(sharedDataPart.SignatureGuid!);
        await using var newFileStream = new FileStream(sourceFullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        await using var deltaStream = new FileStream(deltaFullName, FileMode.Create, FileAccess.Write, FileShare.Read);

        await deltaBuilder.BuildDeltaAsync(newFileStream,
            new SignatureReader(targetMemoryStream, deltaBuilder.ProgressReport),
            new AggregateCopyOperationsDecorator(new BinaryDeltaWriter(deltaStream)));
        
        return deltaFullName;
    }
    
    public async Task ApplyDelta(string destinationFullName, string deltaFullName)
    {
        await using var deltaStream = new FileStream(deltaFullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        await ApplyDelta(destinationFullName, deltaStream);
    }

    public async Task ApplyDelta(string destinationFullName, Stream deltaStream)
    {
        var deltaApplier = new DeltaApplier
        {
            SkipHashCheck = false
        };
        
        var temporaryFileManager = _temporaryFileManagerFactory.Create(destinationFullName);
        var destinationTemporaryPath = temporaryFileManager.GetDestinationTemporaryPath();
        
        _logger.LogInformation("DeltaManager: Applying delta on {finalDestinationPath}", destinationFullName);

        try
        {
            // This block is necessary, otherwise the disposals do not occur and the validate temporary file crashes
            {
                await using var basisStream = new FileStream(destinationFullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                await using var newFileStream = new FileStream(destinationTemporaryPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
                await deltaApplier.ApplyAsync(basisStream, new BinaryDeltaReader(deltaStream, null), newFileStream);
            }

            temporaryFileManager.ValidateTemporaryFile();
        }
        catch (Exception ex)
        {
            temporaryFileManager.TryRevertOnError(ex);
            throw;
        }
    }
}