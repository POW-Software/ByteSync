using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Security;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using MediatR;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public class ValidateJoinCloudSessionRequest : IRequest<Unit>, ISessionRequest
{
    public string SessionId => Parameters.SessionId;

    public Client? Client { get; }

    public ValidateJoinCloudSessionRequest(ValidateJoinCloudSessionParameters parameters, Client? client = null)
    {
        Client = client;
        Parameters = parameters;
    }
    
    public ValidateJoinCloudSessionParameters Parameters { get; }
} 