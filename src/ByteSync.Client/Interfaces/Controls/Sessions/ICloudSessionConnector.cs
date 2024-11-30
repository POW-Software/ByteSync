using System.Threading.Tasks;
using ByteSync.Business.Sessions.RunSessionInfos;
using ByteSync.Common.Business.Sessions.Cloud.Connections;

namespace ByteSync.Interfaces.Controls.Sessions;

public interface ICloudSessionConnector
{
    Task<CloudSessionResult?> CreateSession(RunCloudSessionProfileInfo? lobbySessionDetails);

    Task JoinSession(string sessionId, string sessionPassword, RunCloudSessionProfileInfo? lobbySessionDetails);

    Task QuitSession();
        
    IObservable<bool> CanLogOutOrShutdown { get; }
}