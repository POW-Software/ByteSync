using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.FileTransfers;

public class AssertUploadIsFinishedCommandHandler : IRequestHandler<AssertUploadIsFinishedRequest>
{
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly ISharedFilesService _sharedFilesService;
    private readonly ITrackingActionRepository _trackingActionRepository;
    private readonly ISynchronizationProgressService _synchronizationProgressService;
    private readonly ISynchronizationStatusCheckerService _synchronizationStatusCheckerService;
    private readonly IInvokeClientsService _invokeClientsService;
    private readonly ITransferLocationService _transferLocationService;

    private readonly ILogger<AssertUploadIsFinishedCommandHandler> _logger;
    public AssertUploadIsFinishedCommandHandler(ICloudSessionsRepository cloudSessionsRepository,
        ISharedFilesService sharedFilesService,
        ITrackingActionRepository trackingActionRepository,
        ISynchronizationProgressService synchronizationProgressService,
        ISynchronizationStatusCheckerService synchronizationStatusCheckerService,
        IInvokeClientsService invokeClientsService,
        ITransferLocationService transferLocationService,
        ILogger<AssertUploadIsFinishedCommandHandler> logger)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _sharedFilesService = sharedFilesService;
        _trackingActionRepository = trackingActionRepository;
        _synchronizationProgressService = synchronizationProgressService;
        _synchronizationStatusCheckerService = synchronizationStatusCheckerService;
        _invokeClientsService = invokeClientsService;
        _transferLocationService = transferLocationService;
        _logger = logger;
    }
    
    public async Task Handle(AssertUploadIsFinishedRequest request, CancellationToken cancellationToken)
    {
        var session = await _cloudSessionsRepository.Get(request.SessionId);
        var sessionMemberData = session?.FindMember(request.Client.ClientInstanceId);
        var sharedFileDefinition = request.TransferParameters.SharedFileDefinition;
        var totalParts = request.TransferParameters.TotalParts!.Value;

        if (sessionMemberData != null && _transferLocationService.IsSharedFileDefinitionAllowed(sessionMemberData, sharedFileDefinition))
        {
            _logger.LogInformation("AssertUploadIsFinished: {cloudSession} {sharedFileDefinition}", request.SessionId, sharedFileDefinition.Id);

            if (sharedFileDefinition.IsInventory || sharedFileDefinition.IsSynchronizationStartData || sharedFileDefinition.IsProfileDetails)
            {
                var otherSessionMembers = GetOtherSessionMembers(session!, sessionMemberData);
                
                await _sharedFilesService.AssertUploadIsFinished(request.TransferParameters, 
                    otherSessionMembers.Select(sm => sm.ClientInstanceId).ToList());

                var transferPush = new FileTransferPush
                {
                    SessionId = request.SessionId,
                    SharedFileDefinition = sharedFileDefinition,
                    TotalParts = totalParts,
                    ActionsGroupIds = request.TransferParameters.ActionsGroupIds
                };
                await _invokeClientsService.Clients(otherSessionMembers).UploadFinished(transferPush);
            }
            else
            {
                // Logic moved from SynchronizationService.OnUploadIsFinishedAsync
                var actionsGroupsIds = sharedFileDefinition.ActionsGroupIds;

                HashSet<string> targetInstanceIds = new HashSet<string>();
                        
                var result = await _trackingActionRepository.AddOrUpdate(sharedFileDefinition.SessionId, actionsGroupsIds!, (trackingAction, synchronization) =>
                {
                    if (!_synchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronization))
                    {
                        return false;
                    }
                    
                    trackingAction.IsSourceSuccess = true;

                    foreach (var targetClientInstanceId in trackingAction.TargetClientInstanceAndNodeIds)
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
        }
        
        _logger.LogDebug("Upload finished asserted for session {SessionId}, file {FileId}", 
            request.SessionId, request.TransferParameters.SharedFileDefinition.Id);
    }
    
    private static List<SessionMemberData> GetOtherSessionMembers(CloudSessionData session, SessionMemberData sessionMemberData)
    {
        var otherSessionMembers = session.SessionMembers
            .Where(sm => !Equals(sm.ClientInstanceId, sessionMemberData.ClientInstanceId))
            .ToList();
        
        return otherSessionMembers;
    }
} 