using ByteSync.Common.Business.Synchronizations;
using ByteSync.Interfaces.Controls.Synchronizations;

namespace ByteSync.Business.Synchronizations;

public class ServerInformerData
{
    private readonly Dictionary<string, ServerInformerNodeBatch> _batches;

    public ServerInformerData(ISynchronizationActionServerInformer.CloudActionCaller cloudActionCaller)
    {
        CloudActionCaller = cloudActionCaller;
        SynchronizationActionRequests = new List<SynchronizationActionRequest>();
        _batches = new Dictionary<string, ServerInformerNodeBatch>();
    }

    public ISynchronizationActionServerInformer.CloudActionCaller CloudActionCaller { get; }

    public List<SynchronizationActionRequest> SynchronizationActionRequests { get; }

    public void Add(List<string> actionsGroupIds, string? nodeId)
    {
        var key = nodeId ?? string.Empty;
        if (!_batches.TryGetValue(key, out var batch))
        {
            batch = new ServerInformerNodeBatch(nodeId);
            _batches[key] = batch;
        }

        foreach (var id in actionsGroupIds)
        {
            batch.ActionsGroupIds.Add(id);
        }
    }

    public IEnumerable<ServerInformerData> ExtractReadySlices(int readyCountThreshold, TimeSpan maxAge, int chunkSize)
    {
        var now = DateTime.Now;
        var readyKeys = new List<string>();
        foreach (var pair in _batches)
        {
            var batch = pair.Value;
            if (batch.ActionsGroupIds.Count >= readyCountThreshold || (maxAge > TimeSpan.Zero && (now - batch.CreationDate) > maxAge))
            {
                readyKeys.Add(pair.Key);
            }
        }

        var result = new List<ServerInformerData>();
        foreach (var key in readyKeys)
        {
            var batch = _batches[key];
            result.Add(BuildSlice(batch, chunkSize));
            _batches.Remove(key);
        }

        return result;
    }

    public IEnumerable<ServerInformerData> ExtractAllSlices(int chunkSize)
    {
        var result = new List<ServerInformerData>();
        foreach (var batch in _batches.Values)
        {
            result.Add(BuildSlice(batch, chunkSize));
        }
        _batches.Clear();
        return result;
    }

    private ServerInformerData BuildSlice(ServerInformerNodeBatch batch, int chunkSize)
    {
        var slice = new ServerInformerData(CloudActionCaller);
        var ids = batch.ActionsGroupIds.ToList();
        foreach (var chunk in ids.Chunk(chunkSize))
        {
            slice.SynchronizationActionRequests.Add(new SynchronizationActionRequest(chunk.ToList(), batch.NodeId));
        }
        return slice;
    }
}

