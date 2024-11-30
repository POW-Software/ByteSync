using ByteSync.Business.Sessions;

namespace ByteSync.Interfaces.Controls.Sessions;

public interface ICloudSessionConnectionService
{
    public IObservable<ConnectionStatuses> ConnectionStatusObservable { get; }
    
    public ConnectionStatuses CurrentConnectionStatus { get; }
    
    void SetConnectionStatus(ConnectionStatuses connectionStatus);
}