using ByteSync.Common.Business.Synchronizations;
using ByteSync.Interfaces.Controls.Synchronizations;

namespace ByteSync.Business.Synchronizations;

public class ServerInformerOperatorInfo
{
    private class NodeBatch
    {
        public NodeBatch(string? nodeId)
        {
            NodeId = nodeId;
            ActionsGroupIds = new HashSet<string>();
            CreationDate = DateTime.Now;
        }

        public string? NodeId { get; }
        public HashSet<string> ActionsGroupIds { get; }
        public DateTime CreationDate { get; }
    }

    private readonly Dictionary<string, NodeBatch> _byNode;

    public ServerInformerOperatorInfo(ISynchronizationActionServerInformer.CloudActionCaller cloudActionCaller)
    {
        CloudActionCaller = cloudActionCaller;
        SynchronizationActionRequests = new List<SynchronizationActionRequest>();
        _byNode = new Dictionary<string, NodeBatch>();
    }

    public ISynchronizationActionServerInformer.CloudActionCaller CloudActionCaller { get; }

    public List<SynchronizationActionRequest> SynchronizationActionRequests { get; }

    public void Add(List<string> actionsGroupIds, string? nodeId)
    {
        var key = nodeId ?? string.Empty;
        if (!_byNode.TryGetValue(key, out var batch))
        {
            batch = new NodeBatch(nodeId);
            _byNode[key] = batch;
        }

        foreach (var id in actionsGroupIds)
        {
            batch.ActionsGroupIds.Add(id);
        }
    }

    public IEnumerable<ServerInformerOperatorInfo> ExtractReadySlices(int readyCountThreshold, TimeSpan maxAge, int chunkSize)
    {
        var now = DateTime.Now;
        var readyKeys = new List<string>();
        foreach (var kv in _byNode)
        {
            var batch = kv.Value;
            if (batch.ActionsGroupIds.Count >= readyCountThreshold || (maxAge > TimeSpan.Zero && (now - batch.CreationDate) > maxAge))
            {
                readyKeys.Add(kv.Key);
            }
        }

        var result = new List<ServerInformerOperatorInfo>();
        foreach (var key in readyKeys)
        {
            var batch = _byNode[key];
            result.Add(BuildSlice(batch, chunkSize));
            _byNode.Remove(key);
        }

        return result;
    }

    public IEnumerable<ServerInformerOperatorInfo> ExtractAllSlices(int chunkSize)
    {
        var result = new List<ServerInformerOperatorInfo>();
        foreach (var batch in _byNode.Values)
        {
            result.Add(BuildSlice(batch, chunkSize));
        }
        _byNode.Clear();
        return result;
    }

    private ServerInformerOperatorInfo BuildSlice(NodeBatch batch, int chunkSize)
    {
        var slice = new ServerInformerOperatorInfo(CloudActionCaller);
        var ids = batch.ActionsGroupIds.ToList();
        foreach (var chunk in ids.Chunk(chunkSize))
        {
            slice.SynchronizationActionRequests.Add(new SynchronizationActionRequest(chunk.ToList(), batch.NodeId));
        }
        return slice;
    }
}

