using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Sessions;
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
    private readonly ISynchronizationService _synchronizationService;
    private readonly IInvokeClientsService _invokeClientsService;
    private readonly IUsageStatisticsService _usageStatisticsService;
    private readonly ILogger<AssertFilePartIsUploadedCommandHandler> _logger;
    private readonly ITransferLocationService _transferLocationService;
    public AssertFilePartIsUploadedCommandHandler(
        ICloudSessionsRepository cloudSessionsRepository,
        ISharedFilesService sharedFilesService,
        ISynchronizationService synchronizationService,
        IInvokeClientsService invokeClientsService,
        IUsageStatisticsService usageStatisticsService,
        ITransferLocationService transferLocationService,
        ILogger<AssertFilePartIsUploadedCommandHandler> logger)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _sharedFilesService = sharedFilesService;
        _synchronizationService = synchronizationService;
        _invokeClientsService = invokeClientsService;
        _usageStatisticsService = usageStatisticsService;
        _logger = logger;
        _transferLocationService = transferLocationService;
    }
    
    public async Task Handle(AssertFilePartIsUploadedRequest request, CancellationToken cancellationToken)
    {
        var session = await _cloudSessionsRepository.Get(request.SessionId);
        var sessionMemberData = session?.FindMember(request.Client.ClientInstanceId);
        var sharedFileDefinition = request.TransferParameters.SharedFileDefinition;
        var partNumber = request.TransferParameters.PartNumber!.Value;
        
        _ = _usageStatisticsService.RegisterUploadUsage(request.Client, sharedFileDefinition, partNumber);

        if (sessionMemberData != null && _transferLocationService.IsSharedFileDefinitionAllowed(sessionMemberData, sharedFileDefinition))
        {
            if (sharedFileDefinition.IsInventory || sharedFileDefinition.IsSynchronizationStartData || sharedFileDefinition.IsProfileDetails)
            {
                var otherSessionMembers = GetOtherSessionMembers(session!, sessionMemberData);
                
                await _sharedFilesService.AssertFilePartIsUploaded(sharedFileDefinition, partNumber, 
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
                await _synchronizationService.OnFilePartIsUploadedAsync(sharedFileDefinition, partNumber);
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