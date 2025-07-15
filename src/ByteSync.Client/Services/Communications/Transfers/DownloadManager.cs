using ByteSync.Common.Business.SharedFiles;
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
    private readonly IDictionary<string, IDownloadPartsCoordinator> _partsCoordinatorCache = new Dictionary<string, IDownloadPartsCoordinator>();


    public DownloadManager(IFileDownloaderCache fileDownloaderCache, ISynchronizationDownloadFinalizer synchronizationDownloadFinalizer, 
        IPostDownloadHandlerProxy postDownloadHandlerProxy, IFileTransferApiClient fileTransferApiClient, ILogger<DownloadManager> logger)
    {
        _fileDownloaderCache = fileDownloaderCache;
        _synchronizationDownloadFinalizer = synchronizationDownloadFinalizer;
        _postDownloadHandlerProxy = postDownloadHandlerProxy;
        _fileTransferApiClient = fileTransferApiClient;
        _logger = logger;
        _logger.LogInformation($"DownloadManager instance created. HashCode: {this.GetHashCode()}");
    }

    public async Task OnFilePartReadyToDownload(SharedFileDefinition sharedFileDefinition, int partNumber)
    {
        _logger.LogInformation("Looking up parts coordinator for file Id: {Id} (thread {ThreadId})", sharedFileDefinition.Id, System.Threading.Thread.CurrentThread.ManagedThreadId);
        _logger.LogInformation("Current partsCoordinatorCache keys before lookup: {Keys}", string.Join(", ", _partsCoordinatorCache.Keys));
        if (!_partsCoordinatorCache.TryGetValue(sharedFileDefinition.Id, out var partsCoordinator))
        {
            _logger.LogError("No parts coordinator found for file Id: {Id}", sharedFileDefinition.Id);
            _logger.LogError("Current partsCoordinatorCache keys at error: {Keys}", string.Join(", ", _partsCoordinatorCache.Keys));
            _logger.LogError("StackTrace: {StackTrace}", Environment.StackTrace);
            throw new InvalidOperationException("No parts coordinator found for the given file definition.");
        }

        if (partNumber == 1)
        {
            var fileDownloader = await _fileDownloaderCache.GetFileDownloader(sharedFileDefinition);
            _logger.LogInformation("{Type}: {SharedFileDefinitionId} - Download started to {downloadDestinations}", sharedFileDefinition.SharedFileType, 
                sharedFileDefinition.Id, fileDownloader.DownloadTarget.DownloadDestinations.JoinToString(", "));
        }

        await partsCoordinator.AddAvailablePartAsync(partNumber);
    }

    public async Task OnFileReadyToFinalize(SharedFileDefinition sharedFileDefinition, int partsCount)
    {
        if (!_partsCoordinatorCache.TryGetValue(sharedFileDefinition.Id, out var partsCoordinator))
        {
            throw new InvalidOperationException("No parts coordinator found for the given file definition.");
        }
        var fileDownloader = await _fileDownloaderCache.GetFileDownloader(sharedFileDefinition);
        var downloadTarget = fileDownloader.DownloadTarget;

        try
        {
            await partsCoordinator.SetAllPartsKnownAsync(partsCount);
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
    
    public void RegisterPartsCoordinator(SharedFileDefinition sharedFileDefinition, IDownloadPartsCoordinator coordinator)
    {
        _logger.LogInformation("Registering parts coordinator for file Id: {Id} (thread {ThreadId})", sharedFileDefinition.Id, System.Threading.Thread.CurrentThread.ManagedThreadId);
        _logger.LogInformation("Current partsCoordinatorCache keys before registration: {Keys}", string.Join(", ", _partsCoordinatorCache.Keys));
        _partsCoordinatorCache[sharedFileDefinition.Id] = coordinator;
        _logger.LogInformation("Current partsCoordinatorCache keys after registration: {Keys}", string.Join(", ", _partsCoordinatorCache.Keys));
    }

    public IFileDownloaderCache FileDownloaderCache => _fileDownloaderCache;
}