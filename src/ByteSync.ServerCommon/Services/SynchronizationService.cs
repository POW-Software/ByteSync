using System.Collections.Concurrent;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Common.Helpers;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Exceptions;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Services;

public class SynchronizationService : ISynchronizationService
{
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly ISynchronizationRepository _synchronizationRepository;
    private readonly ITrackingActionRepository _trackingActionRepository;
    private readonly ISynchronizationProgressService _synchronizationProgressService;
    private readonly ISynchronizationStatusCheckerService _synchronizationStatusCheckerService;
    private readonly ILogger<SynchronizationService> _logger;

    public SynchronizationService(ICloudSessionsRepository cloudSessionsRepository, ISynchronizationRepository synchronizationRepository,
        ITrackingActionRepository trackingActionRepository, ISynchronizationProgressService synchronizationProgressService, 
        ISynchronizationStatusCheckerService synchronizationStatusCheckerService, ILogger<SynchronizationService> logger)
    {
        _cloudSessionsRepository = cloudSessionsRepository;    
        _synchronizationRepository = synchronizationRepository;
        _trackingActionRepository = trackingActionRepository;
        _synchronizationProgressService = synchronizationProgressService;
        _synchronizationStatusCheckerService = synchronizationStatusCheckerService;
        _logger = logger;
    }
    
    public async Task<Synchronization?> GetSynchronization(string sessionId, Client client)
    {
        var synchronizationEntity = await _synchronizationRepository.Get(sessionId);

        if (synchronizationEntity != null && synchronizationEntity.Progress.Members.Any(m => client.ClientInstanceId == m))
        {
            return await _synchronizationProgressService.MapToSynchronization(synchronizationEntity);
        }
        else
        {
            return null;
        }
    }
    
    public async Task StartSynchronization(string sessionId, Client client, List<ActionsGroupDefinition> actionsGroupDefinitions)
    {
        var synchronizationEntity = await _synchronizationRepository.Get(sessionId);
        
        if (synchronizationEntity == null)
        {
            var session = await _cloudSessionsRepository.Get(sessionId);
            
            synchronizationEntity = new SynchronizationEntity
            {
                SessionId = sessionId,
                Progress = new SynchronizationProgressEntity
                {
                    TotalActionsCount = actionsGroupDefinitions.Count,
                    Members = session!.SessionMembers.Select(m => m.ClientInstanceId).ToList(),
                },
                StartedOn = DateTimeOffset.Now,
                StartedBy = client.ClientInstanceId
            };
            
            await _synchronizationRepository.AddSynchronization(synchronizationEntity, actionsGroupDefinitions);
            
            await _synchronizationProgressService.InformSynchronizationStarted(synchronizationEntity, client);
        }
    }
    
    public async Task OnUploadIsFinishedAsync(SharedFileDefinition sharedFileDefinition, int totalParts, Client client)
    {
        var actionsGroupsIds = sharedFileDefinition.ActionsGroupIds;

        HashSet<string> targetInstanceIds = new HashSet<string>();
                
        var result = await _trackingActionRepository.AddOrUpdate(sharedFileDefinition.SessionId, actionsGroupsIds!, (trackingAction, synchronization) =>
        {
            if (!_synchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronization))
            {
                return false;
            }
            
            trackingAction.IsSourceSuccess = true;

            foreach (var targetClientInstanceId in trackingAction.TargetClientInstanceIds)
            {
                targetInstanceIds.Add(targetClientInstanceId);
            }

            return true;
        });

