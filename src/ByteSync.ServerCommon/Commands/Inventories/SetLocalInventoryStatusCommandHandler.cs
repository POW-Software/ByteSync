using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class SetLocalInventoryStatusCommandHandler : IRequestHandler<SetLocalInventoryStatusRequest, bool>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IInventoryMemberService _inventoryMemberService;
    private readonly IInvokeClientsService _invokeClientsService;
    private readonly ILogger<SetLocalInventoryStatusCommandHandler> _logger;

    public SetLocalInventoryStatusCommandHandler(IInventoryRepository inventoryRepository, IInventoryMemberService inventoryMemberService, 
        IInvokeClientsService invokeClientsService, ILogger<SetLocalInventoryStatusCommandHandler> logger)
    {
        _inventoryRepository = inventoryRepository;
        _inventoryMemberService = inventoryMemberService;
        _invokeClientsService = invokeClientsService;
        _logger = logger;
    }

    public async Task<bool> Handle(SetLocalInventoryStatusRequest request, CancellationToken cancellationToken)
    {
        var sessionId = request.Parameters.SessionId;
        var client = request.Client;
        var parameters = request.Parameters;

        var updateResult = await _inventoryRepository.AddOrUpdate(sessionId, inventoryData =>
        {
            inventoryData ??= new InventoryData(sessionId);

            if (!inventoryData.IsInventoryStarted)
            {
                var inventoryMember = _inventoryMemberService.GetOrCreateInventoryMember(inventoryData, sessionId, client);
                
                if (inventoryMember.LastLocalInventoryStatusUpdate == null ||
                    parameters.UtcChangeDate > inventoryMember.LastLocalInventoryStatusUpdate)
                {
                    inventoryMember.SessionMemberGeneralStatus = parameters.SessionMemberGeneralStatus;
                    inventoryMember.LastLocalInventoryStatusUpdate = parameters.UtcChangeDate;
                
                    _invokeClientsService.SessionGroupExcept(sessionId, client).SessionMemberGeneralStatusUpdated(parameters);

                    return inventoryData;
                }
                else
                {
                    _logger.LogWarning("SetLocalInventoryStatus: session {sessionId}, client {clientInstanceId} has a more recent status update", sessionId,
                        client.ClientInstanceId);
                    return null;
                }
            }
            else
            {
                _logger.LogWarning("RemovePathItem: session {sessionId} is already activated", sessionId);
                return null;
            }
        });

        if (!updateResult.IsSaved)
        {
            _logger.LogWarning("SetLocalInventoryStatus: Failed to update status for session {sessionId}, client {clientInstanceId}", sessionId,
                client.ClientInstanceId);
        }

        return updateResult.IsSaved;
    }
}