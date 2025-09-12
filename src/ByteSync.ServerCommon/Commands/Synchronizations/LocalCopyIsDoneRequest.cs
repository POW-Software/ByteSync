using ByteSync.ServerCommon.Business.Auth;
using MediatR;
using ByteSync.Common.Business.Synchronizations;

namespace ByteSync.ServerCommon.Commands.Synchronizations;

public class LocalCopyIsDoneRequest : IActionCompletedRequest
{
    public LocalCopyIsDoneRequest(string sessionId, Client client, List<string> actionsGroupIds, string? nodeId)
        : this(sessionId, client, actionsGroupIds, nodeId, null)
    {
    }

    public LocalCopyIsDoneRequest(string sessionId, Client client, List<string> actionsGroupIds, string? nodeId,
        Dictionary<string, SynchronizationActionMetrics>? actionMetricsByActionId)
    {
        SessionId = sessionId;
        Client = client;
        ActionsGroupIds = actionsGroupIds;
        NodeId = nodeId;
        ActionMetricsByActionId = actionMetricsByActionId;
    }

    public string SessionId { get; set; }
    public Client Client { get; set; }
    public List<string> ActionsGroupIds { get; set; }
    public string? NodeId { get; set; }
    public Dictionary<string, SynchronizationActionMetrics>? ActionMetricsByActionId { get; set; }
}
