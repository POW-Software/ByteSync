using ByteSync.Common.Business.Sessions;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using System.Linq;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class AddDataSourceCommandHandler : IRequestHandler<AddDataSourceRequest, bool>
{
    private readonly IInventoryMemberService _inventoryMemberService;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly IInvokeClientsService _invokeClientsService;
    private readonly ILogger<AddDataSourceCommandHandler> _logger;
    
    public AddDataSourceCommandHandler(
        IInventoryMemberService inventoryMemberService,
        IInventoryRepository inventoryRepository,
        ICloudSessionsRepository cloudSessionsRepository,
        IInvokeClientsService invokeClientsService,
        ILogger<AddDataSourceCommandHandler> logger)
    {
        _inventoryMemberService = inventoryMemberService;
        _inventoryRepository = inventoryRepository;
        _cloudSessionsRepository = cloudSessionsRepository;
        _invokeClientsService = invokeClientsService;
        _logger = logger;
    }

    public async Task<bool> Handle(AddDataSourceRequest request, CancellationToken cancellationToken)
    {
        var sessionId = request.SessionId;
        var client = request.Client;
        var encryptedDataSource = request.EncryptedDataSource;
        
        var cloudSessionData = await _cloudSessionsRepository.Get(sessionId);
        if (cloudSessionData == null)
        {
            _logger.LogInformation("AddDataSource: session {@sessionId}: not found", sessionId);
            return false;
        }
        
        var updateEntityResult = await _inventoryRepository.AddOrUpdate(sessionId, inventoryData =>
        {
            inventoryData ??= new InventoryData(sessionId);

            if (!inventoryData.IsInventoryStarted)
            {
                var inventoryMember = _inventoryMemberService.GetOrCreateInventoryMember(inventoryData, sessionId, client);

                // TODO data-nodes-and-local-sync : to rework
                var dataNode = inventoryMember.DataNodes.FirstOrDefault();
                if (dataNode == null)
                {
                    dataNode = new DataNodeData { NodeId = client.ClientInstanceId };
                    inventoryMember.DataNodes.Add(dataNode);
                }

                dataNode.DataSources.RemoveAll(p => p.Code == encryptedDataSource.Code);
                dataNode.DataSources.Add(encryptedDataSource);

                inventoryData.RecodeDataSources(cloudSessionData);
                
                return inventoryData;
            }
            else
            {
                _logger.LogWarning("AddDataSource: session {session} is already activated", sessionId);
                return null;
            }
        });

        if (updateEntityResult.IsSaved)
        {
            var dataSourceDto = new DataSourceDTO(sessionId, client.ClientInstanceId, encryptedDataSource);
            
            await _invokeClientsService.SessionGroupExcept(sessionId, client).DataSourceAdded(dataSourceDto);
        }

        return updateEntityResult.IsSaved;
    }
}