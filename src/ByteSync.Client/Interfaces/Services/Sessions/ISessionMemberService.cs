using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;

namespace ByteSync.Interfaces.Services.Sessions;

public interface ISessionMemberService
{
    Task UpdateCurrentMemberGeneralStatus(SessionMemberGeneralStatus sessionMemberGeneralStatus);
    
    void AddOrUpdate(List<SessionMemberInfoDTO> sessionMemberInfoDtos);
}