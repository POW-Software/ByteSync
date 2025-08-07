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
    private readonly ISynchronizationService _synchronizationService;
    private readonly IInvokeClientsService _invokeClientsService;
    private readonly ILogger<AssertUploadIsFinishedCommandHandler> _logger;
    private readonly ITransferLocationService _transferLocationService;

    public AssertUploadIsFinishedCommandHandler(
        ICloudSessionsRepository cloudSessionsRepository,
        ISharedFilesService sharedFilesService,
        ISynchronizationService synchronizationService,
        IInvokeClientsService invokeClientsService,
        ILogger<AssertUploadIsFinishedCommandHandler> logger,
        ITransferLocationService transferLocationService)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _sharedFilesService = sharedFilesService;
        _synchronizationService = synchronizationService;
        _invokeClientsService = invokeClientsService;
        _logger = logger;
        _transferLocationService = transferLocationService;
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
                
                await _sharedFilesService.AssertUploadIsFinished(sharedFileDefinition, totalParts, 
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
                await _synchronizationService.OnUploadIsFinishedAsync(sharedFileDefinition, totalParts, request.Client);
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