using System.Threading.Tasks;
using ByteSync.Business.Sessions.RunSessionInfos;
using ByteSync.Common.Business.Sessions.Cloud.Connections;

namespace ByteSync.Interfaces.Services.Sessions.Connecting;

public interface ICloudSessionConnector
{
    // Task<CloudSessionResult?> CreateSession(RunCloudSessionProfileInfo? lobbySessionDetails);

    Task JoinSession(string sessionId, string sessionPassword, RunCloudSessionProfileInfo? lobbySessionDetails);

    // Task QuitSession();

    public Task ClearConnectionData();

    public Task OnJoinSessionError(JoinSessionResult joinSessionResult);
    
    IObservable<bool> CanLogOutOrShutdown { get; }
}