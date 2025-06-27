using ByteSync.Common.Business.Inventories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class GetDataSourcesCommandHandler : IRequestHandler<GetDataSourcesRequest, List<EncryptedDataSource>>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ILogger<GetDataSourcesCommandHandler> _logger;

    public GetDataSourcesCommandHandler(
        IInventoryRepository inventoryRepository,
        ILogger<GetDataSourcesCommandHandler> logger)
    {
        _inventoryRepository = inventoryRepository;
        _logger = logger;
    }

    public async Task<List<EncryptedDataSource>> Handle(GetDataSourcesRequest request, CancellationToken cancellationToken)
    {
        var sessionId = request.SessionId;
        var clientInstanceId = request.ClientInstanceId;

        var inventoryData = await _inventoryRepository.Get(sessionId);
        if (inventoryData == null)
        {
            _logger.LogInformation("GetDataSources: session {sessionId}: not found", sessionId);
            return new List<EncryptedDataSource>();
        }

        var inventoryMember = inventoryData.InventoryMembers
            .Find(m => m.ClientInstanceId == clientInstanceId);

        if (inventoryMember == null)
        {
            _logger.LogInformation("GetDataSources: clientInstanceId {clientInstanceId} not found in session {sessionId}", clientInstanceId, sessionId);
            return new List<EncryptedDataSource>();
        }

        return inventoryMember.SharedDataSources;
    }
}