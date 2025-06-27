using ByteSync.Common.Business.Inventories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class GetPathItemsCommandHandler : IRequestHandler<GetPathItemsRequest, List<EncryptedDataSource>>
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

    public async Task<List<EncryptedDataSource>> Handle(GetPathItemsRequest request, CancellationToken cancellationToken)
    {
        var sessionId = request.SessionId;
        var clientInstanceId = request.ClientInstanceId;

        var inventoryData = await _inventoryRepository.Get(sessionId);
        if (inventoryData == null)
        {
            _logger.LogInformation("GetPathItems: session {sessionId}: not found", sessionId);
            return new List<EncryptedDataSource>();
        }

        var inventoryMember = inventoryData.InventoryMembers
            .Find(m => m.ClientInstanceId == clientInstanceId);

        if (inventoryMember == null)
        {
            _logger.LogInformation("GetPathItems: clientInstanceId {clientInstanceId} not found in session {sessionId}", clientInstanceId, sessionId);
            return new List<EncryptedDataSource>();
        }

        return inventoryMember.SharedPathItems;
    }
}