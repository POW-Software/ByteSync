using ByteSync.Interfaces.Controls.Synchronizations;

namespace ByteSync.Business.Synchronizations;

public class ServerInformerOperatorInfo
{
    public ServerInformerOperatorInfo(ISynchronizationActionServerInformer.CloudActionCaller cloudActionCaller, string? nodeId = null)
    {
        CloudActionCaller = cloudActionCaller;
        // ActionsGroupDefinitions = new List<ActionsGroupDefinition>();
        ActionsGroupIds = new List<string>();
        NodeId = nodeId;
        
        CreationDate = DateTime.Now;
    }

    public ISynchronizationActionServerInformer.CloudActionCaller CloudActionCaller { get; }

    // public List<ActionsGroupDefinition> ActionsGroupDefinitions { get; }
    
    public List<string> ActionsGroupIds { get; }

    public string? NodeId { get; set; }
    
    public DateTime CreationDate
    {
        get;
    }

    public int ActionsCount
    {
        get
        {
            return ActionsGroupIds.Count;
        } 
    }

    // public void Add(ActionsGroupDefinition actionsGroupDefinition)
    // {
    //     // ActionsGroupDefinitions.Add(actionsGroupDefinition);
    //     ActionsGroupIds.Add(actionsGroupDefinition.ActionsGroupId);
    // }
    
    public void Add(string actionsGroupdId)
    {
        ActionsGroupIds.Add(actionsGroupdId);
    }
    
    public void Add(List<string> actionsGroupdIds)
    {
        ActionsGroupIds.AddAll(actionsGroupdIds);
    }
}