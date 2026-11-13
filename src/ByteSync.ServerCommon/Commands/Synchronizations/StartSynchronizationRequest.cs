using ByteSync.ServerCommon.Security;
using ByteSync.Common.Business.Actions;
using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Synchronizations;

public class StartSynchronizationRequest : IRequest, ISessionRequest
{
    public StartSynchronizationRequest(string sessionId, Client client, List<ActionsGroupDefinition> actionsGroupDefinitions)
    {
        SessionId = sessionId;
        Client = client;
        ActionsGroupDefinitions = actionsGroupDefinitions;
    }

    public string SessionId { get; set; }
    public Client Client { get; set; }
    public List<ActionsGroupDefinition> ActionsGroupDefinitions { get; set; }
}