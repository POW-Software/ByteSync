using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Helpers;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Helpers;
using ByteSync.ServerCommon.Interfaces.Hubs;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Services;

public class InventoryService : IInventoryService
{
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly ISharedFilesService _sharedFilesService;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IByteSyncClientCaller _byteSyncClientCaller;
    private readonly ICacheService _cacheService;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(ICloudSessionsRepository cloudSessionsRepository, IInventoryRepository inventoryRepository, 
        ISharedFilesService sharedFilesService, IByteSyncClientCaller byteSyncClientCaller, ICacheService cacheService, 
        ILogger<InventoryService> logger)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _inventoryRepository = inventoryRepository;
        _sharedFilesService = sharedFilesService;
        _byteSyncClientCaller = byteSyncClientCaller;
        _cacheService = cacheService;
        _logger = logger;
    }

    // public async Task<bool> AddPathItem(string sessionId, Client client, EncryptedPathItem encryptedPathItem)
    // {
    //     var cloudSessionData = await _cloudSessionsRepository.Get(sessionId);
    //     if (cloudSessionData == null)
    //     {
    //         _logger.LogInformation("AddPathItem: session {@sessionId}: not found", sessionId);
    //         return false;
    //     }
    //     
    //     var updateEntityResult = await _inventoryRepository.AddOrUpdate(sessionId, inventoryData =>
    //     {
    //         inventoryData ??= new InventoryData(sessionId);
    //
    //         if (!inventoryData.IsInventoryStarted)
    //         {
    //             var inventoryMember = GetOrCreateInventoryMember(inventoryData, sessionId, client);
    //
    //             inventoryMember.SharedPathItems.RemoveAll(p => p.Code == encryptedPathItem.Code);
    //             inventoryMember.SharedPathItems.Add(encryptedPathItem);
    //
    //             inventoryData.RecodePathItems(cloudSessionData);
    //             
    //             return inventoryData;
    //         }
    //         else
    //         {
    //             _logger.LogWarning("AddPathItem: session {session} is already activated", sessionId);
    //             return null;
    //         }
    //     });
    //
    //     if (updateEntityResult.IsSaved)
    //     {
    //         var pathItemDto = new PathItemDTO(sessionId, client.ClientInstanceId, encryptedPathItem);
    //         
    //         await _byteSyncClientCaller.SessionGroupExcept(sessionId, client).PathItemAdded(pathItemDto);
    //     }
    //
    //     return updateEntityResult.IsSaved;
    // }

    // public async Task<bool> RemovePathItem(string sessionId, Client client, EncryptedPathItem encryptedPathItem)
    // {
    //     var cloudSessionData = await _cloudSessionsRepository.Get(sessionId);
    //     if (cloudSessionData == null)
    //     {
    //         _logger.LogInformation("RemovePathItem: session {@sessionId}: not found", sessionId);
    //         return false;
    //     }
    //     
    //     var updateEntityResult = await _inventoryRepository.AddOrUpdate(sessionId, inventoryData =>
    //     {
    //         inventoryData ??= new InventoryData(sessionId);
    //
    //         if (!inventoryData.IsInventoryStarted)
    //         {
    //             var inventoryMember = GetOrCreateInventoryMember(inventoryData, sessionId, client);
    //             
    //             inventoryMember.SharedPathItems.RemoveAll(p => p.Code == encryptedPathItem.Code);
    //             
    //             inventoryData.RecodePathItems(cloudSessionData);
    //             
    //             return inventoryData;
    //         }
    //         else
    //         {
    //             _logger.LogWarning("RemovePathItem: session {session} is already activated", sessionId);
    //             return null;
    //         }
    //     });
    //     
    //     if (updateEntityResult.IsSaved)
    //     {
    //         var pathItemDto = new PathItemDTO(sessionId, client.ClientInstanceId, encryptedPathItem);
    //         
    //         await _byteSyncClientCaller.SessionGroupExcept(sessionId, client).PathItemRemoved(pathItemDto);
    //     }
    //     
    //     return updateEntityResult.IsSaved;
    // }
    //
    // public async Task<List<EncryptedPathItem>> GetPathItems(string sessionId, string clientInstanceId)
    // {
    //     var inventoryData = await _inventoryRepository.Get(sessionId);
    //     
    //     var inventoryMember = inventoryData?.InventoryMembers.SingleOrDefault(m => m.ClientInstanceId == clientInstanceId);
    //     
    //     if (inventoryMember == null)
    //     {
    //         return new List<EncryptedPathItem>();
    //     }
    //     else
    //     {
    //         return inventoryMember!.SharedPathItems;
    //     }
    // }
    
    public async Task<bool> SetLocalInventoryStatus(Client client, UpdateSessionMemberGeneralStatusParameters parameters)
    {
        var updateResult = await _inventoryRepository.Update(parameters.SessionId, inventoryData =>
        {
            var inventoryMember = inventoryData.InventoryMembers.Single(m => m.ClientInstanceId == client.ClientInstanceId);
            
            if (inventoryMember.LastLocalInventoryStatusUpdate == null || 
                parameters.UtcChangeDate > inventoryMember.LastLocalInventoryStatusUpdate)
            {
                inventoryMember.SessionMemberGeneralStatus = parameters.SessionMemberGeneralStatus;

                inventoryMember.LastLocalInventoryStatusUpdate = parameters.UtcChangeDate;
                
                _byteSyncClientCaller.SessionGroupExcept(parameters.SessionId, client).SessionMemberGeneralStatusUpdated(parameters);

                return true;
            }
            else
            {
                return false;
            }
        });

        return updateResult.IsSaved;
    }

    public async Task ResetSession(string sessionId)
    {
        await _inventoryRepository.UpdateIfExists(sessionId, inventoryData =>
        {
            inventoryData.IsInventoryStarted = false;

            return true;
        });
    }

    private static InventoryMemberData GetOrCreateInventoryMember(InventoryData inventoryData, string sessionId, Client client)
    {
        var inventoryMember = inventoryData.InventoryMembers.SingleOrDefault(imd => imd.ClientInstanceId == client.ClientInstanceId);

        if (inventoryMember == null)
        {
            inventoryMember = new InventoryMemberData
            {
                SessionId = sessionId,
                ClientInstanceId = client.ClientInstanceId,
                SessionMemberGeneralStatus = SessionMemberGeneralStatus.InventoryWaitingForStart,
            };

            inventoryData.InventoryMembers.Add(inventoryMember);
        }
        
        return inventoryMember;
    }
}