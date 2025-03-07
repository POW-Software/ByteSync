using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public class CreateSessionRequest : IRequest<CloudSessionResult>
{
    public CreateSessionRequest(CreateCloudSessionParameters createCloudSessionParameters, Client client)
    {
        CreateCloudSessionParameters = createCloudSessionParameters;
        Client = client;
    }

    public CreateCloudSessionParameters CreateCloudSessionParameters { get; set; }
    
    public Client Client { get; set; }
}