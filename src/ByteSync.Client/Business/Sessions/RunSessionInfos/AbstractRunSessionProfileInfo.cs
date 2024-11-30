using ByteSync.Business.Profiles;
using ByteSync.Common.Business.Lobbies;
using ByteSync.Common.Helpers;

namespace ByteSync.Business.Sessions.RunSessionInfos;

public abstract class AbstractRunSessionProfileInfo
{
    protected AbstractRunSessionProfileInfo(LobbySessionModes? lobbySessionMode)
    {
        LobbySessionMode = lobbySessionMode;
    }
    
    public abstract AbstrastSessionProfileDetails GetProfileDetails();
    
    public LobbySessionModes? LobbySessionMode { get; set; }

    public abstract string ProfileName { get; }

    public bool AutoStartsInventory
    {
        get
        {
            return LobbySessionMode.In(LobbySessionModes.RunInventory, LobbySessionModes.RunSynchronization);
        }
    }
    
    public bool AutoStartsSynchronization
    {
        get
        {
            return LobbySessionMode.In(LobbySessionModes.RunSynchronization);
        }
    }
}