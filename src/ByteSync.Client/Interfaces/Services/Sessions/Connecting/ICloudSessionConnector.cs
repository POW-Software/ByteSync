using System.Threading.Tasks;
using ByteSync.Business.Sessions;
using ByteSync.Business.Sessions.Connecting;
using ByteSync.Business.Sessions.RunSessionInfos;
using ByteSync.Common.Business.Sessions.Cloud.Connections;

namespace ByteSync.Interfaces.Services.Sessions.Connecting;

public interface ICloudSessionConnector
{
    public Task ClearConnectionData();
    
    IObservable<bool> CanLogOutOrShutdown { get; }
    
    Task InitializeConnection(SessionConnectionStatus creatingSession);
    
    public Task OnJoinSessionError(JoinSessionResult joinSessionResult);
    
    Task OnCreateSessionError(CreateSessionError createSessionError);
}