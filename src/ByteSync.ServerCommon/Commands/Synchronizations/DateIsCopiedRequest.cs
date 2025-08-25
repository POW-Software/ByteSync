using ByteSync.ServerCommon.Business.Auth;

namespace ByteSync.ServerCommon.Commands.Synchronizations;

public class DateIsCopiedRequest : IActionCompletedRequest
{
    public DateIsCopiedRequest(string sessionId, Client client, List<string> actionsGroupIds)
    {
        SessionId = sessionId;
        Client = client;
        ActionsGroupIds = actionsGroupIds;
    }

    public string SessionId { get; set; }
    public Client Client { get; set; }
    public List<string> ActionsGroupIds { get; set; }
}