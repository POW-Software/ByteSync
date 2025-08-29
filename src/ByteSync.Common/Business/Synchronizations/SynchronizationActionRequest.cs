namespace ByteSync.Common.Business.Synchronizations;

using System.Collections.Generic;

public class SynchronizationActionRequest
{
    public SynchronizationActionRequest()
    {
        ActionsGroupIds = new List<string>();
    }

    public SynchronizationActionRequest(List<string> actionsGroupIds, string? nodeId)
    {
        ActionsGroupIds = actionsGroupIds;
        NodeId = nodeId;
    }

    public List<string> ActionsGroupIds { get; set; }
    public string? NodeId { get; set; }
}
