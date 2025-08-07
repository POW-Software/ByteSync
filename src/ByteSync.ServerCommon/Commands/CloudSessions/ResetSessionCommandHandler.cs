using ByteSync.Common.Business.Sessions;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public class ResetSessionCommandHandler : IRequestHandler<ResetSessionRequest, Unit>
{
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly IInventoryService _inventoryService;
    private readonly ISynchronizationService _synchronizationService;
    private readonly ISharedFilesService _sharedFilesService;
    private readonly IInvokeClientsService _invokeClientsService;
    private readonly ILogger<ResetSessionCommandHandler> _logger;
    
    public ResetSessionCommandHandler(ICloudSessionsRepository cloudSessionsRepository, 
        IInventoryService inventoryService, ISynchronizationService synchronizationService,
        ISharedFilesService sharedFilesService, IInvokeClientsService invokeClientsService,
        ILogger<ResetSessionCommandHandler> logger)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _inventoryService = inventoryService;
        _synchronizationService = synchronizationService;
        _sharedFilesService = sharedFilesService;
        _invokeClientsService = invokeClientsService;
        _logger = logger;
    }
    
    public async Task<Unit> Handle(ResetSessionRequest request, CancellationToken cancellationToken)
    {
        await _cloudSessionsRepository.Update(request.SessionId, cloudSessionData =>
        {
            cloudSessionData.ResetSession();
            
            return true;
        });
        
        await _inventoryService.ResetSession(request.SessionId);

        await _synchronizationService.ResetSession(request.SessionId);
        
        await _sharedFilesService.ClearSession(request.SessionId);
        
        _logger.LogInformation("ResetSession: session {sessionId} reset by {clientInstanceId}", request.SessionId, request.Client.ClientInstanceId);
        
        await _invokeClientsService.SessionGroupExcept(request.SessionId, request.Client)
            .SessionResetted(new BaseSessionDto(request.SessionId, request.Client.ClientInstanceId));

        return Unit.Value;
    }
} 