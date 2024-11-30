using System.Threading.Tasks;
using ByteSync.Common.Business.Sessions.Cloud;

namespace ByteSync.Interfaces.Controls.Communications.Http;

public interface ISessionMemberApiClient
{
    Task UpdateSessionMemberGeneralStatus(UpdateSessionMemberGeneralStatusParameters sessionMemberGeneralStatusParameters);
}