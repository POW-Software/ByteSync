using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using ByteSync.Business.Communications.Downloading;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Factories;

namespace ByteSync.Services.Communications.Transfers;

public class SynchronizationDownloadFinalizer : ISynchronizationDownloadFinalizer
{
    private readonly IDeltaManager _deltaManager;
    private readonly ITemporaryFileManagerFactory _temporaryFileManagerFactory;
    private readonly IFileDatesSetter _fileDatesSetter;
    private readonly ILogger<SynchronizationDownloadFinalizer> _logger;

    public SynchronizationDownloadFinalizer(IDeltaManager deltaManager, ITemporaryFileManagerFactory temporaryFileManagerFactory,
        IFileDatesSetter fileDatesSetter, ILogger<SynchronizationDownloadFinalizer> logger)
    {
        _deltaManager = deltaManager;
        _temporaryFileManagerFactory = temporaryFileManagerFactory;
        _fileDatesSetter = fileDatesSetter;
        _logger = logger;
    }
    
    public async Task FinalizeSynchronization(SharedFileDefinition sharedFileDefinition, DownloadTarget downloadTarget)
    {
        if (downloadTarget.IsMultiFileZip)
        {
            await HandleSynchronizationZipMultiFile(sharedFileDefinition, downloadTarget);
        }
        else
        {
            await HandleSynchronizationMonoFile(sharedFileDefinition, downloadTarget);
        }
    }
    
    private async Task HandleSynchronizationZipMultiFile(SharedFileDefinition sharedFileDefinition, DownloadTarget downloadTarget)
    {
        var downloadDestination = downloadTarget.DownloadDestinations.Single();
        using (var zipArchive = ZipFile.Open(downloadDestination, ZipArchiveMode.Read))
        {
            foreach (var entry in zipArchive.Entries)
            {
                var finalDestinations = downloadTarget.FinalDestinationsPerActionsGroupId![entry.Name];
                var downloadTargetDates = downloadTarget.GetTargetDates(entry.Name);
            
                if (sharedFileDefinition.IsDeltaSynchronization)
                {
                    // Synchro delta
                    foreach (var finalDestination in finalDestinations)
                    {
                        _logger.LogInformation("{SharedFileDefinitionId}: Extracting and applying delta on {FinalDestination}", 
                            sharedFileDefinition.Id, finalDestination);
                        
                        await using (var stream = entry.Open())
                        {
                            // On fait une copie en memoryStream, sinon, on rencontre des erreurs si on travaille directement sur le "stream"
                            using (var reader = new MemoryStream())
                            {
                                await stream.CopyToAsync(reader);
                            
                                await _deltaManager.ApplyDelta(finalDestination, reader);
                            }
                        }

                        await _fileDatesSetter.SetDates(sharedFileDefinition, finalDestination, downloadTargetDates);
                    }
                }
                else
                {
                    // Synchro full
                    foreach (var finalDestination in finalDestinations)
                    {
                        _logger.LogInformation("{SharedFileDefinitionId}: Extracting to :{FinalDestination}", 
                            sharedFileDefinition.Id, finalDestination);
                        
                        var fileInfo = new FileInfo(finalDestination);
                        if (fileInfo.Directory is { Exists: false })
                        {
                            fileInfo.Directory.Create();
                        }

                        var temporaryFileManager = _temporaryFileManagerFactory.Create(finalDestination);
                        var destinationTemporaryPath = temporaryFileManager.GetDestinationTemporaryPath();
                        
                        try
                        {
                            entry.ExtractToFile(destinationTemporaryPath);
                            temporaryFileManager.ValidateTemporaryFile();
                            await _fileDatesSetter.SetDates(sharedFileDefinition, finalDestination, downloadTargetDates);
                        }
                        catch (Exception ex)
                        {
                            temporaryFileManager.TryRevertOnError(ex);
                            throw;
                        }
                    }
                }
            }
        }
        
        DeleteTemporaryDownloadedFile(downloadDestination);
    }
    
    private async Task HandleSynchronizationMonoFile(SharedFileDefinition sharedFileDefinition, DownloadTarget downloadTarget)
    {
        var actionsGroupId = sharedFileDefinition.ActionsGroupIds!.Single();
            
        var downloadTargetDates = downloadTarget.GetTargetDates(actionsGroupId);

        if (sharedFileDefinition.IsDeltaSynchronization)
        {
            var downloadDestination = downloadTarget.DownloadDestinations.Single();
            
            var deltaFullName = downloadDestination;
            var finalDestinations = downloadTarget.FinalDestinationsPerActionsGroupId![actionsGroupId];
            foreach (var finalDestination in finalDestinations)
            {
                _logger.LogInformation("{SharedFileDefinitionId}: Applying delta on {FinalDestination}", 
                    sharedFileDefinition.Id, finalDestination);
                
                await _deltaManager.ApplyDelta(finalDestination, deltaFullName);

                await _fileDatesSetter.SetDates(sharedFileDefinition, finalDestination, downloadTargetDates);
                // await SetDates(sharedFileDefinition, finalDestination, downloadTargetDates);
            }
            
            DeleteTemporaryDownloadedFile(downloadDestination);
        }
        else
        {
            var tasks = new List<Task>();
            
            foreach (var downloadDestination in downloadTarget.AllFinalDestinations)
            {
                tasks.Add(_fileDatesSetter.SetDates(sharedFileDefinition, downloadDestination, downloadTargetDates));
            }
            
            await Task.WhenAll(tasks);
        }
    }
    
    private void DeleteTemporaryDownloadedFile(string temporaryDownloadedFile)
    {
        _logger.LogInformation("Deleting temporary downloaded file {temporaryDownloadedFile}", temporaryDownloadedFile);
        File.Delete(temporaryDownloadedFile);
    }

    // private Task SetDates(SharedFileDefinition sharedFileDefinition, string fullName, DownloadTargetDates? downloadTargetDates)
    // {
    //     return Task.Run(() =>
    //     {
    //         if (downloadTargetDates != null)
    //         {
    //             _logger.LogInformation("{sharedFileDefinitionId}: Setting CreationTime and LastWriteTime on {FinalDestination}", 
    //                 sharedFileDefinition.Id, fullName);
    //             
    //             File.SetCreationTimeUtc(fullName, downloadTargetDates.CreationTimeUtc);
    //             File.SetLastWriteTimeUtc(fullName, downloadTargetDates.LastWriteTimeUtc);
    //         }
    //         else
    //         {
    //             _logger.LogInformation("{sharedFileDefinitionId}: Setting CreationTime and LastWriteTime on {FinalDestination} to now", 
    //                 sharedFileDefinition.Id, fullName);
    //             
    //             File.SetCreationTimeUtc(fullName, DateTime.UtcNow);
    //             File.SetLastWriteTimeUtc(fullName, DateTime.UtcNow);
    //         }
    //     });
    // }
}