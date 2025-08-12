using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public class AskJoinCloudSessionRequest : IRequest<JoinSessionResult>
{
    public AskJoinCloudSessionRequest(Client client, AskJoinCloudSessionParameters parameters)
    {
        Client = client;
        Parameters = parameters;
    }
    
    public Client Client { get; }

    public AskJoinCloudSessionParameters Parameters { get; }
} 