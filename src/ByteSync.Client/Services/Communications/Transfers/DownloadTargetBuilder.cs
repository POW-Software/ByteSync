﻿using ByteSync.Business.Communications;
using ByteSync.Business.Communications.Downloading;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Profiles;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Communications.Transfers;

public class DownloadTargetBuilder : IDownloadTargetBuilder
{
    private readonly ICloudSessionLocalDataManager _cloudSessionLocalDataManager;
    private readonly ISessionProfileLocalDataManager _sessionProfileLocalDataManager;
    private readonly ISharedActionsGroupRepository _sharedActionsGroupRepository;
    private readonly IConnectionService _connectionService;
    private readonly ITemporaryFileManagerFactory _temporaryFileManagerFactory;

    public DownloadTargetBuilder(ICloudSessionLocalDataManager cloudSessionLocalDataManager, ISessionProfileLocalDataManager sessionProfileLocalDataManager,
        ISharedActionsGroupRepository sharedActionsGroupRepository, IConnectionService connectionService,
        ITemporaryFileManagerFactory temporaryFileManagerFactory)
    {
        _cloudSessionLocalDataManager = cloudSessionLocalDataManager;
        _sessionProfileLocalDataManager = sessionProfileLocalDataManager;
        _sharedActionsGroupRepository = sharedActionsGroupRepository;
        _connectionService = connectionService;
        _temporaryFileManagerFactory = temporaryFileManagerFactory;
    }
    
    public DownloadTarget BuildDownloadTarget(SharedFileDefinition sharedFileDefinition)
    {
        LocalSharedFile? sharedFile = null;
        var downloadDestinations = new HashSet<string>();
        string destinationPath;
        var isMultiFileZip = false;
        Dictionary<string, HashSet<string>>? finalDestinationsPerActionsGroupId = null;
        Dictionary<string, DownloadTargetDates>? datesPerActionsGroupId = null;
        List<ITemporaryFileManager>? temporaryFileManagers = null;

        DownloadTarget downloadTarget;
        if (sharedFileDefinition.IsInventory)
        {
            destinationPath = _cloudSessionLocalDataManager.GetInventoryPath(sharedFileDefinition);
            sharedFile = new LocalSharedFile(sharedFileDefinition, destinationPath);
            downloadDestinations.Add(destinationPath);
        }
        else if (sharedFileDefinition.IsSynchronizationStartData)
        {
            destinationPath = _cloudSessionLocalDataManager.GetSynchronizationStartDataPath();
            sharedFile = new LocalSharedFile(sharedFileDefinition, destinationPath);
            downloadDestinations.Add(destinationPath);
        }
        else if (sharedFileDefinition.IsProfileDetails)
        {
            destinationPath = _sessionProfileLocalDataManager.GetProfileZipPath(sharedFileDefinition);
            sharedFile = new LocalSharedFile(sharedFileDefinition, destinationPath);
            downloadDestinations.Add(destinationPath);
        }
        else if (sharedFileDefinition.IsSynchronization)
        {
            isMultiFileZip = sharedFileDefinition.IsMultiFileZip;
            finalDestinationsPerActionsGroupId = new Dictionary<string, HashSet<string>>();
            datesPerActionsGroupId = new Dictionary<string, DownloadTargetDates>();
            
            // var actionsGroupsIds = _synchronizationActionsService.GetActionsGroupIds(sharedFileDefinition)!;
            
            if (isMultiFileZip)
            {
                var zipDestinationPath = _cloudSessionLocalDataManager.GetSynchronizationTempZipPath(sharedFileDefinition);
                downloadDestinations.Add(zipDestinationPath);

                foreach (var actionsGroupId in sharedFileDefinition.ActionsGroupIds!)
                {
                    var sharedActionsGroup = _sharedActionsGroupRepository.GetSharedActionsGroup(actionsGroupId);
                    var actionsGroupDestinations = sharedActionsGroup!.GetTargetsFullNames(_connectionService.CurrentEndPoint!);
                    
                    finalDestinationsPerActionsGroupId.Add(actionsGroupId, actionsGroupDestinations);

                    if (sharedActionsGroup.IsSynchronizeContentAndDate)
                    {
                        datesPerActionsGroupId.Add(actionsGroupId, DownloadTargetDates.FromSharedActionsGroup(sharedActionsGroup));
                    }
                }
            }
            else
            {
                var sharedActionsGroup = _sharedActionsGroupRepository.GetSharedActionsGroup(sharedFileDefinition.ActionsGroupIds!.Single());
                
                var finalDestinations = sharedActionsGroup!.GetTargetsFullNames(_connectionService.CurrentEndPoint!);
                finalDestinationsPerActionsGroupId.Add(sharedActionsGroup.ActionsGroupId, finalDestinations);
                
                if (sharedActionsGroup.SynchronizationType == SynchronizationTypes.Full)
                {
                    temporaryFileManagers = new List<ITemporaryFileManager>();
                    foreach (var finalDestination in finalDestinations)
                    {
                        var temporaryFileManager = _temporaryFileManagerFactory.Create(finalDestination);
                        var destinationTemporaryPath = temporaryFileManager.GetDestinationTemporaryPath();

                        downloadDestinations.Add(destinationTemporaryPath);
                        temporaryFileManagers.Add(temporaryFileManager);
                    }
                }
                else
                {
                    var target = sharedActionsGroup.Targets.First(t => Equals(t.ClientInstanceId, _connectionService.ClientInstanceId));
                    
                    var deltaDestination = _cloudSessionLocalDataManager.GetTempDeltaFullName(sharedActionsGroup.Source!, target);
                    downloadDestinations.Add(deltaDestination);
                }
                
                if (sharedActionsGroup.IsSynchronizeContentAndDate)
                {
                    datesPerActionsGroupId.Add(sharedActionsGroup.ActionsGroupId, DownloadTargetDates.FromSharedActionsGroup(sharedActionsGroup));
                }
            }
        }
        else
        {
            throw new ApplicationException("SharedFileDefinition Type unknown");
        }
        
        downloadTarget = new DownloadTarget(sharedFileDefinition, sharedFile, downloadDestinations);
        downloadTarget.IsMultiFileZip = isMultiFileZip;
        downloadTarget.FinalDestinationsPerActionsGroupId = finalDestinationsPerActionsGroupId;
        downloadTarget.LastWriteTimeUtcPerActionsGroupId = datesPerActionsGroupId;
        downloadTarget.TemporaryFileManagers = temporaryFileManagers;

        return downloadTarget;
    }
}