using System.Reactive.Linq;
using ByteSync.Business.DataNodes;
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
    private readonly IDataNodeRepository _dataNodeRepository;
    private readonly IDataSourceCodeGenerator _codeGenerator;

    public DataSourceService(ISessionService sessionService, IDataSourceChecker dataSourceChecker, IDataEncrypter dataEncrypter,
        IConnectionService connectionService, IInventoryApiClient inventoryApiClient,
        IDataSourceRepository dataSourceRepository, IDataNodeRepository dataNodeRepository,
        IDataSourceCodeGenerator codeGenerator)
    {
        _sessionService = sessionService;
        _dataSourceChecker = dataSourceChecker;
        _dataEncrypter = dataEncrypter;
        _connectionService = connectionService;
        _inventoryApiClient = inventoryApiClient;
        _dataSourceRepository = dataSourceRepository;
        _dataNodeRepository = dataNodeRepository;
        _codeGenerator = codeGenerator;

        // _dataNodeRepository.SortedSessionMembersObservable
        //     .OnItemRemoved(_ =>
        //     {
        //         UpdateCodesForAllMembers(_dataNodeRepository.Elements);
        //     })
        //     .Subscribe();
        //
        // _dataNodeRepository.SortedSessionMembersObservable
        //     .OnItemAdded(sessionMemberInfo => Observable.FromAsync(() => GetSessionMemberDataSources(sessionMemberInfo))) // Task.Run(() => NewMethod(sessionMemberInfo)))
        //     .Subscribe();
    }

    private async Task GetSessionMemberDataSources(SessionMember sessionMember)
    {
        if (!sessionMember.HasClientInstanceId(_connectionService.ClientInstanceId!))
        {
            var encryptedDataSources = await _inventoryApiClient.GetDataSources(sessionMember.SessionId, sessionMember.ClientInstanceId);

            if (encryptedDataSources != null)
            {
                foreach (var encryptedDataSource in encryptedDataSources)
                {
                    var dataSource = _dataEncrypter.DecryptDataSource(encryptedDataSource);
                    await TryAddDataSource(dataSource);
                }
            }
        }
    }

    // TODO data-nodes-and-local-sync
    public async Task<bool> TryAddDataSource(DataSource dataSource)
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
        _codeGenerator.RecomputeCodesForNode(dataSource.DataNodeId);
    }

    public Task CreateAndTryAddDataSource(string path, FileSystemTypes fileSystemType, DataNode dataNode)
    {
        var dataSource = new DataSource();

        dataSource.Path = path;
        dataSource.Type = fileSystemType;
        dataSource.ClientInstanceId = _connectionService.ClientInstanceId!;
        dataSource.DataNodeId = dataNode.NodeId;

        // var sessionMemberInfo = _dataNodeRepository.GetCurrentSessionMember();
        // dataSource.Code = sessionMemberInfo.GetLetter() +
        //                 (_dataSourceRepository.Elements.Count(ds => ds.BelongsTo(sessionMemberInfo)) + 1);

        return TryAddDataSource(dataSource);
    }

    public async Task<bool> TryRemoveDataSource(DataSource dataSource)
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
        _codeGenerator.RecomputeCodesForNode(dataSource.DataNodeId);
    }

}