using System.IO;
using System.Threading;
using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Communications.Downloading;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Synchronizations;

public class SynchronizationActionHandler : ISynchronizationActionHandler
{
    private readonly ISessionService _sessionService;
    private readonly IConnectionService _connectionService;
    private readonly IDeltaManager _deltaManager;
    private readonly ISynchronizationActionServerInformer _synchronizationActionServerInformer;
    private readonly ISynchronizationActionRemoteUploader _synchronizationActionRemoteUploader;
    private readonly ISynchronizationService _synchronizationService;
    private readonly ISynchronizationApiClient _synchronizationApiClient;
    private readonly IFileDatesSetter _fileDatesSetter;
    private readonly ILogger<SynchronizationActionHandler> _logger;

    public SynchronizationActionHandler(ISessionService sessionService, IConnectionService connectionService, IDeltaManager deltaManager,
        ISynchronizationActionServerInformer synchronizationActionServerInformer,
        ISynchronizationActionRemoteUploader synchronizationActionRemoteUploader,
        ISynchronizationService synchronizationService, ISynchronizationApiClient synchronizationApiClient,
        IFileDatesSetter fileDatesSetter,
        ILogger<SynchronizationActionHandler> logger)
    {
        _sessionService = sessionService;
        _connectionService = connectionService;
        _deltaManager = deltaManager;
        _synchronizationActionServerInformer = synchronizationActionServerInformer;
        _synchronizationActionRemoteUploader = synchronizationActionRemoteUploader;
        _synchronizationService = synchronizationService;
        _synchronizationApiClient = synchronizationApiClient;
        _fileDatesSetter = fileDatesSetter;
        _logger = logger;
    }

    public ByteSyncEndpoint CurrentEndPoint => _connectionService.CurrentEndPoint!;

    public async Task RunSynchronizationAction(SharedActionsGroup sharedActionsGroup, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (sharedActionsGroup.IsSynchronizeContentOnly || sharedActionsGroup.IsFinallySynchronizeContentAndDate)
            {
                await RunCopyContentSynchronizationAction(sharedActionsGroup, cancellationToken);
            }
            else if (sharedActionsGroup.IsSynchronizeDate || sharedActionsGroup.IsFinallySynchronizeDate)
            {
                await RunCopyDateSynchronizationAction(sharedActionsGroup, cancellationToken);
            }
            else if (sharedActionsGroup.IsDelete)
            {
                await RunDeleteSynchronizationAction(sharedActionsGroup, cancellationToken);
            }
            else if (sharedActionsGroup.IsCreate)
            {
                await RunCreateSynchronizationAction(sharedActionsGroup, cancellationToken);
            }
            else
            {
                throw new ApplicationException("Unknown action operator");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SynchronizationAction cancelled for {ActionsGroupId}", sharedActionsGroup.ActionsGroupId);

            throw;
        }
        catch (Exception)
        {
            await _synchronizationActionServerInformer.HandleCloudActionError(sharedActionsGroup);

            throw;
        }
    }

    public async Task RunPendingSynchronizationActions(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Running pending synchronization actions");

        cancellationToken.ThrowIfCancellationRequested();

        if (_sessionService.CurrentSession is CloudSession)
        {
            if (_synchronizationService.SynchronizationProcessData.SynchronizationAbortRequest.Value == null)
            {
                await _synchronizationActionRemoteUploader.Complete();

                await _synchronizationActionServerInformer.HandlePendingActions();
            }
            else
            {
                await _synchronizationActionRemoteUploader.Abort();

                await _synchronizationActionServerInformer.ClearPendingActions();
            }
        }
    }

