using Autofac.Features.Indexed;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Communications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.SignalR;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Communications.PushReceivers;

public class FileTransferPushReceiver : IPushReceiver
{
    private readonly IHubPushHandler2 _hubPushHandler2;
    private readonly IConnectionService _connectionService;
    private readonly ISessionService _sessionService;
    private readonly IDownloadManager _downloadManager;
    private readonly ILogger<FileTransferPushReceiver> _logger;
    private readonly IIndex<SharedFileTypes, IAfterTransferSharedFile?> _operations;

    public FileTransferPushReceiver(IHubPushHandler2 hubPushHandler2, IConnectionService connectionService,
        ISessionService sessionService, IDownloadManager downloadManager, IIndex<SharedFileTypes, IAfterTransferSharedFile?> operations, 
        ILogger<FileTransferPushReceiver> logger)
    {
        _hubPushHandler2 = hubPushHandler2;
        _connectionService = connectionService;
        _sessionService = sessionService;
        _downloadManager = downloadManager;
        _operations = operations;
        _logger = logger;
        
        _hubPushHandler2.FilePartUploaded.Subscribe(OnFilePartUploaded);
        _hubPushHandler2.UploadFinished.Subscribe(OnUploadFinished);
    }

    private async void OnFilePartUploaded(FileTransferPush fileTransferPush)
    {
        var sharedFileDefinition = fileTransferPush.SharedFileDefinition;
        
        _operations.TryGetValue(sharedFileDefinition.SharedFileType, out var afterTransferSharedFile);
        
        if (fileTransferPush.SharedFileDefinition.ClientInstanceId == _connectionService.ClientInstanceId)
        {
            // may be handled in a better way?
            return;
        }

        try
        {
            if (_sessionService.CurrentSession?.SessionId == fileTransferPush.SessionId)
            {
                if (afterTransferSharedFile != null)
                {
                    await afterTransferSharedFile.OnFilePartUploaded(sharedFileDefinition);
                }

                // Ensure the download is started so the coordinator is registered
                await _downloadManager.FileDownloaderCache.GetFileDownloader(sharedFileDefinition);

                await _downloadManager.OnFilePartReadyToDownload(sharedFileDefinition, fileTransferPush.PartNumber!.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CloudSessionManager.OnFilePartUploaded sharedFileDefinition.Id : {id}", fileTransferPush.SharedFileDefinition.Id);

            if (afterTransferSharedFile != null)
            {
                await afterTransferSharedFile.OnFilePartUploadedError(sharedFileDefinition, ex);
            }
        }
    }

    private async void OnUploadFinished(FileTransferPush fileTransferPush)
    {
        var sharedFileDefinition = fileTransferPush.SharedFileDefinition;
        IAfterTransferSharedFile? afterTransferSharedFile;
        _operations.TryGetValue(sharedFileDefinition.SharedFileType, out afterTransferSharedFile);
        
        if (fileTransferPush.SharedFileDefinition.ClientInstanceId == _connectionService.ClientInstanceId)
        {
            // may be handled in a better way?
            return;
        }

        try
        {
            _logger.LogInformation("OnUploadFinished: {SharedFileId} ({SharedFileType})", 
                fileTransferPush.SharedFileDefinition.Id,
                fileTransferPush.SharedFileDefinition.SharedFileType);
            
            if (_sessionService.CurrentSession?.SessionId == fileTransferPush.SessionId)
            {
                if (afterTransferSharedFile != null)
                {
                    await afterTransferSharedFile.OnUploadFinished(sharedFileDefinition);
                }

                // Ensure the download is started so the coordinator is registered
                await _downloadManager.FileDownloaderCache.GetFileDownloader(sharedFileDefinition);

                await _downloadManager.OnFileReadyToFinalize(sharedFileDefinition, fileTransferPush.TotalParts!.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CloudSessionManager.OnUploadFinished sharedFileDefinition.Id :{id}, uploadedBy:{UploaderClientInstanceId} ", 
                fileTransferPush.SharedFileDefinition.Id, fileTransferPush.SharedFileDefinition.ClientInstanceId);

            if (afterTransferSharedFile != null)
            {
                try
                {
                    await afterTransferSharedFile.OnUploadFinishedError(sharedFileDefinition, ex);
                }
                catch (Exception ex2)
                {
                    _logger.LogError(ex2, "CloudSessionManager.OnUploadFinishedError sharedFileDefinition.Id :{id}, uploadedBy:{UploaderClientInstanceId} ", 
                        fileTransferPush.SharedFileDefinition.Id, fileTransferPush.SharedFileDefinition.ClientInstanceId);
                }
            }
        }
    }
}