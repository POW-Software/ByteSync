using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;

namespace ByteSync.ServerCommon.Commands.Synchronizations;

public class SynchronizationErrorRequest : IActionErrorRequest
{
    public SynchronizationErrorRequest(string sessionId, Client client, SharedFileDefinition sharedFileDefinition)
    {
        SessionId = sessionId;
        Client = client;
        SharedFileDefinition = sharedFileDefinition;
    }

    public string SessionId { get; set; }
    public Client Client { get; set; }
    public SharedFileDefinition SharedFileDefinition { get; set; }
}