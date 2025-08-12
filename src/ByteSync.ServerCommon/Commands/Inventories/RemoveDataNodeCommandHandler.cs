using ByteSync.Common.Business.Sessions;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using MediatR;
using Microsoft.Extensions.Logging;
using ByteSync.ServerCommon.Entities.Inventories;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class RemoveDataNodeCommandHandler : IRequestHandler<RemoveDataNodeRequest, bool>
{
    private readonly IInventoryMemberService _inventoryMemberService;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly IInvokeClientsService _invokeClientsService;
    private readonly ILogger<RemoveDataNodeCommandHandler> _logger;

    public RemoveDataNodeCommandHandler(
        IInventoryMemberService inventoryMemberService,
        IInventoryRepository inventoryRepository,
        ICloudSessionsRepository cloudSessionsRepository,
        IInvokeClientsService invokeClientsService,
        ILogger<RemoveDataNodeCommandHandler> logger)
    {
        _inventoryMemberService = inventoryMemberService;
        _inventoryRepository = inventoryRepository;
        _cloudSessionsRepository = cloudSessionsRepository;
        _invokeClientsService = invokeClientsService;
        _logger = logger;
    }

    public async Task<bool> Handle(RemoveDataNodeRequest request, CancellationToken cancellationToken)
    {
        var sessionId = request.SessionId;
        var client = request.Client;
        var cloudSessionData = await _cloudSessionsRepository.Get(sessionId);
        if (cloudSessionData == null)
        {
            _logger.LogInformation("RemoveDataNode: session {sessionId}: not found", sessionId);
            return false;
        }

        var updateEntityResult = await _inventoryRepository.AddOrUpdate(sessionId, inventoryData =>
        {
            inventoryData ??= new InventoryEntity(sessionId);

            if (!inventoryData.IsInventoryStarted)
            {
                var inventoryMember = _inventoryMemberService.GetOrCreateInventoryMember(inventoryData, sessionId, client);

                inventoryMember.DataNodes.RemoveAll(p => p.Id == request.EncryptedDataNode.Id);

                return inventoryData;
            }
            else
            {
                _logger.LogWarning("RemoveDataNode: session {sessionId} is already activated", sessionId);
                return null;
            }
        });

        if (updateEntityResult.IsSaved)
        {
            var dto = new DataNodeDTO(sessionId, client.ClientInstanceId, request.EncryptedDataNode);
            await _invokeClientsService.SessionGroupExcept(sessionId, client).DataNodeRemoved(dto);
        }

        return updateEntityResult.IsSaved;
    }
}
