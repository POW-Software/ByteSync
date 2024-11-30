using System.Threading.Tasks;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;

namespace ByteSync.Services.Communications.Transfers;

public class DownloadManager : IDownloadManager
{
    private readonly IFileDownloaderCache _fileDownloaderCache;
    private readonly ISynchronizationDownloadFinalizer _synchronizationDownloadFinalizer;
    private readonly IPostDownloadHandlerProxy _postDownloadHandlerProxy;
    private readonly IFileTransferApiClient _fileTransferApiClient;
    private readonly ILogger<DownloadManager> _logger;



    public DownloadManager(IFileDownloaderCache fileDownloaderCache, ISynchronizationDownloadFinalizer synchronizationDownloadFinalizer, 
        IPostDownloadHandlerProxy postDownloadHandlerProxy, IFileTransferApiClient fileTransferApiClient, ILogger<DownloadManager> logger)
    {
        _fileDownloaderCache = fileDownloaderCache;
        _synchronizationDownloadFinalizer = synchronizationDownloadFinalizer;
        _postDownloadHandlerProxy = postDownloadHandlerProxy;
        _fileTransferApiClient = fileTransferApiClient;
        _logger = logger;
    }

    public async Task OnFilePartReadyToDownload(SharedFileDefinition sharedFileDefinition, int partNumber)
    {
        var fileDownloader = await _fileDownloaderCache.GetFileDownloader(sharedFileDefinition); // GetOrBuild(sharedFileDefinition);

        if (partNumber == 1)
        {
            _logger.LogInformation("{Type}: {SharedFileDefinitionId} - Download started to {downloadDestinations}", sharedFileDefinition.SharedFileType, 
                sharedFileDefinition.Id, fileDownloader.DownloadTarget.DownloadDestinations.JoinToString(", "));
        }

        await fileDownloader.AddAvailablePartAsync(partNumber);
    }

    public async Task OnFileReadyToFinalize(SharedFileDefinition sharedFileDefinition, int partsCount)
    {
        var fileDownloader = await _fileDownloaderCache.GetFileDownloader(sharedFileDefinition);
            
        var downloadTarget = fileDownloader.DownloadTarget;

        try
        {
            await fileDownloader.SetAllAvailablePartsKnownAsync(partsCount);

            await fileDownloader.WaitForFileFullyExtracted();

            if (downloadTarget.TemporaryFileManagers != null)
            {
                foreach (var tempFileHelper in downloadTarget.TemporaryFileManagers)
                {
                    tempFileHelper.ValidateTemporaryFile();
                }
            }
        }
        catch (Exception ex)
        {
            if (downloadTarget.TemporaryFileManagers != null)
            {
                foreach (var tempFileHelper in downloadTarget.TemporaryFileManagers)
                {
                    tempFileHelper.TryRevertOnError(ex);
                }
            }

            throw;
        }

        if (sharedFileDefinition.IsSynchronization)
        {
            await _synchronizationDownloadFinalizer.FinalizeSynchronization(sharedFileDefinition, downloadTarget);
        }

        _logger.LogInformation("{Type}: {SharedFileDefinitionId} - Download complete", sharedFileDefinition.SharedFileType, sharedFileDefinition.Id);

        var transferParameters = new TransferParameters
        {
            SessionId = sharedFileDefinition.SessionId,
            SharedFileDefinition = sharedFileDefinition
        };
        await _fileTransferApiClient.AssertDownloadIsFinished(transferParameters);

        await _fileDownloaderCache.RemoveFileDownloader(fileDownloader);

        await _postDownloadHandlerProxy.HandleDownloadFinished(downloadTarget.LocalSharedFile);
    }
}