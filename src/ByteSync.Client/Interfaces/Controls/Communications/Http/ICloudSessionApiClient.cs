using System.Threading.Tasks;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Sessions.Cloud.Connections;

namespace ByteSync.Interfaces.Controls.Communications.Http;

public interface ICloudSessionApiClient
{
    Task<CloudSessionResult> CreateCloudSession(CreateCloudSessionParameters parameters);
    
    Task QuitCloudSession(string sessionId);

    Task ResetCloudSession(string sessionId);
    
    Task<List<string>> GetMembersClientInstanceIds(string sessionId);
    
    Task<JoinSessionResult> AskPasswordExchangeKey(AskCloudSessionPasswordExchangeKeyParameters parameters);
    
    Task<FinalizeJoinSessionResult> FinalizeJoinCloudSession(FinalizeJoinCloudSessionParameters parameters);
    
    Task GiveCloudSessionPasswordExchangeKey(GiveCloudSessionPasswordExchangeKeyParameters parameters);
    
    Task<JoinSessionResult> AskJoinCloudSession(AskJoinCloudSessionParameters parameters);
    
    Task ValidateJoinCloudSession(ValidateJoinCloudSessionParameters parameters);
    
    Task InformPasswordIsWrong(string sessionId, string joinerInstanceId);
    
    Task UpdateSettings(string sessionId, EncryptedSessionSettings encryptedSessionSettings);

    Task<List<SessionMemberInfoDTO>> GetMembers(string sessionId);
}