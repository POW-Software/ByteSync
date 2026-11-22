using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Security;
using MediatR;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public record BeginJoinSessionRequest(Client Client, AskCloudSessionPasswordExchangeKeyParameters Parameters) : IRequest<List<string>>, ISessionRequest
{
    public string SessionId => Parameters.SessionId;
}
