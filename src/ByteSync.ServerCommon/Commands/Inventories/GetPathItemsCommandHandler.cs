using ByteSync.Common.Business.Inventories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class GetPathItemsCommandHandler : IRequestHandler<GetPathItemsRequest, List<EncryptedPathItem>>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ILogger<GetPathItemsCommandHandler> _logger;

    public GetPathItemsCommandHandler(
        IInventoryRepository inventoryRepository,
        ILogger<GetPathItemsCommandHandler> logger)
    {
        _inventoryRepository = inventoryRepository;
        _logger = logger;
    }

    public async Task<List<EncryptedPathItem>> Handle(GetPathItemsRequest request, CancellationToken cancellationToken)
    {
        var sessionId = request.SessionId;
        var clientInstanceId = request.ClientInstanceId;

        var inventoryData = await _inventoryRepository.Get(sessionId);
        if (inventoryData == null)
        {
            _logger.LogInformation("GetPathItems: session {sessionId}: not found", sessionId);
            return new List<EncryptedPathItem>();
        }

        var inventoryMember = inventoryData.InventoryMembers
            .Find(m => m.ClientInstanceId == clientInstanceId);

        if (inventoryMember == null)
        {
            _logger.LogInformation("GetPathItems: clientInstanceId {clientInstanceId} not found in session {sessionId}", clientInstanceId, sessionId);
            return new List<EncryptedPathItem>();
        }

        return inventoryMember.SharedPathItems;
    }
}