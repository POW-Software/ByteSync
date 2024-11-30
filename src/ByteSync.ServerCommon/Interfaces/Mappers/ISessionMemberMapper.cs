using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.ServerCommon.Business.Sessions;

namespace ByteSync.ServerCommon.Interfaces.Mappers;

public interface ISessionMemberMapper
{
    Task<SessionMemberInfoDTO> Convert(SessionMemberData sessionMemberData);
}