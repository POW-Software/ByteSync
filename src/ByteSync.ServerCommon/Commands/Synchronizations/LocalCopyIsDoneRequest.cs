using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Synchronizations;

public class LocalCopyIsDoneRequest : IActionCompletedRequest
{
    public LocalCopyIsDoneRequest(string sessionId, Client client, List<string> actionsGroupIds, string? nodeId)
    {
        SessionId = sessionId;
        Client = client;
        ActionsGroupIds = actionsGroupIds;
        NodeId = nodeId;
    }

    public string SessionId { get; set; }
    public Client Client { get; set; }
    public List<string> ActionsGroupIds { get; set; }
    public string? NodeId { get; set; }
}