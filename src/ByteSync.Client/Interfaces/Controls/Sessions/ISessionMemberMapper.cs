using ByteSync.Business.SessionMembers;
using ByteSync.Common.Business.Sessions.Cloud;

namespace ByteSync.Interfaces.Controls.Sessions;

public interface ISessionMemberMapper
{
    SessionMemberInfo Map(SessionMemberInfoDTO sessionMemberInfoDto);
}