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
    private readonly ISynchronizationStatusCheckerService _synchronizationStatusCheckerService;
    private readonly IInvokeClientsService _invokeClientsService;
    private readonly ITransferLocationService _transferLocationService;

    private readonly ILogger<AssertUploadIsFinishedCommandHandler> _logger;
    public AssertUploadIsFinishedCommandHandler(ICloudSessionsRepository cloudSessionsRepository,
        ISharedFilesService sharedFilesService,
        ITrackingActionRepository trackingActionRepository,
        ISynchronizationStatusCheckerService synchronizationStatusCheckerService,
        IInvokeClientsService invokeClientsService,
        ITransferLocationService transferLocationService,
        ILogger<AssertUploadIsFinishedCommandHandler> logger)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _sharedFilesService = sharedFilesService;
        _trackingActionRepository = trackingActionRepository;
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
                
                var otherSessionMemberIds = otherSessionMembers.Select(sm => sm.ClientInstanceId).ToList();
                
                await _sharedFilesService.AssertUploadIsFinished(request.TransferParameters, otherSessionMemberIds);
                
                await InformOtherClients(sharedFileDefinition, totalParts, otherSessionMemberIds);
            }
            else
            {
                HashSet<string> targetInstanceIds = new HashSet<string>();
                        
                var result = await _trackingActionRepository.AddOrUpdate(sharedFileDefinition.SessionId, sharedFileDefinition.ActionsGroupIds!, 
                    (trackingAction, synchronization) =>
                {
                    if (!_synchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronization))
                    {
                        return false;
                    }
                    
                    trackingAction.IsSourceSuccess = true;

                    foreach (var target in trackingAction.TargetClientInstanceAndNodeIds)
                    {
                        targetInstanceIds.Add(target.ClientInstanceId);
                    }

                    return true;
                });

                if (result.IsSuccess)
                {
                    await _sharedFilesService.AssertUploadIsFinished(request.TransferParameters, targetInstanceIds);

                    await InformOtherClients(sharedFileDefinition, totalParts, targetInstanceIds);
                }
            }
        }
        
        _logger.LogDebug("Upload finished asserted for session {SessionId}, file {FileId}", 
            request.SessionId, request.TransferParameters.SharedFileDefinition.Id);
    }

    private async Task InformOtherClients(SharedFileDefinition sharedFileDefinition, int totalParts,
        ICollection<string> targetInstanceIds)
    {
        var transferPush = new FileTransferPush
        {
            SessionId = sharedFileDefinition.SessionId,
            SharedFileDefinition = sharedFileDefinition,
            TotalParts = totalParts,
            ActionsGroupIds = sharedFileDefinition.ActionsGroupIds!
        };

        await _invokeClientsService.Clients(targetInstanceIds).UploadFinished(transferPush);
    }

    private static List<SessionMemberData> GetOtherSessionMembers(CloudSessionData session, SessionMemberData sessionMemberData)
    {
        var otherSessionMembers = session.SessionMembers
            .Where(sm => !Equals(sm.ClientInstanceId, sessionMemberData.ClientInstanceId))
            .ToList();
        
        return otherSessionMembers;
    }
} 