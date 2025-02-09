using ByteSync.Business.Sessions;
using ByteSync.Business.Sessions.Connecting;
using ByteSync.Common.Business.Sessions.Cloud.Connections;

namespace ByteSync.Interfaces.Services.Sessions.Connecting;

public interface ICloudSessionConnectionService
{
    IObservable<bool> CanLogOutOrShutdown { get; }
    
    Task ClearConnectionData();
    
    Task InitializeConnection(SessionConnectionStatus creatingSession);
    
    Task OnJoinSessionError(JoinSessionError joinSessionError);
    
    Task OnCreateSessionError(CreateSessionError createSessionError);
    
    Task HandleJoinSessionError(Exception exception);
}