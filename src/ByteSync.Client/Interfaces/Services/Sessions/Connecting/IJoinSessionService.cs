using ByteSync.Business.Sessions.RunSessionInfos;

namespace ByteSync.Interfaces.Services.Sessions.Connecting;

public interface IJoinSessionService
{
    Task JoinSession(string sessionId, string sessionPassword, RunCloudSessionProfileInfo? lobbySessionDetails);
    
    Task CancelJoinCloudSession();
}