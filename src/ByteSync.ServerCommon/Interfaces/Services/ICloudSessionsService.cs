using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface ICloudSessionsService
{
    Task<CloudSessionResult> BuildCloudSessionResult(CloudSessionData cloudSessionData, SessionMemberData sessionMemberData);

    Task<List<string>> GetMembersInstanceIds(string sessionId);
    
    Task<JoinSessionResult> AskCloudSessionPasswordExchangeKey(Client client, AskCloudSessionPasswordExchangeKeyParameters parameters);

    Task<JoinSessionResult> AskJoinCloudSession(Client client, AskJoinCloudSessionParameters parameters);
    
    Task ValidateJoinCloudSession(ValidateJoinCloudSessionParameters parameters);

    Task<List<SessionMemberInfoDTO>> GetSessionMembersInfosAsync(string sessionId);
    
    Task<bool> ResetSession(string sessionId, Client client);
    
    Task GiveCloudSessionPasswordExchangeKey(Client client, GiveCloudSessionPasswordExchangeKeyParameters parameters);

    Task InformPasswordIsWrong(Client client, string sessionId, string clientInstanceId);
}