using ByteSync.Business.Sessions;

namespace ByteSync.Business.Profiles;

public class CloudSessionProfileOptions
{
    public SessionSettings Settings { get; set; } = null!;

    public TimeSpan? MaxLobbyLifeTime
    {
        get
        {
            return TimeSpan.FromMinutes(5);
        }
    }
}