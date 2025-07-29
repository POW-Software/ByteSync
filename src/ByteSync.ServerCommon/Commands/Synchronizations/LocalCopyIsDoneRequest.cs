using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Synchronizations;

public class LocalCopyIsDoneRequest : IRequest
{
    public LocalCopyIsDoneRequest(string sessionId, Client client, List<string> actionsGroupIds)
    {
        SessionId = sessionId;
        Client = client;
        ActionsGroupIds = actionsGroupIds;
    }

    public string SessionId { get; set; }
    public Client Client { get; set; }
    public List<string> ActionsGroupIds { get; set; }
}