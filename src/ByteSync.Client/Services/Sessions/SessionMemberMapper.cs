using ByteSync.Business.SessionMembers;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Services.Sessions;

namespace ByteSync.Services.Sessions;

public class SessionMemberMapper : ISessionMemberMapper
{
    private readonly IDataEncrypter _dataEncrypter;

    public SessionMemberMapper(IDataEncrypter dataEncrypter)
    {
        _dataEncrypter = dataEncrypter;
    }
    
    public SessionMember Map(SessionMemberInfoDTO sessionMemberInfoDto)
    {
        var sessionMemberInfo = new SessionMember
        {
            Endpoint = sessionMemberInfoDto.Endpoint,
            SessionId = sessionMemberInfoDto.SessionId,
            JoinedSessionOn = sessionMemberInfoDto.JoinedSessionOn,
            PositionInList = sessionMemberInfoDto.PositionInList,
            LobbyId = sessionMemberInfoDto.LobbyId,
            LastLocalInventoryGlobalStatusUpdate = sessionMemberInfoDto.LastLocalInventoryGlobalStatusUpdate,
            SessionMemberGeneralStatus = sessionMemberInfoDto.SessionMemberGeneralStatus,
            ProfileClientId = sessionMemberInfoDto.ProfileClientId,
            PrivateData = _dataEncrypter.DecryptSessionMemberPrivateData(sessionMemberInfoDto.EncryptedPrivateData)
        };
        
        return sessionMemberInfo;
    }
}