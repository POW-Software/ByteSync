using System.Reactive.Linq;
using System.Threading;
using ByteSync.Business.DataNodes;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Inventories;

public class DataNodeService : IDataNodeService
{
    private readonly ISessionService _sessionService;
    private readonly IConnectionService _connectionService;
    private readonly IDataEncrypter _dataEncrypter;
    private readonly IInventoryApiClient _inventoryApiClient;
    private readonly IDataNodeRepository _dataNodeRepository;
    private readonly IDataNodeCodeGenerator _codeGenerator;
    private readonly IDataSourceService _dataSourceService;
    private readonly IDataSourceRepository _dataSourceRepository;
    
    private int _nodeCounter;
    private readonly SemaphoreSlim _counterSemaphore = new(1, 1);

    public DataNodeService(ISessionService sessionService, IConnectionService connectionService,
        IDataEncrypter dataEncrypter,
        IInventoryApiClient inventoryApiClient, IDataNodeRepository dataNodeRepository,
        IDataNodeCodeGenerator codeGenerator, IDataSourceService dataSourceService,
        IDataSourceRepository dataSourceRepository)
    {
        _sessionService = sessionService;
        _connectionService = connectionService;
        _dataEncrypter = dataEncrypter;
        _inventoryApiClient = inventoryApiClient;
        _dataNodeRepository = dataNodeRepository;
        _codeGenerator = codeGenerator;
        _dataSourceService = dataSourceService;
        _dataSourceRepository = dataSourceRepository;
        
        // Reset the counter when a session ends or is reset
        _sessionService.SessionObservable
            .Where(session => session == null)
            .SelectMany(_ => Observable.FromAsync(ResetNodeCounterAsync));

        _sessionService.SessionStatusObservable
            .Where(status => status == SessionStatus.Preparation)
            .SelectMany(_ => Observable.FromAsync(ResetNodeCounterAsync));
    }

    public async Task<bool> TryAddDataNode(DataNode dataNode)
    {
        var isAddOK = true;
        if (_sessionService.CurrentSession is CloudSession cloudSession
            && dataNode.ClientInstanceId == _connectionService.ClientInstanceId)
        {
            var encryptedDataNode = _dataEncrypter.EncryptDataNode(dataNode);
            isAddOK = await _inventoryApiClient.AddDataNode(cloudSession.SessionId, _connectionService.ClientInstanceId!, encryptedDataNode);
        }

        if (isAddOK)
        {
            ApplyAddDataNodeLocally(dataNode);
        }

        return isAddOK;
    }

    public async Task CreateAndTryAddDataNode(string? nodeId = null)
    {
        if (nodeId == null)
        {
            await _counterSemaphore.WaitAsync();
            try
            {
                nodeId = $"NID_{DateTimeOffset.UtcNow.Ticks}_{++_nodeCounter:D6}";
            }
            finally
            {
                _counterSemaphore.Release();
            }
        }
        
        var dataNode = new DataNode
        {
            Id = nodeId,
            ClientInstanceId = _connectionService.ClientInstanceId!
        };

        await TryAddDataNode(dataNode);
    }

    public async Task<bool> TryRemoveDataNode(DataNode dataNode)
    {
        var isRemoveOK = true;
        if (_sessionService.CurrentSession is CloudSession cloudSession
            && dataNode.ClientInstanceId == _connectionService.ClientInstanceId)
        {
            var encryptedDataNode = _dataEncrypter.EncryptDataNode(dataNode);
            isRemoveOK = await _inventoryApiClient.RemoveDataNode(cloudSession.SessionId, _connectionService.ClientInstanceId!, encryptedDataNode);
        }
        
        if (isRemoveOK)
        {
            var associatedDataSources = _dataSourceRepository.Elements
                .Where(ds => ds.DataNodeId == dataNode.Id)
                .ToList();
            
            ApplyRemoveDataNodeLocally(dataNode);
            
            foreach (var dataSource in associatedDataSources)
            {
                _dataSourceService.ApplyRemoveDataSourceLocally(dataSource);
            }
        }

        return isRemoveOK;
    }

    public void ApplyAddDataNodeLocally(DataNode dataNode)
    {
        _dataNodeRepository.AddOrUpdate(dataNode);
        _codeGenerator.RecomputeCodes();
    }

    public void ApplyRemoveDataNodeLocally(DataNode dataNode)
    {
        _dataNodeRepository.Remove(dataNode);
        _codeGenerator.RecomputeCodes();
    }
    
    public async Task ResetNodeCounterAsync()
    {
        await _counterSemaphore.WaitAsync();
        try
        {
            _nodeCounter = 0;
        }
        finally
        {
            _counterSemaphore.Release();
        }
    }
}
