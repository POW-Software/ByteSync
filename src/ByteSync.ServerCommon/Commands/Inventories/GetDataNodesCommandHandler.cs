
using ByteSync.Common.Business.Sessions;
using ByteSync.ServerCommon.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class GetDataNodesCommandHandler : IRequestHandler<GetDataNodesRequest, List<EncryptedDataNode>>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ILogger<GetDataNodesCommandHandler> _logger;

    public GetDataNodesCommandHandler(
        IInventoryRepository inventoryRepository,
        ILogger<GetDataNodesCommandHandler> logger)
    {
        _inventoryRepository = inventoryRepository;
        _logger = logger;
    }

    public async Task<List<EncryptedDataNode>> Handle(GetDataNodesRequest request, CancellationToken cancellationToken)
    {
        var sessionId = request.SessionId;
        var clientInstanceId = request.ClientInstanceId;

        var inventoryData = await _inventoryRepository.Get(sessionId);
        if (inventoryData == null)
        {
            _logger.LogInformation("GetDataSources: session {sessionId}: not found", sessionId);
            return new List<EncryptedDataNode>();
        }

        var inventoryMember = inventoryData.InventoryMembers
            .Find(m => m.ClientInstanceId == clientInstanceId);

        if (inventoryMember == null)
        {
            _logger.LogInformation("GetDataSources: clientInstanceId {clientInstanceId} not found in session {sessionId}", clientInstanceId, sessionId);
            return new List<EncryptedDataNode>();
        }

        // TBD
        return new List<EncryptedDataNode>(); //inventoryMember.DataNodes.SelectMany(n => n.DataSources).ToList();
    }
}