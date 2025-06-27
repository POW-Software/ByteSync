using ByteSync.Business.Profiles;
using ByteSync.Common.Business.Lobbies;

namespace ByteSync.Business.Sessions.RunSessionInfos;

public class RunCloudSessionProfileInfo : AbstractRunSessionProfileInfo
{
    public RunCloudSessionProfileInfo(string lobbyId, CloudSessionProfile cloudSessionProfile,
        CloudSessionProfileDetails cloudSessionProfileDetails, LobbySessionModes? lobbySessionMode) 
        : base(lobbySessionMode)
    {
        LobbyId = lobbyId;

        Profile = cloudSessionProfile;
        
        ProfileDetails = cloudSessionProfileDetails;
    }
    
    public string LobbyId { get; set; }
    
    public CloudSessionProfile Profile { get; set; }
    
    public CloudSessionProfileDetails ProfileDetails { get; set; }

    public string? LocalProfileClientId
    {
        get
        {
            return Profile.ProfileClientId;
        }
    }

    public List<SessionProfileDataSource> GetMyDataSources()
    {
        var myProfileMember = ProfileDetails.Members.Single(m => m.ProfileClientId.Equals(LocalProfileClientId));

        return myProfileMember.DataSources.OrderBy(pi => pi.Code).ToList();
    }

    public override AbstrastSessionProfileDetails GetProfileDetails()
    {
        return ProfileDetails;
    }

    public override string ProfileName
    {
        get
        {
            return Profile.Name;
        }
    }
}