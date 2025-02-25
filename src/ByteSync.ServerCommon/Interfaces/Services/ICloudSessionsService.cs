using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.ServerCommon.Business.Auth;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface ICloudSessionsService
{
    Task<CloudSessionResult> CreateCloudSession(CreateCloudSessionParameters createCloudSessionParameters, Client creator);

    Task<List<string>> GetMembersInstanceIds(string sessionId);
    
    Task<JoinSessionResult> AskCloudSessionPasswordExchangeKey(Client client, AskCloudSessionPasswordExchangeKeyParameters parameters);

    Task<JoinSessionResult> AskJoinCloudSession(Client client, AskJoinCloudSessionParameters parameters);
    
    Task ValidateJoinCloudSession(ValidateJoinCloudSessionParameters parameters);
        
    Task<FinalizeJoinSessionResult> FinalizeJoinCloudSession(Client client, FinalizeJoinCloudSessionParameters parameters);

    // Task QuitCloudSession(Client client, string sessionId);
        
    Task<List<SessionMemberInfoDTO>> GetSessionMembersInfosAsync(string sessionId);
        
    Task UpdateSessionSettings(Client client, string sessionId, EncryptedSessionSettings sessionSettings);
    
    Task<bool> ResetSession(string sessionId, Client client);
    
    Task GiveCloudSessionPasswordExchangeKey(Client client, GiveCloudSessionPasswordExchangeKeyParameters parameters);

    Task InformPasswordIsWrong(Client client, string sessionId, string clientInstanceId);
}