    private async Task RunCopyContentSynchronizationAction(SharedActionsGroup sharedActionsGroup, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var localTargets = GetLocalTargets(sharedActionsGroup);
        if (localTargets.Count > 0)
        {
            await RunCopyContentLocal(sharedActionsGroup, localTargets, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var remoteTargets = GetRemoteTargets(sharedActionsGroup);
        if (remoteTargets.Count > 0)
        {
            await _synchronizationActionRemoteUploader.UploadForRemote(sharedActionsGroup);
        }
    }

    private async Task RunCopyContentLocal(SharedActionsGroup sharedActionsGroup, HashSet<SharedDataPart> localTargets,
        CancellationToken cancellationToken)
    {
        var sourceFullName = sharedActionsGroup.GetSourceFullName();

        foreach (var localTarget in localTargets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var destinationFullName = sharedActionsGroup.GetFullName(localTarget);

            long? transferredBytes;
            if (sharedActionsGroup.SynchronizationType == SynchronizationTypes.Full)
            {
                var destinationFileInfo = new FileInfo(destinationFullName);
                if (!destinationFileInfo.Directory!.Exists)
                {
                    _logger.LogInformation("{Type:l}: creating directory {directory}",
                        $"Synchronization.{sharedActionsGroup.Operator}", destinationFileInfo.Directory);

                    destinationFileInfo.Directory.Create();
                }

                _logger.LogInformation("{Type:l}: copying from {source} to {destination}",
                    $"Synchronization.{sharedActionsGroup.Operator}", sourceFullName, destinationFullName);

                // For full copy, the transferred volume equals the file size
                transferredBytes = new FileInfo(sourceFullName).Length;
                File.Copy(sourceFullName, destinationFullName, true);

                await ApplyDatesFromLocalSource(sharedActionsGroup, destinationFullName);
            }
            else
            {
                var deltaFullName = await _deltaManager.BuildDelta(sharedActionsGroup, localTarget, sourceFullName);

                try
                {
                    // For delta copy, the transferred volume equals the delta size
                    transferredBytes = new FileInfo(deltaFullName).Length;
                    await _deltaManager.ApplyDelta(destinationFullName, deltaFullName);

                    await ApplyDatesFromSharedActionsGroup(sharedActionsGroup, destinationFullName);
                }
                finally
                {
                    _logger.LogInformation("Deleting delta file {delta}", deltaFullName);
                    File.Delete(deltaFullName);
                }
            }

            var metrics = new Dictionary<string, SynchronizationActionMetrics>
            {
                [sharedActionsGroup.ActionsGroupId] = new()
                {
                    TransferredBytes = transferredBytes
                }
            };

            await _synchronizationActionServerInformer.HandleCloudActionDone(sharedActionsGroup, localTarget,
                _synchronizationApiClient.AssertLocalCopyIsDone, metrics);
        }
    }

    private async Task ApplyDatesFromLocalSource(SharedActionsGroup sharedActionsGroup, string destinationFullName)
    {
        if (sharedActionsGroup.IsSynchronizeContentOnly)
        {
            await _fileDatesSetter.SetDates(sharedActionsGroup, destinationFullName, null);
        }
        else
        {
            var downloadTargetDates = DownloadTargetDates.FromSharedActionsGroup(sharedActionsGroup);
            await _fileDatesSetter.SetDates(sharedActionsGroup, destinationFullName, downloadTargetDates);
        }
    }

    private async Task ApplyDatesFromSharedActionsGroup(SharedActionsGroup sharedActionsGroup, string destinationFullName)
    {
        DownloadTargetDates? downloadTargetDates = null;

        if (sharedActionsGroup.IsSynchronizeContentAndDate || sharedActionsGroup.IsSynchronizeDate)
        {
            downloadTargetDates = DownloadTargetDates.FromSharedActionsGroup(sharedActionsGroup);
        }

        await _fileDatesSetter.SetDates(sharedActionsGroup, destinationFullName, downloadTargetDates);
    }

    private HashSet<SharedDataPart> GetLocalTargets(SharedActionsGroup sharedActionsGroup)
    {
        var localTargets = new HashSet<SharedDataPart>();

        foreach (var sharedDataPart in sharedActionsGroup.Targets)
        {
            if (sharedDataPart.ClientInstanceId.Equals(CurrentEndPoint.ClientInstanceId))
            {
                localTargets.Add(sharedDataPart);
            }
        }

        return localTargets;
    }

    private HashSet<SharedDataPart> GetRemoteTargets(SharedActionsGroup sharedActionsGroup)
    {
        var remoteTargets = new HashSet<SharedDataPart>();

        foreach (var sharedDataPart in sharedActionsGroup.Targets)
        {
            if (!sharedDataPart.ClientInstanceId.Equals(CurrentEndPoint.ClientInstanceId))
            {
                remoteTargets.Add(sharedDataPart);
            }
        }

        return remoteTargets;
    }

    private async Task RunCopyDateSynchronizationAction(SharedActionsGroup sharedActionsGroup, CancellationToken cancellationToken)
    {
        var localTargets = GetLocalTargets(sharedActionsGroup);

        foreach (var localTarget in localTargets)
        {
            var destinationPath = sharedActionsGroup.GetFullName(localTarget);

            cancellationToken.ThrowIfCancellationRequested();

            if (File.Exists(destinationPath))
            {
                await ApplyDatesFromSharedActionsGroup(sharedActionsGroup, destinationPath);

                await _synchronizationActionServerInformer.HandleCloudActionDone(sharedActionsGroup, localTarget,
                    _synchronizationApiClient.AssertDateIsCopied);
            }
            else
            {
                _logger.LogWarning("{Type:l}: can not apply last write time on {fileInfo}. This file does not exist",
                    $"Synchronization.{sharedActionsGroup.Operator}", destinationPath);

                await _synchronizationActionServerInformer.HandleCloudActionError(sharedActionsGroup, localTarget);
            }
        }
    }

    private async Task RunDeleteSynchronizationAction(SharedActionsGroup sharedActionsGroup, CancellationToken cancellationToken)
    {
        var localTargets = GetLocalTargets(sharedActionsGroup);

        foreach (var localTarget in localTargets)
        {
            var destinationPath = sharedActionsGroup.GetFullName(localTarget);

            cancellationToken.ThrowIfCancellationRequested();

            if (sharedActionsGroup.IsFile && File.Exists(destinationPath))
            {
                var fileInfo = new FileInfo(destinationPath);

                _logger.LogInformation("{Type:l}: deleting {fileInfo}",
                    $"Synchronization.{sharedActionsGroup.Operator}", fileInfo.FullName);
                fileInfo.Delete();
            }
            else if (sharedActionsGroup.IsDirectory && Directory.Exists(destinationPath))
            {
                var directoryInfo = new DirectoryInfo(destinationPath);

                _logger.LogInformation("{Type:l}: deleting {fileInfo}",
                    $"Synchronization.{sharedActionsGroup.Operator}", directoryInfo.FullName);

                var subFilesCount = directoryInfo.GetFiles("*", SearchOption.AllDirectories).Length;
                directoryInfo.Delete(subFilesCount == 0);
            }
            else
            {
                _logger.LogWarning("{Type:l}: {fileInfo} is already missing, will not try to delete it",
                    $"Synchronization.{sharedActionsGroup.Operator}", destinationPath);
            }

            await _synchronizationActionServerInformer.HandleCloudActionDone(sharedActionsGroup, localTarget,
                _synchronizationApiClient.AssertFileOrDirectoryIsDeleted);
        }
    }

    private async Task RunCreateSynchronizationAction(SharedActionsGroup sharedActionsGroup, CancellationToken cancellationToken)
    {
        var localTargets = GetLocalTargets(sharedActionsGroup);

        foreach (var localTarget in localTargets)
        {
            var destinationPath = sharedActionsGroup.GetFullName(localTarget);
            cancellationToken.ThrowIfCancellationRequested();

            if (sharedActionsGroup.PathIdentity.FileSystemType == FileSystemTypes.Directory)
            {
                var directoryInfo = new DirectoryInfo(destinationPath);

                if (!directoryInfo.Exists)
                {
                    _logger.LogInformation("{Type:l}: creating {directoryInfo}",
                        $"Synchronization.{sharedActionsGroup.Operator}", directoryInfo.FullName);

                    directoryInfo.Create();
                }
                else
                {
                    _logger.LogInformation("{Type:l}: {directoryInfo} already exists",
                        $"Synchronization.{sharedActionsGroup.Operator}", directoryInfo.FullName);
                }

                await _synchronizationActionServerInformer.HandleCloudActionDone(sharedActionsGroup, localTarget,
                    _synchronizationApiClient.AssertDirectoryIsCreated);
            }
            else
            {
                throw new ApplicationException("sharedActionsGroup.PathIdentity.FileSystemType should be FileSystemTypes.File for " +
                                               sharedActionsGroup.ActionsGroupId);
            }
        }
    }
}