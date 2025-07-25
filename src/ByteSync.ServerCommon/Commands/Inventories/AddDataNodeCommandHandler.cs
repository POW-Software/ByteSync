using ByteSync.Common.Business.Sessions;
using ByteSync.ServerCommon.Entities.Inventories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class AddDataNodeCommandHandler : IRequestHandler<AddDataNodeRequest, bool>
{
    private readonly IInventoryMemberService _inventoryMemberService;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly IInvokeClientsService _invokeClientsService;
    private readonly ILogger<AddDataNodeCommandHandler> _logger;

    public AddDataNodeCommandHandler(
        IInventoryMemberService inventoryMemberService,
        IInventoryRepository inventoryRepository,
        ICloudSessionsRepository cloudSessionsRepository,
        IInvokeClientsService invokeClientsService,
        ILogger<AddDataNodeCommandHandler> logger)
    {
        _inventoryMemberService = inventoryMemberService;
        _inventoryRepository = inventoryRepository;
        _cloudSessionsRepository = cloudSessionsRepository;
        _invokeClientsService = invokeClientsService;
        _logger = logger;
    }

    public async Task<bool> Handle(AddDataNodeRequest request, CancellationToken cancellationToken)
    {
        var sessionId = request.SessionId;
        var client = request.Client;
        var encryptedDataNode = request.EncryptedDataNode;
        var cloudSessionData = await _cloudSessionsRepository.Get(sessionId);
        if (cloudSessionData == null)
        {
            _logger.LogInformation("AddDataNode: session {sessionId}: not found", sessionId);
            return false;
        }

        var updateEntityResult = await _inventoryRepository.AddOrUpdate(sessionId, inventoryData =>
        {
            inventoryData ??= new InventoryEntity(sessionId);

            if (!inventoryData.IsInventoryStarted)
            {
                var inventoryMember = _inventoryMemberService.GetOrCreateInventoryMember(inventoryData, sessionId, client);

                inventoryMember.DataNodes.RemoveAll(n => n.Id == encryptedDataNode.Id);
                inventoryMember.DataNodes.Add(encryptedDataNode);

                return inventoryData;
            }
            else
            {
                _logger.LogWarning("AddDataNode: session {sessionId} is already activated", sessionId);
                return null;
            }
        });

        if (updateEntityResult.IsSaved)
        {
            var dto = new DataNodeDTO(sessionId, client.ClientInstanceId, encryptedDataNode);
            await _invokeClientsService.SessionGroupExcept(sessionId, client).DataNodeAdded(dto);
        }

        return updateEntityResult.IsSaved;
    }
}
