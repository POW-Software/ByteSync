using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public class FinalizeJoinCloudSessionRequest : IRequest<FinalizeJoinSessionResult>
{
    public FinalizeJoinCloudSessionRequest(FinalizeJoinCloudSessionParameters finalizeJoinCloudSessionParameters, Client client)
    {
        FinalizeJoinCloudSessionParameters = finalizeJoinCloudSessionParameters;
        Client = client;
    }

    public FinalizeJoinCloudSessionParameters FinalizeJoinCloudSessionParameters { get; set; }
    
    public Client Client { get; set; }
}