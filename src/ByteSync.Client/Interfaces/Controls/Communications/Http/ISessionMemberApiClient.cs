using System.Threading;
using System.Threading.Tasks;
using ByteSync.Common.Business.Sessions.Cloud;

namespace ByteSync.Interfaces.Controls.Communications.Http;

public interface ISessionMemberApiClient
{
    Task<List<string>> GetMembersClientInstanceIds(string sessionId, CancellationToken cancellationToken = default);
    
    Task<List<SessionMemberInfoDTO>> GetMembers(string sessionId);
    
    Task UpdateSessionMemberGeneralStatus(UpdateSessionMemberGeneralStatusParameters sessionMemberGeneralStatusParameters);
}