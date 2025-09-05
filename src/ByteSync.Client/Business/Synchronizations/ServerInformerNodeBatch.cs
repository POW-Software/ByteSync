namespace ByteSync.Business.Synchronizations;

public class ServerInformerNodeBatch
{
    public ServerInformerNodeBatch(string? nodeId)
    {
        NodeId = nodeId;
        ActionsGroupIds = new HashSet<string>();
        CreationDate = DateTime.Now;
    }

    public string? NodeId { get; }
    
    public HashSet<string> ActionsGroupIds { get; }
    
    public DateTime CreationDate { get; }
}