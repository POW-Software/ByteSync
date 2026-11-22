using ByteSync.ServerCommon.Security;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public class FinalizeJoinCloudSessionRequest : IRequest<FinalizeJoinSessionResult>, ISessionRequest
{
    public string SessionId => FinalizeJoinCloudSessionParameters.SessionId;

    public FinalizeJoinCloudSessionRequest(FinalizeJoinCloudSessionParameters finalizeJoinCloudSessionParameters, Client client)
    {
        FinalizeJoinCloudSessionParameters = finalizeJoinCloudSessionParameters;
        Client = client;
    }

    public FinalizeJoinCloudSessionParameters FinalizeJoinCloudSessionParameters { get; set; }
    
    public Client Client { get; set; }
}