        if (result.IsSuccess)
        {
            await _synchronizationProgressService.UploadIsFinished(sharedFileDefinition, totalParts, targetInstanceIds);
        }
    }

    public async Task OnFilePartIsUploadedAsync(SharedFileDefinition sharedFileDefinition, int partNumber)
    {
        var synchronization = await _synchronizationRepository.Get(sharedFileDefinition.SessionId);

        if (!_synchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronization))
        {
            return;
        }
        
        if (sharedFileDefinition.ActionsGroupIds == null || sharedFileDefinition.ActionsGroupIds.Count == 0)
        {
            throw new BadRequestException("sharedFileDefinition.ActionsGroupIds is null or empty");
        }
        
        var actionsGroupsId = sharedFileDefinition.ActionsGroupIds!.First();
        var trackingAction = await _trackingActionRepository.GetOrThrow(sharedFileDefinition.SessionId, actionsGroupsId);
        
        await _synchronizationProgressService.FilePartIsUploaded(sharedFileDefinition, partNumber, trackingAction.TargetClientInstanceIds);
    }

    public async Task OnDownloadIsFinishedAsync(SharedFileDefinition sharedFileDefinition, Client client)
    {
        var actionsGroupsIds = sharedFileDefinition.ActionsGroupIds;

        bool needSendSynchronizationUpdated = false;
                
        var result = await _trackingActionRepository.AddOrUpdate(sharedFileDefinition.SessionId, actionsGroupsIds!, (trackingAction, synchronization) =>
        {
            if (!_synchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronization))
            {
                return false;
            }
            
            bool wasTrackingActionFinished = trackingAction.IsFinished;
            
            trackingAction.AddSuccessOnTarget(client.ClientInstanceId);

            if (!wasTrackingActionFinished && trackingAction.IsFinished)
            {
                synchronization.Progress.FinishedActionsCount += 1;
                synchronization.Progress.ProcessedVolume += trackingAction.Size ?? 0;
            }
            
            synchronization.Progress.ExchangedVolume += sharedFileDefinition.UploadedFileLength;
            
            needSendSynchronizationUpdated = CheckSynchronizationIsFinished(synchronization);

            return true;
        });

        if (result.IsSuccess)
        {
            await _synchronizationProgressService.UpdateSynchronizationProgress(result, needSendSynchronizationUpdated);
        }
    }

    public Task OnDateIsCopied(string sessionId, List<string> actionsGroupIds, Client client)
    {
        return OnSuccessOnTarget(sessionId, actionsGroupIds, client);
    }

    public Task OnFileOrDirectoryIsDeletedAsync(string sessionId, List<string> actionsGroupIds, Client client)
    {
        return OnSuccessOnTarget(sessionId, actionsGroupIds, client);
    }

    public Task OnDirectoryIsCreatedAsync(string sessionId, List<string> actionsGroupIds, Client client)
    {
        return OnSuccessOnTarget(sessionId, actionsGroupIds, client);
    }

    private async Task OnSuccessOnTarget(string sessionId, List<string> actionsGroupIds, Client client)
    {
        if (actionsGroupIds.Count == 0)
        {
            _logger.LogInformation("OnSuccessOnTarget: no action group id provided");
            return;
        }
        
        bool needSendSynchronizationUpdated = false;
        
        var result = await _trackingActionRepository.AddOrUpdate(sessionId, actionsGroupIds, (trackingAction, synchronization) =>
        {
            if (!_synchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronization))
            {
                return false;
            }
            
            bool wasTrackingActionFinished = trackingAction.IsFinished;
            
            trackingAction.AddSuccessOnTarget(client.ClientInstanceId);
            
            if (!wasTrackingActionFinished && trackingAction.IsFinished)
            {
                synchronization.Progress.FinishedActionsCount += 1;
            }
            
            needSendSynchronizationUpdated = CheckSynchronizationIsFinished(synchronization);

            return true;
        });

        if (result.IsSuccess)
        {
            await _synchronizationProgressService.UpdateSynchronizationProgress(result, needSendSynchronizationUpdated);
        }
    }

    public async Task OnLocalCopyIsDoneAsync(string sessionId, List<string> actionsGroupIds, Client client)
    {
        if (actionsGroupIds.Count == 0)
        {
            _logger.LogInformation("OnSuccessOnTarget: no action group id provided");
            return;
        }
        
        bool needSendSynchronizationUpdated = false;
                
        var result = await _trackingActionRepository.AddOrUpdate(sessionId, actionsGroupIds, (trackingAction, synchronization) =>
        {
            if (!_synchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronization))
            {
                return false;
            }
            
            bool wasTrackingActionFinished = trackingAction.IsFinished;
            
            trackingAction.IsSourceSuccess = true;
            trackingAction.AddSuccessOnTarget(client.ClientInstanceId);

            if (!wasTrackingActionFinished && trackingAction.IsFinished)
            {
                synchronization.Progress.FinishedActionsCount += 1;
            }
            
            synchronization.Progress.ProcessedVolume += trackingAction.Size ?? 0;
            
            needSendSynchronizationUpdated = CheckSynchronizationIsFinished(synchronization);

            return true;
        });

        if (result.IsSuccess)
        {
            await _synchronizationProgressService.UpdateSynchronizationProgress(result, needSendSynchronizationUpdated);
        }
    }

    public async Task AssertSynchronizationActionErrors(string sessionId, List<string> actionsGroupIds, Client client)
    {
        if (actionsGroupIds.Count == 0)
        {
            _logger.LogInformation("OnSuccessOnTarget: no action group id provided");
            return;
        }
        
        bool needSendSynchronizationUpdated = false;
        
        var result = await _trackingActionRepository.AddOrUpdate(sessionId, actionsGroupIds, (trackingAction, synchronization) =>
        {
            if (!_synchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronization))
            {
                return false;
            }
            
            bool wasTrackingActionFinished = trackingAction.IsFinished;
            bool isNewError = !trackingAction.IsError;
            
            if (trackingAction.SourceClientInstanceId == client.ClientInstanceId)
            {
                trackingAction.IsSourceSuccess = false;
            }
            else
            {
                if (trackingAction.TargetClientInstanceIds.Contains(client.ClientInstanceId))
                {
                    trackingAction.AddErrorOnTarget(client.ClientInstanceId);
                }
                else
                {
                    throw new InvalidOperationException("Client is not a target of the action");
                }
            }
            
            if (isNewError)
            {
                synchronization.Progress.ErrorsCount += 1;
            }
            if (!wasTrackingActionFinished && trackingAction.IsFinished)
            {
                synchronization.Progress.FinishedActionsCount += 1;
            }
            
            needSendSynchronizationUpdated = CheckSynchronizationIsFinished(synchronization);

            return true;
        });

        if (result.IsSuccess)
        {
            await _synchronizationProgressService.UpdateSynchronizationProgress(result, needSendSynchronizationUpdated);
        }
    }

    public async Task OnMemberHasFinished(string sessionId, Client client)
    {
        bool needSendSynchronizationUpdated = false;
        
        var result = await _synchronizationRepository.UpdateIfExists(sessionId, synchronizationEntity =>
        {
            if (synchronizationEntity.Progress.Members.Contains(client.ClientInstanceId))
            {
                synchronizationEntity.Progress.CompletedMembers.Add(client.ClientInstanceId);

                needSendSynchronizationUpdated = CheckSynchronizationIsFinished(synchronizationEntity);
                
                _logger.LogInformation("Member {ClientInstanceId} has finished synchronization", client.ClientInstanceId);
                
                return true;
            }

            return false;
        });

        if (result.IsSaved)
        {
            await _synchronizationProgressService.UpdateSynchronizationProgress(result.Element!, needSendSynchronizationUpdated);
        }
    }

    private bool CheckSynchronizationIsFinished(SynchronizationEntity synchronizationEntity)
    {
        bool isUpdated = false;
        
        if (!synchronizationEntity.IsEnded && 
            (synchronizationEntity.Progress.AllMembersCompleted && 
                (synchronizationEntity.Progress.AllActionsDone || synchronizationEntity.IsAbortRequested)))
        {
            synchronizationEntity.EndedOn = DateTimeOffset.Now;
            
            if (synchronizationEntity.IsAbortRequested)
            {
                synchronizationEntity.EndStatus = SynchronizationEndStatuses.Abortion;
            }
            else
            {
                synchronizationEntity.EndStatus = SynchronizationEndStatuses.Regular;
            }
            
            isUpdated = true;
        }

        return isUpdated;
    }

    public async Task ResetSession(string sessionId)
    {
        await _synchronizationRepository.ResetSession(sessionId);
    }

    public async Task RequestAbortSynchronization(string sessionId, Client client)
    {
        var result = await _synchronizationRepository.UpdateIfExists(sessionId, synchronizationEntity =>
        {
            if (! _synchronizationStatusCheckerService.CheckSynchronizationCanBeAborted(synchronizationEntity))
            {
                return false;
            }

            bool isDateUpdated = false;
            bool isRequesterAdded = false;

            if (synchronizationEntity.AbortRequestedOn == null)
            {
                synchronizationEntity.AbortRequestedOn = DateTimeOffset.Now;
                isDateUpdated = true;
            }
            
            if (!synchronizationEntity.AbortRequestedBy.Contains(client.ClientInstanceId))
            {
                synchronizationEntity.AbortRequestedBy.Add(client.ClientInstanceId);
                isRequesterAdded = true;
            }
            
            return isDateUpdated || isRequesterAdded;
        });

        if (result.IsSaved)
        {
            await _synchronizationProgressService.UpdateSynchronizationProgress(result.Element!, true);
        }
    }
}