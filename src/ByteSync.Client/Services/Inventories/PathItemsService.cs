using System.Reactive.Linq;
using System.Threading.Tasks;
using ByteSync.Business.PathItems;
using ByteSync.Business.SessionMembers;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Interfaces.Services.Sessions;
using DynamicData;

namespace ByteSync.Services.Inventories;

public class PathItemsService : IPathItemsService
{
    private readonly ISessionService _sessionService;
    private readonly IPathItemChecker _pathItemChecker;
    private readonly IDataEncrypter _dataEncrypter;
    private readonly IConnectionService _connectionService;
    private readonly IInventoryApiClient _inventoryApiClient;
    private readonly IPathItemRepository _pathItemRepository;
    private readonly ISessionMemberRepository _sessionMemberRepository;

    public PathItemsService(ISessionService sessionService, IPathItemChecker pathItemChecker, IDataEncrypter dataEncrypter,
        IConnectionService connectionService, IInventoryApiClient inventoryApiClient,
        IPathItemRepository pathItemRepository, ISessionMemberRepository sessionMemberRepository)
    {
        _sessionService = sessionService;
        _pathItemChecker = pathItemChecker;
        _dataEncrypter = dataEncrypter;
        _connectionService = connectionService;
        _inventoryApiClient = inventoryApiClient;
        _pathItemRepository = pathItemRepository;
        _sessionMemberRepository = sessionMemberRepository;

        _sessionMemberRepository.SortedSessionMembersObservable
            .OnItemRemoved(_ =>
            {
                UpdateCodesForAllMembers(_sessionMemberRepository.Elements);
            })
            .Subscribe();

        _sessionMemberRepository.SortedSessionMembersObservable
            .OnItemAdded(sessionMemberInfo => Observable.FromAsync(() => GetSessionMemberPathItems(sessionMemberInfo))) // Task.Run(() => NewMethod(sessionMemberInfo)))
            .Subscribe();
    }

    private async Task GetSessionMemberPathItems(SessionMemberInfo sessionMemberInfo)
    {
        if (!sessionMemberInfo.HasClientInstanceId(_connectionService.ClientInstanceId!))
        {
            var encryptedPathItems = await _inventoryApiClient.GetPathItems(sessionMemberInfo.SessionId, sessionMemberInfo.ClientInstanceId);

            if (encryptedPathItems != null)
            {
                foreach (var encryptedPathItem in encryptedPathItems)
                {
                    var pathItem = _dataEncrypter.DecryptPathItem(encryptedPathItem);
                    await AddPathItem(pathItem);
                }
            }
        }
    }

    public async Task AddPathItem(PathItem pathItem)
    {
        if (await _pathItemChecker.CheckPathItem(pathItem, _pathItemRepository.Elements))
        {
            var isAddOK = true;
            if (_sessionService.CurrentSession is CloudSession cloudSession
                && pathItem.ClientInstanceId == _connectionService.ClientInstanceId)
            {
                var encryptedPathItem = _dataEncrypter.EncryptPathItem(pathItem);
                isAddOK = await _inventoryApiClient.AddPathItem(cloudSession.SessionId, encryptedPathItem);
            }

            if (isAddOK)
            {
                _pathItemRepository.AddOrUpdate(pathItem);
            }
        }
    }

    public Task CreateAndAddPathItem(string path, FileSystemTypes fileSystemType)
    {
        var pathItem = new PathItem();

        pathItem.Path = path;
        pathItem.Type = fileSystemType;
        pathItem.ClientInstanceId = _connectionService.ClientInstanceId!;

        var sessionMemberInfo = _sessionMemberRepository.GetCurrentSessionMember();
        pathItem.Code = sessionMemberInfo.Letter +
                        (_pathItemRepository.Elements.Count(pi => pi.BelongsTo(sessionMemberInfo)) + 1);

        return AddPathItem(pathItem);
    }

    public async Task RemovePathItem(PathItem pathItem)
    {
        var encryptedPathItem = _dataEncrypter.EncryptPathItem(pathItem);
        var isRemoveOK = await _inventoryApiClient.RemovePathItem(_sessionService.SessionId!, encryptedPathItem);
        
        if (isRemoveOK)
        {
            _pathItemRepository.Remove(pathItem);
        
            var sessionMemberInfo = _sessionMemberRepository.GetElement(pathItem.ClientInstanceId)!;
            UpdateCodesForMember(sessionMemberInfo);
        }
    }
    
    private void UpdateCodesForAllMembers(IEnumerable<SessionMemberInfo> allSessionMembersInfos)
    {
        foreach (var sessionMemberInfo in allSessionMembersInfos)
        {
            UpdateCodesForMember(sessionMemberInfo);
        }
    }

    private void UpdateCodesForMember(SessionMemberInfo sessionMemberInfo)
    {
        var pathItems = _pathItemRepository.Elements
            .Where(pi => pi.BelongsTo(sessionMemberInfo))
            .OrderBy(pi => pi.Code);

        var i = 1;
        foreach (var remainingPathItem in pathItems)
        {
            remainingPathItem.Code = sessionMemberInfo.Letter + i;
            i += 1;
        }
    }
}