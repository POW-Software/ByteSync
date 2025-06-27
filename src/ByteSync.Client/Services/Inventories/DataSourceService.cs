using System.Reactive.Linq;
using ByteSync.Business.DataSources;
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
using ByteSync.Services.Sessions;
using DynamicData;

namespace ByteSync.Services.Inventories;

public class DataSourceService : IDataSourceService
{
    private readonly ISessionService _sessionService;
    private readonly IPathItemChecker _pathItemChecker;
    private readonly IDataEncrypter _dataEncrypter;
    private readonly IConnectionService _connectionService;
    private readonly IInventoryApiClient _inventoryApiClient;
    private readonly IDataSourceRepository _dataSourceRepository;
    private readonly ISessionMemberRepository _sessionMemberRepository;

    public DataSourceService(ISessionService sessionService, IPathItemChecker pathItemChecker, IDataEncrypter dataEncrypter,
        IConnectionService connectionService, IInventoryApiClient inventoryApiClient,
        IDataSourceRepository dataSourceRepository, ISessionMemberRepository sessionMemberRepository)
    {
        _sessionService = sessionService;
        _pathItemChecker = pathItemChecker;
        _dataEncrypter = dataEncrypter;
        _connectionService = connectionService;
        _inventoryApiClient = inventoryApiClient;
        _dataSourceRepository = dataSourceRepository;
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
                    await TryAddDataSource(pathItem);
                }
            }
        }
    }

    public async Task<bool> TryAddDataSource(DataSource dataSource)
    {
        if (await _pathItemChecker.CheckPathItem(dataSource, _dataSourceRepository.Elements))
        {
            var isAddOK = true;
            if (_sessionService.CurrentSession is CloudSession cloudSession
                && dataSource.ClientInstanceId == _connectionService.ClientInstanceId)
            {
                var encryptedPathItem = _dataEncrypter.EncryptPathItem(dataSource);
                isAddOK = await _inventoryApiClient.AddPathItem(cloudSession.SessionId, encryptedPathItem);
            }

            if (isAddOK)
            {
                ApplyAddDataSourceLocally(dataSource);
            }
            
            return isAddOK;
        }
        
        return false;
    }

    public void ApplyAddDataSourceLocally(DataSource dataSource)
    {
        _dataSourceRepository.AddOrUpdate(dataSource);
    }

    public Task CreateAndTryAddDataSource(string path, FileSystemTypes fileSystemType)
    {
        var pathItem = new DataSource();

        pathItem.Path = path;
        pathItem.Type = fileSystemType;
        pathItem.ClientInstanceId = _connectionService.ClientInstanceId!;

        var sessionMemberInfo = _sessionMemberRepository.GetCurrentSessionMember();
        pathItem.Code = sessionMemberInfo.GetLetter() +
                        (_dataSourceRepository.Elements.Count(pi => pi.BelongsTo(sessionMemberInfo)) + 1);

        return TryAddDataSource(pathItem);
    }

    public async Task<bool> TryRemoveDataSource(DataSource dataSource)
    {
        var encryptedPathItem = _dataEncrypter.EncryptPathItem(dataSource);
        var isRemoveOK = await _inventoryApiClient.RemovePathItem(_sessionService.SessionId!, encryptedPathItem);
        
        if (isRemoveOK)
        {
            ApplyRemoveDataSourceLocally(dataSource);
        }
        
        return isRemoveOK;
    }

    public void ApplyRemoveDataSourceLocally(DataSource dataSource)
    {
        _dataSourceRepository.Remove(dataSource);
        
        var sessionMemberInfo = _sessionMemberRepository.GetElement(dataSource.ClientInstanceId);

        if (sessionMemberInfo != null)
        {
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
        var pathItems = _dataSourceRepository.Elements
            .Where(pi => pi.BelongsTo(sessionMemberInfo))
            .OrderBy(pi => pi.Code);

        var i = 1;
        foreach (var remainingPathItem in pathItems)
        {
            remainingPathItem.Code = sessionMemberInfo.GetLetter() + i;
            i += 1;
        }
    }
}