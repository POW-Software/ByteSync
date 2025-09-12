using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Exceptions;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.FileTransfers;

public class AssertFilePartIsUploadedCommandHandler : IRequestHandler<AssertFilePartIsUploadedRequest>
{
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly ISharedFilesService _sharedFilesService;
    private readonly ISynchronizationRepository _synchronizationRepository;
    private readonly ITrackingActionRepository _trackingActionRepository;
    private readonly ISynchronizationStatusCheckerService _synchronizationStatusCheckerService;
    private readonly IInvokeClientsService _invokeClientsService;
    private readonly IUsageStatisticsService _usageStatisticsService;
    private readonly ITransferLocationService _transferLocationService;
    private readonly ILogger<AssertFilePartIsUploadedCommandHandler> _logger;
    
    public AssertFilePartIsUploadedCommandHandler(
        ICloudSessionsRepository cloudSessionsRepository,
        ISharedFilesService sharedFilesService,
        ISynchronizationRepository synchronizationRepository,
        ITrackingActionRepository trackingActionRepository,
        ISynchronizationStatusCheckerService synchronizationStatusCheckerService,
        IInvokeClientsService invokeClientsService,
        IUsageStatisticsService usageStatisticsService,
        ITransferLocationService transferLocationService,
        ILogger<AssertFilePartIsUploadedCommandHandler> logger)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _sharedFilesService = sharedFilesService;
        _synchronizationRepository = synchronizationRepository;
        _trackingActionRepository = trackingActionRepository;
        _synchronizationStatusCheckerService = synchronizationStatusCheckerService;
        _invokeClientsService = invokeClientsService;
        _usageStatisticsService = usageStatisticsService;
        _transferLocationService = transferLocationService;
        _logger = logger;
    }
    
    public async Task Handle(AssertFilePartIsUploadedRequest request, CancellationToken cancellationToken)
    {
        var session = await _cloudSessionsRepository.Get(request.SessionId);
        var sessionMemberData = session?.FindMember(request.Client.ClientInstanceId);
        var sharedFileDefinition = request.TransferParameters.SharedFileDefinition;
        var partNumber = request.TransferParameters.PartNumber!.Value;
        
        // Track uploaded volume if part size is provided
        if (request.TransferParameters.PartSizeInBytes.HasValue && sharedFileDefinition.IsSynchronization)
        {
            await _synchronizationRepository.UpdateIfExists(sharedFileDefinition.SessionId, synchronization =>
            {
                if (_synchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronization))
                {
                    synchronization.Progress.ActualUploadedVolume += request.TransferParameters.PartSizeInBytes.Value;
                    return true;
                }
                return false;
            });
        }
        
        _ = _usageStatisticsService.RegisterUploadUsage(request.Client, sharedFileDefinition, partNumber);

        if (sessionMemberData != null && _transferLocationService.IsSharedFileDefinitionAllowed(sessionMemberData, sharedFileDefinition))
        {
            if (sharedFileDefinition.IsInventory || sharedFileDefinition.IsSynchronizationStartData || sharedFileDefinition.IsProfileDetails)
            {
                var otherSessionMembers = GetOtherSessionMembers(session!, sessionMemberData);
                
                await _sharedFilesService.AssertFilePartIsUploaded(request.TransferParameters, 
                    otherSessionMembers.Select(sm => sm.ClientInstanceId).ToList());

                var transferPush = new FileTransferPush
                {
                    SessionId = request.SessionId,
                    SharedFileDefinition = sharedFileDefinition,
                    PartNumber = partNumber,
                    ActionsGroupIds = request.TransferParameters.ActionsGroupIds
                };
                await _invokeClientsService.Clients(otherSessionMembers).FilePartUploaded(transferPush);
            }
            else
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

                var transferParameters = new TransferParameters
                {
                    SessionId = sharedFileDefinition.SessionId,
                    SharedFileDefinition = sharedFileDefinition,
                    PartNumber = partNumber
                };
                var recipientClientIds = trackingAction.TargetClientInstanceAndNodeIds
                    .Select(x => x.ClientInstanceId)
                    .Distinct()
                    .ToList();
                await _sharedFilesService.AssertFilePartIsUploaded(transferParameters, recipientClientIds);
                
                var fileTransferPush = new FileTransferPush
                {
                    SessionId = sharedFileDefinition.SessionId,
                    SharedFileDefinition = sharedFileDefinition,
                    PartNumber = partNumber,
                    ActionsGroupIds = sharedFileDefinition.ActionsGroupIds!
                };
                await _invokeClientsService.Clients(recipientClientIds).FilePartUploaded(fileTransferPush);
            }
        }
        
        _logger.LogDebug("File part upload asserted for session {SessionId}, file {FileId}, part {PartNumber}", 
            request.SessionId, request.TransferParameters.SharedFileDefinition.Id, request.TransferParameters.PartNumber);
    }
    
    private static List<SessionMemberData> GetOtherSessionMembers(CloudSessionData session, SessionMemberData sessionMemberData)
    {
        var otherSessionMembers = session.SessionMembers
            .Where(sm => !Equals(sm.ClientInstanceId, sessionMemberData.ClientInstanceId))
            .ToList();
        
        return otherSessionMembers;
    }
} 