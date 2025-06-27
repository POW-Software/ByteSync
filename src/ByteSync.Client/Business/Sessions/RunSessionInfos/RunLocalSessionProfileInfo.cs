using ByteSync.Business.DataSources;
using ByteSync.Business.Profiles;
using ByteSync.Common.Business.Lobbies;

namespace ByteSync.Business.Sessions.RunSessionInfos;

public class RunLocalSessionProfileInfo : AbstractRunSessionProfileInfo
{
    public RunLocalSessionProfileInfo(LocalSessionProfile cloudSessionProfile, 
        LocalSessionProfileDetails localSessionProfileDetails, LobbySessionModes? lobbySessionMode) 
        : base(lobbySessionMode)
    {
        Profile = cloudSessionProfile;
        
        ProfileDetails = localSessionProfileDetails;
    }

    public LocalSessionProfile Profile { get; set; }
    
    public LocalSessionProfileDetails ProfileDetails { get; set; }

    public IList<DataSource> GetMyDataSources()
    {
        return ProfileDetails.DataSources;
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