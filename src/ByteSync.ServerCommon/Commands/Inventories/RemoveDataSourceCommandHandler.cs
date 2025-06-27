using ByteSync.Common.Business.Sessions;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class RemoveDataSourceCommandHandler : IRequestHandler<RemoveDataSourceRequest, bool>
{
    private readonly IInventoryMemberService _inventoryMemberService;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly IInvokeClientsService _invokeClientsService;
    private readonly ILogger<RemoveDataSourceCommandHandler> _logger;
    
    public RemoveDataSourceCommandHandler(
        IInventoryMemberService inventoryMemberService,
        IInventoryRepository inventoryRepository,
        ICloudSessionsRepository cloudSessionsRepository,
        IInvokeClientsService invokeClientsService,
        ILogger<RemoveDataSourceCommandHandler> logger)
    {
        _inventoryMemberService = inventoryMemberService;
        _inventoryRepository = inventoryRepository;
        _cloudSessionsRepository = cloudSessionsRepository;
        _invokeClientsService = invokeClientsService;
        _logger = logger;
    }

    public async Task<bool> Handle(RemoveDataSourceRequest request, CancellationToken cancellationToken)
    {
        var cloudSessionData = await _cloudSessionsRepository.Get(request.SessionId);
        if (cloudSessionData == null)
        {
            _logger.LogInformation("RemoveDataSource: session {sessionId}: not found", request.SessionId);
            return false;
        }

        var updateEntityResult = await _inventoryRepository.AddOrUpdate(request.SessionId, inventoryData =>
        {
            inventoryData ??= new InventoryData(request.SessionId);

            if (!inventoryData.IsInventoryStarted)
            {
                var inventoryMember = _inventoryMemberService.GetOrCreateInventoryMember(inventoryData, request.SessionId, request.Client);

                inventoryMember.SharedDataSources.RemoveAll(p => p.Code == request.EncryptedDataSource.Code);

                inventoryData.RecodeDataSources(cloudSessionData);

                return inventoryData;
            }
            else
            {
                _logger.LogWarning("RemoveDataSource: session {sessionId} is already activated", request.SessionId);
                return null;
            }
        });

        if (updateEntityResult.IsSaved)
        {
            var dataSourceDto = new DataSourceDTO(request.SessionId, request.Client.ClientInstanceId, request.EncryptedDataSource);
            await _invokeClientsService.SessionGroupExcept(request.SessionId, request.Client).DataSourceRemoved(dataSourceDto);
        }

        return updateEntityResult.IsSaved;
    }
}