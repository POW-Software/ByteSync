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
    private readonly IDataSourceChecker _dataSourceChecker;
    private readonly IDataEncrypter _dataEncrypter;
    private readonly IConnectionService _connectionService;
    private readonly IInventoryApiClient _inventoryApiClient;
    private readonly IDataSourceRepository _dataSourceRepository;
    private readonly ISessionMemberRepository _sessionMemberRepository;

    public DataSourceService(ISessionService sessionService, IDataSourceChecker dataSourceChecker, IDataEncrypter dataEncrypter,
        IConnectionService connectionService, IInventoryApiClient inventoryApiClient,
        IDataSourceRepository dataSourceRepository, ISessionMemberRepository sessionMemberRepository)
    {
        _sessionService = sessionService;
        _dataSourceChecker = dataSourceChecker;
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
            .OnItemAdded(sessionMemberInfo => Observable.FromAsync(() => GetSessionMemberDataSources(sessionMemberInfo))) // Task.Run(() => NewMethod(sessionMemberInfo)))
            .Subscribe();
    }

    private async Task GetSessionMemberDataSources(SessionMemberInfo sessionMemberInfo)
    {
        if (!sessionMemberInfo.HasClientInstanceId(_connectionService.ClientInstanceId!))
        {
            var encryptedDataSources = await _inventoryApiClient.GetDataSources(sessionMemberInfo.SessionId, sessionMemberInfo.ClientInstanceId);

            if (encryptedDataSources != null)
            {
                foreach (var encryptedDataSource in encryptedDataSources)
                {
                    var dataSource = _dataEncrypter.DecryptDataSource(encryptedDataSource);
                    await TryAddDataSource(dataSource, sessionMemberInfo.ClientInstanceId);
                }
            }
        }
    }

    // TODO data-nodes-and-local-sync
    public async Task<bool> TryAddDataSource(DataSource dataSource, string? nodeId = null)
    {
        if (await _dataSourceChecker.CheckDataSource(dataSource, _dataSourceRepository.Elements))
        {
            var isAddOK = true;
            if (_sessionService.CurrentSession is CloudSession cloudSession
                && dataSource.ClientInstanceId == _connectionService.ClientInstanceId)
            {
                var encryptedDataSource = _dataEncrypter.EncryptDataSource(dataSource);
                isAddOK = await _inventoryApiClient.AddDataSource(cloudSession.SessionId, encryptedDataSource);
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

    public Task CreateAndTryAddDataSource(string path, FileSystemTypes fileSystemType, string? nodeId = null)
    {
        var dataSource = new DataSource();

        dataSource.Path = path;
        dataSource.Type = fileSystemType;
        dataSource.ClientInstanceId = _connectionService.ClientInstanceId!;

        var sessionMemberInfo = _sessionMemberRepository.GetCurrentSessionMember();
        dataSource.Code = sessionMemberInfo.GetLetter() +
                        (_dataSourceRepository.Elements.Count(ds => ds.BelongsTo(sessionMemberInfo)) + 1);

        return TryAddDataSource(dataSource, nodeId);
    }

    public async Task<bool> TryRemoveDataSource(DataSource dataSource, string? nodeId = null)
    {
        var encryptedDataSource = _dataEncrypter.EncryptDataSource(dataSource);
        var isRemoveOK = await _inventoryApiClient.RemoveDataSource(_sessionService.SessionId!, encryptedDataSource);
        
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
        var dataSources = _dataSourceRepository.Elements
            .Where(ds => ds.BelongsTo(sessionMemberInfo))
            .OrderBy(ds => ds.Code);

        var i = 1;
        foreach (var remainingDataSource in dataSources)
        {
            remainingDataSource.Code = sessionMemberInfo.GetLetter() + i;
            i += 1;
        }
    }
}