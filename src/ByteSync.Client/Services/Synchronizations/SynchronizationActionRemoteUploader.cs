using System.IO;
using System.Threading;
using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Synchronizations;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Synchronizations;

public class SynchronizationActionRemoteUploader : ISynchronizationActionRemoteUploader
{
    private readonly ICloudProxy _connectionManager;
    private readonly IDeltaManager _deltaManager;
    private readonly ISessionService _sessionService;
    private readonly ISynchronizationActionServerInformer _synchronizationActionServerInformer;
    private readonly IFileUploaderFactory _fileUploaderFactory;
    private readonly ILogger<SynchronizationActionRemoteUploader> _logger;
    
    private MultiUploadZip? _currentMultiUploadZip;
    
    public SynchronizationActionRemoteUploader(ICloudProxy connectionManager, ISessionService sessionService, 
        IDeltaManager deltaManager, ISynchronizationActionServerInformer synchronizationActionServerInformer, IFileUploaderFactory fileUploaderFactory,
        ILogger<SynchronizationActionRemoteUploader> logger)
    {
        _connectionManager = connectionManager;
        _sessionService = sessionService;
        _deltaManager = deltaManager;
        _synchronizationActionServerInformer = synchronizationActionServerInformer;
        _fileUploaderFactory = fileUploaderFactory;
        _logger = logger;
        
        _currentMultiUploadZip = null;
        
        MultiZipPrepareSemaphore = new SemaphoreSlim(2);
        UploadSemaphore = new SemaphoreSlim(2);

        UploadTasks = new List<Task>();
    }

    private List<Task> UploadTasks { get; }

    private SemaphoreSlim MultiZipPrepareSemaphore { get; }
    
    private SemaphoreSlim UploadSemaphore { get; }
    
    private ByteSyncEndpoint CurrentEndPoint => _connectionManager.CurrentEndPoint;

    public async Task UploadForRemote(SharedActionsGroup sharedActionsGroup)
    {
        string? localFullName = null;
        string? uploadFullName;
        SharedFileTypes sharedFileType;
        string? deltaFullName = null;
        
        try
        {
            if (sharedActionsGroup.SynchronizationType == SynchronizationTypes.Full)
            {
                localFullName = sharedActionsGroup.GetSourceFullName();
                uploadFullName = sharedActionsGroup.GetSourceFullName();

                sharedFileType = SharedFileTypes.FullSynchronization;
            }
            else
            {
                var target = sharedActionsGroup.Targets.First();
                localFullName = sharedActionsGroup.GetSourceFullName();

                uploadFullName = await _deltaManager.BuildDelta(sharedActionsGroup, target, localFullName);
                deltaFullName = uploadFullName;

                sharedFileType = SharedFileTypes.DeltaSynchronization;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Type:l}: an error occurred while starting {logSourceFileName} upload process, " +
                             "ActionsGroupId:{ActionsGroupId}",
                $"Synchronization.{sharedActionsGroup.Operator}", localFullName, sharedActionsGroup.ActionsGroupId);

            throw;
        }

        var fileInfo = new FileInfo(uploadFullName);

        if (_currentMultiUploadZip != null &&
            (!sharedActionsGroup.Key.Equals(_currentMultiUploadZip.Key) ||
             (IsFileUploadableWithMultiUpload(fileInfo) && !_currentMultiUploadZip.CanAdd(fileInfo, sharedActionsGroup.ActionsGroupId)) ||
             _currentMultiUploadZip.CreationDate.IsOlderThan(TimeSpan.FromSeconds(15))))
        {
            try
            {
                await CloseAndUploadCurrentMultiZip();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CloseAndUploadCurrentMultiZip");
            }
        }

