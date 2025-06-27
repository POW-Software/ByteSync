using ByteSync.Business.DataNodes;

namespace ByteSync.Interfaces.Controls.Inventories;

public interface IDataNodeService
{
    Task<bool> TryAddDataNode(DataNode dataNode);

    Task CreateAndTryAddDataNode(string nodeId);

    void ApplyAddDataNodeLocally(DataNode dataNode);

    Task<bool> TryRemoveDataNode(DataNode dataNode);

    void ApplyRemoveDataNodeLocally(DataNode dataNode);
}
