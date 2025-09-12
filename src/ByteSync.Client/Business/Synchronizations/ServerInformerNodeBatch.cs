namespace ByteSync.Business.Synchronizations;

public class ServerInformerNodeBatch
{
    public ServerInformerNodeBatch(string? nodeId)
    {
        NodeId = nodeId;
        ActionsGroupIds = new HashSet<string>();
        CreationDate = DateTime.Now;
        ActionMetricsByActionId = new Dictionary<string, Common.Business.Synchronizations.SynchronizationActionMetrics>();
    }

    public string? NodeId { get; }
    
    public HashSet<string> ActionsGroupIds { get; }
    
    public DateTime CreationDate { get; }

    public Dictionary<string, Common.Business.Synchronizations.SynchronizationActionMetrics> ActionMetricsByActionId { get; }
}
