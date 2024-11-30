using System.Threading.Tasks;
using ByteSync.Business.Lobbies;
using ByteSync.Business.Profiles;
using ByteSync.Business.Sessions.RunSessionInfos;
using ByteSync.Common.Business.Lobbies;
using ByteSync.Common.Interfaces;

namespace ByteSync.Interfaces.Lobbies;

public interface ILobbyRepository : IRepository<LobbyDetails>
{
    Task SetCloudSessionProfileDetails(CloudSessionProfile cloudSessionProfile, ByteSync.Business.Profiles.CloudSessionProfileDetails sessionProfileDetails,
        LobbyInfo? lobbyInfo);
    
    Task SetTrustCheckSuccess(string lobbyId);
    
    Task SetTrustCheckError(string lobbyId);

    Task UpdateLobbyMemberStatus(string lobbyId, LobbyMemberStatuses securityChecksInProgress);
    
    Task<bool> IsFirstLobbyMember(string lobbyId);
    
    Task SetExpectedMember(LobbySessionExpectedMember lobbySessionExpectedMember);
    
    Task<RunCloudSessionProfileInfo> BuildCloudProfileSessionDetails(string lobbyId);

    Task<bool> AreAllOtherMembersCheckSuccess(string lobbyId);
    
    Task<bool> IsEverythingOKBeforeSession(string lobbyId);
}