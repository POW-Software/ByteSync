using ByteSync.ServerCommon.Entities.Inventories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.SessionMembers;

public class SetGeneralStatusCommandHandler : IRequestHandler<SetGeneralStatusRequest, bool>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IInventoryMemberService _inventoryMemberService;
    private readonly IInvokeClientsService _invokeClientsService;
    private readonly ILogger<SetGeneralStatusCommandHandler> _logger;

    public SetGeneralStatusCommandHandler(IInventoryRepository inventoryRepository, IInventoryMemberService inventoryMemberService, 
        IInvokeClientsService invokeClientsService, ILogger<SetGeneralStatusCommandHandler> logger)
    {
        _inventoryRepository = inventoryRepository;
        _inventoryMemberService = inventoryMemberService;
        _invokeClientsService = invokeClientsService;
        _logger = logger;
    }

    public async Task<bool> Handle(SetGeneralStatusRequest request, CancellationToken cancellationToken)
    {
        var sessionId = request.Parameters.SessionId;
        var client = request.Client;
        var parameters = request.Parameters;

        var updateResult = await _inventoryRepository.AddOrUpdate(sessionId, inventoryData =>
        {
            inventoryData ??= new InventoryEntity(sessionId);

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
                _logger.LogWarning("SetGeneralStatus: session {sessionId}, client {clientInstanceId} has a more recent status update", sessionId,
                    client.ClientInstanceId);
                return null;
            }
        });

        if (!updateResult.IsSaved)
        {
            _logger.LogWarning("SetGeneralStatus: Failed to update status for session {sessionId}, client {clientInstanceId}", sessionId,
                client.ClientInstanceId);
        }

        return updateResult.IsSaved;
    }
} 