        if (IsFileUploadableWithMultiUpload(fileInfo))
        {
            if (_currentMultiUploadZip == null)
            {
                await MultiZipPrepareSemaphore.WaitAsync();

                // on crée le currentMultiUploadZip
                var sharedFileDefinition = BuildSharedFileDefinition(sharedFileType);

                _logger.LogInformation("Creating MultiUploadZip with id:{MultiZipId} for grouped upload (delta:{isDelta})",
                    sharedFileDefinition.Id, deltaFullName != null);
                _currentMultiUploadZip = new MultiUploadZip(sharedActionsGroup.Key, sharedFileDefinition);
            }

            try
            {
                _currentMultiUploadZip.AddEntry(fileInfo, sharedActionsGroup.ActionsGroupId);
            }
            catch (Exception ex)
            {
                await _synchronizationActionServerInformer.HandleCloudActionError(sharedActionsGroup);

                _logger.LogError(ex, "Can not add {File} to MultiUploadZip {MultiZipId}", fileInfo.FullName,
                    _currentMultiUploadZip.SharedFileDefinition.Id);
            }
            finally
            {
                DeleteDeltaFile(deltaFullName);
            }
        }
        else
        {
            var sharedFileDefinition = BuildSharedFileDefinition(sharedFileType);
            
            sharedFileDefinition.ActionsGroupIds = [sharedActionsGroup.ActionsGroupId];
            
            _logger.LogInformation("{Type:l}: uploading {sourceFile} (delta:{isDelta})",
                $"Synchronization.{sharedActionsGroup.Operator}", localFullName, deltaFullName != null);

            var fileUploader = _fileUploaderFactory.Build(uploadFullName, sharedFileDefinition);
            var uploadTask = RunUploadTask(sharedActionsGroup, fileUploader,
                () => DeleteDeltaFile(deltaFullName));
            
            UploadTasks.Add(uploadTask);
        }
    }

    private void DeleteDeltaFile(string? deltaFullName)
    {
        if (deltaFullName != null)
        {
            _logger.LogInformation("Deleting delta file {delta}", deltaFullName);
            File.Delete(deltaFullName);
        }
    }

    public async Task Complete()
    {
        try
        {
            if (_currentMultiUploadZip != null)
            {
                await CloseAndUploadCurrentMultiZip();
            }

            await Task.WhenAll(UploadTasks);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during Complete");
        }
    }

    public Task Abort()
    {
        return Task.Run(() =>
        {
            // L'abandon de la synchro a été demandé, on doit libérer les ressources
            if (_currentMultiUploadZip != null)
            {
                try
                {
                    _currentMultiUploadZip!.CloseZip();
                    _currentMultiUploadZip?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "An error occurred while disposing currentMultiUploadZip");
                }
                finally
                {
                    _currentMultiUploadZip = null;
                }
            }
        });
    }

    private bool IsFileUploadableWithMultiUpload(FileInfo fileInfo)
    {
        return fileInfo.Length <= 200 * SizeConstants.ONE_KILO_BYTES;
    }

    private async Task CloseAndUploadCurrentMultiZip()
    {
        var canDisposeInFinallyBlock = true;
        try
        {
            if (_currentMultiUploadZip == null || _currentMultiUploadZip.ActionGroupsIds.Count == 0)
            {
                _currentMultiUploadZip?.CloseZip();
                return;
            }

            _logger.LogInformation("Closing and uploading MultiUploadZip with id:{MultiZipId}. It contains {FilesFullNames}",
                _currentMultiUploadZip.SharedFileDefinition.Id, _currentMultiUploadZip.FilesFullNames);

            _currentMultiUploadZip.CloseZip();

            _currentMultiUploadZip!.SharedFileDefinition.ActionsGroupIds = _currentMultiUploadZip.ActionGroupsIds;

            canDisposeInFinallyBlock = false;
            
            var currentMultiUploadZip = _currentMultiUploadZip;
            var fileUploader = _fileUploaderFactory.Build(currentMultiUploadZip.MemoryStream, currentMultiUploadZip.SharedFileDefinition);
            var uploadTask = RunUploadTask(currentMultiUploadZip.ActionGroupsIds, fileUploader, 
                () => currentMultiUploadZip.Dispose());
            UploadTasks.Add(uploadTask);

            _currentMultiUploadZip = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during CloseAndUploadCurrentMultiZip");
            if (_currentMultiUploadZip != null)
            {
                await _synchronizationActionServerInformer.HandleCloudActionError(_currentMultiUploadZip.ActionGroupsIds);
            }
        }
        finally
        {
            if (canDisposeInFinallyBlock)
            {
                _currentMultiUploadZip?.Dispose();
                _currentMultiUploadZip = null;
            }

            MultiZipPrepareSemaphore.Release();
        }
    }

    private async Task RunUploadTask(SharedActionsGroup sharedActionsGroup, IFileUploader fileUploader, Action? postAction = null)
    {
        await RunUploadTask([sharedActionsGroup.ActionsGroupId], fileUploader, postAction);
    }

    private async Task RunUploadTask(List<string> actionsGroupsIds, IFileUploader fileUploader, Action? postAction = null)
    {
        try
        {
            await UploadSemaphore.WaitAsync();

            await fileUploader.Upload();
        }
        catch (Exception ex)
        {
            await _synchronizationActionServerInformer.HandleCloudActionError(actionsGroupsIds);
            
            _logger.LogError(ex, "CloseAndUploadCurrentMultiZip.Upload");
        }
        finally
        {
            UploadSemaphore.Release();

            postAction?.Invoke();
        }
    }

    private SharedFileDefinition BuildSharedFileDefinition(SharedFileTypes sharedFileType)
    {
        var sharedFileDefinition = new SharedFileDefinition();

        sharedFileDefinition.ClientInstanceId = CurrentEndPoint.ClientInstanceId;
        sharedFileDefinition.SessionId = _sessionService.SessionId!;
        sharedFileDefinition.SharedFileType = sharedFileType;

        return sharedFileDefinition;
    }
}