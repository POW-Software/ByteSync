using System.Threading.Tasks;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;

namespace ByteSync.Interfaces.Controls.Sessions;

public interface ISessionMemberService
{
    Task UpdateCurrentMemberGeneralStatus(SessionMemberGeneralStatus sessionMemberGeneralStatus);
    
    void AddOrUpdate(List<SessionMemberInfoDTO> sessionMemberInfoDtos);
}