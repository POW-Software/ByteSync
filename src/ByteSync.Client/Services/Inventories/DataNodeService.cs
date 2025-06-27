using ByteSync.Business.DataNodes;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Inventories;

public class DataNodeService : IDataNodeService
{
    private readonly ISessionService _sessionService;
    private readonly IConnectionService _connectionService;
    private readonly IInventoryApiClient _inventoryApiClient;
    private readonly IDataNodeRepository _dataNodeRepository;

    public DataNodeService(ISessionService sessionService, IConnectionService connectionService,
        IInventoryApiClient inventoryApiClient, IDataNodeRepository dataNodeRepository)
    {
        _sessionService = sessionService;
        _connectionService = connectionService;
        _inventoryApiClient = inventoryApiClient;
        _dataNodeRepository = dataNodeRepository;
    }

    public async Task<bool> TryAddDataNode(DataNode dataNode)
    {
        var isAddOK = true;
        if (_sessionService.CurrentSession is CloudSession cloudSession
            && dataNode.ClientInstanceId == _connectionService.ClientInstanceId)
        {
            isAddOK = await _inventoryApiClient.AddDataNode(cloudSession.SessionId, dataNode.NodeId);
        }

        if (isAddOK)
        {
            ApplyAddDataNodeLocally(dataNode);
        }

        return isAddOK;
    }

    public Task CreateAndTryAddDataNode(string nodeId)
    {
        var dataNode = new DataNode
        {
            NodeId = nodeId,
            ClientInstanceId = _connectionService.ClientInstanceId!
        };

        return TryAddDataNode(dataNode);
    }

    public async Task<bool> TryRemoveDataNode(DataNode dataNode)
    {
        var isRemoveOK = true;
        if (_sessionService.CurrentSession is CloudSession cloudSession
            && dataNode.ClientInstanceId == _connectionService.ClientInstanceId)
        {
            isRemoveOK = await _inventoryApiClient.RemoveDataNode(cloudSession.SessionId, dataNode.NodeId);
        }

        if (isRemoveOK)
        {
            ApplyRemoveDataNodeLocally(dataNode);
        }

        return isRemoveOK;
    }

    public void ApplyAddDataNodeLocally(DataNode dataNode)
    {
        _dataNodeRepository.AddOrUpdate(dataNode);
    }

    public void ApplyRemoveDataNodeLocally(DataNode dataNode)
    {
        _dataNodeRepository.Remove(dataNode);
    }
}
