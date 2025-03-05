using ByteSync.Common.Business.Sessions;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Interfaces.Hubs;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class AddPathItemCommandHandler : IRequestHandler<AddPathItemRequest, bool>
{
    private readonly IInventoryMemberService _inventoryMemberService;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly IClientsGroupsInvoker _clientsGroupsInvoker;
    private readonly ILogger<AddPathItemCommandHandler> _logger;
    
    public AddPathItemCommandHandler(
        IInventoryMemberService inventoryMemberService,
        IInventoryRepository inventoryRepository,
        ICloudSessionsRepository cloudSessionsRepository,
        IClientsGroupsInvoker clientsGroupsInvoker,
        ILogger<AddPathItemCommandHandler> logger)
    {
        _inventoryMemberService = inventoryMemberService;
        _inventoryRepository = inventoryRepository;
        _cloudSessionsRepository = cloudSessionsRepository;
        _clientsGroupsInvoker = clientsGroupsInvoker;
        _logger = logger;
    }

    public async Task<bool> Handle(AddPathItemRequest request, CancellationToken cancellationToken)
    {
        var sessionId = request.SessionId;
        var client = request.Client;
        var encryptedPathItem = request.EncryptedPathItem;
        
        var cloudSessionData = await _cloudSessionsRepository.Get(sessionId);
        if (cloudSessionData == null)
        {
            _logger.LogInformation("AddPathItem: session {@sessionId}: not found", sessionId);
            return false;
        }
        
        var updateEntityResult = await _inventoryRepository.AddOrUpdate(sessionId, inventoryData =>
        {
            inventoryData ??= new InventoryData(sessionId);

            if (!inventoryData.IsInventoryStarted)
            {
                var inventoryMember = _inventoryMemberService.GetOrCreateInventoryMember(inventoryData, sessionId, client);

                inventoryMember.SharedPathItems.RemoveAll(p => p.Code == encryptedPathItem.Code);
                inventoryMember.SharedPathItems.Add(encryptedPathItem);

                inventoryData.RecodePathItems(cloudSessionData);
                
                return inventoryData;
            }
            else
            {
                _logger.LogWarning("AddPathItem: session {session} is already activated", sessionId);
                return null;
            }
        });

        if (updateEntityResult.IsSaved)
        {
            var pathItemDto = new PathItemDTO(sessionId, client.ClientInstanceId, encryptedPathItem);
            
            await _clientsGroupsInvoker.SessionGroupExcept(sessionId, client).PathItemAdded(pathItemDto);
        }

        return updateEntityResult.IsSaved;
    }
}