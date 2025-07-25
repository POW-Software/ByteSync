using System.Threading;
using System.Threading.Tasks;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Sessions.Cloud.Connections;

namespace ByteSync.Interfaces.Controls.Communications.Http;

public interface ICloudSessionApiClient
{
    Task<CloudSessionResult> CreateCloudSession(CreateCloudSessionParameters parameters, CancellationToken cancellationToken = default);
    
    Task QuitCloudSession(string sessionId);

    Task ResetCloudSession(string sessionId);
    
    Task<JoinSessionResult> AskPasswordExchangeKey(AskCloudSessionPasswordExchangeKeyParameters parameters, 
        CancellationToken cancellationToken = default);
    
    Task<FinalizeJoinSessionResult> FinalizeJoinCloudSession(FinalizeJoinCloudSessionParameters parameters,
        CancellationToken cancellationToken = default);
    
    Task GiveCloudSessionPasswordExchangeKey(GiveCloudSessionPasswordExchangeKeyParameters parameters,
        CancellationToken cancellationToken = default);
    
    Task<JoinSessionResult> AskJoinCloudSession(AskJoinCloudSessionParameters parameters,
        CancellationToken cancellationToken = default);
    
    Task ValidateJoinCloudSession(ValidateJoinCloudSessionParameters parameters);
    
    Task InformPasswordIsWrong(string sessionId, string joinerInstanceId);
    
    Task UpdateSettings(string sessionId, EncryptedSessionSettings encryptedSessionSettings);
}