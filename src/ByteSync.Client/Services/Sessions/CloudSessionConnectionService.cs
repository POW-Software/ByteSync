using System.Reactive.Linq;
using System.Reactive.Subjects;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces.Controls.Sessions;

namespace ByteSync.Services.Sessions;

public class CloudSessionConnectionService : ICloudSessionConnectionService
{
    private readonly BehaviorSubject<ConnectionStatuses> _connectionStatus;

    public CloudSessionConnectionService()
    {
        _connectionStatus = new BehaviorSubject<ConnectionStatuses>(ConnectionStatuses.None);
    }
    
    public IObservable<ConnectionStatuses> ConnectionStatusObservable => _connectionStatus.AsObservable();
    
    public ConnectionStatuses CurrentConnectionStatus => _connectionStatus.Value;
    
    public void SetConnectionStatus(ConnectionStatuses connectionStatus)
    {
        _connectionStatus.OnNext(connectionStatus);
    }
}