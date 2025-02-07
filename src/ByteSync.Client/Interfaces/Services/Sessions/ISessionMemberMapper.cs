using ByteSync.Business.SessionMembers;
using ByteSync.Common.Business.Sessions.Cloud;

namespace ByteSync.Interfaces.Services.Sessions;

public interface ISessionMemberMapper
{
    SessionMemberInfo Map(SessionMemberInfoDTO sessionMemberInfoDto);
}