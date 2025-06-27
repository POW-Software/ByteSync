using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class AddDataNodeCommandHandler : IRequestHandler<AddDataNodeRequest, bool>
{
    private readonly IInventoryMemberService _inventoryMemberService;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly ILogger<AddDataNodeCommandHandler> _logger;

    public AddDataNodeCommandHandler(
        IInventoryMemberService inventoryMemberService,
        IInventoryRepository inventoryRepository,
        ICloudSessionsRepository cloudSessionsRepository,
        ILogger<AddDataNodeCommandHandler> logger)
    {
        _inventoryMemberService = inventoryMemberService;
        _inventoryRepository = inventoryRepository;
        _cloudSessionsRepository = cloudSessionsRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(AddDataNodeRequest request, CancellationToken cancellationToken)
    {
        var sessionId = request.SessionId;
        var client = request.Client;
        var nodeId = request.NodeId;

        var cloudSessionData = await _cloudSessionsRepository.Get(sessionId);
        if (cloudSessionData == null)
        {
            _logger.LogInformation("AddDataNode: session {sessionId}: not found", sessionId);
            return false;
        }

        var updateEntityResult = await _inventoryRepository.AddOrUpdate(sessionId, inventoryData =>
        {
            inventoryData ??= new InventoryData(sessionId);

            if (!inventoryData.IsInventoryStarted)
            {
                var inventoryMember = _inventoryMemberService.GetOrCreateInventoryMember(inventoryData, sessionId, client);

                var dataNode = inventoryMember.DataNodes.FirstOrDefault(n => n.NodeId == nodeId);
                if (dataNode == null)
                {
                    inventoryMember.DataNodes.Add(new DataNodeData { NodeId = nodeId });
                }

                return inventoryData;
            }
            else
            {
                _logger.LogWarning("AddDataNode: session {sessionId} is already activated", sessionId);
                return null;
            }
        });

        return updateEntityResult.IsSaved;
    }
}
