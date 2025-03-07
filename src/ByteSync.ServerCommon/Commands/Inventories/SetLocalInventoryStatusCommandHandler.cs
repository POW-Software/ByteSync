using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class SetLocalInventoryStatusCommandHandler : IRequestHandler<SetLocalInventoryStatusRequest, bool>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IClientsGroupsInvoker _clientsGroupsInvoker;
    private readonly ILogger<SetLocalInventoryStatusCommandHandler> _logger;

    public SetLocalInventoryStatusCommandHandler(
        IInventoryRepository inventoryRepository,
        IClientsGroupsInvoker clientsGroupsInvoker,
        ILogger<SetLocalInventoryStatusCommandHandler> logger)
    {
        _inventoryRepository = inventoryRepository;
        _clientsGroupsInvoker = clientsGroupsInvoker;
        _logger = logger;
    }

    public async Task<bool> Handle(SetLocalInventoryStatusRequest request, CancellationToken cancellationToken)
    {
        var sessionId = request.Parameters.SessionId;
        var client = request.Client;
        var parameters = request.Parameters;

        var updateResult = await _inventoryRepository.Update(sessionId, inventoryData =>
        {
            var inventoryMember = inventoryData.InventoryMembers.SingleOrDefault(m => m.ClientInstanceId == client.ClientInstanceId);
            if (inventoryMember == null)
            {
                _logger.LogInformation("SetLocalInventoryStatus: clientInstanceId {clientInstanceId} not found in session {sessionId}", client.ClientInstanceId,
                    sessionId);
                return false;
            }

            if (inventoryMember.LastLocalInventoryStatusUpdate == null ||
                parameters.UtcChangeDate > inventoryMember.LastLocalInventoryStatusUpdate)
            {
                inventoryMember.SessionMemberGeneralStatus = parameters.SessionMemberGeneralStatus;
                inventoryMember.LastLocalInventoryStatusUpdate = parameters.UtcChangeDate;
                
                _clientsGroupsInvoker.SessionGroupExcept(sessionId, client).SessionMemberGeneralStatusUpdated(parameters);

                return true;
            }
            else
            {
                _logger.LogWarning("SetLocalInventoryStatus: session {sessionId}, client {clientInstanceId} has a more recent status update", sessionId,
                    client.ClientInstanceId);
                return false;
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