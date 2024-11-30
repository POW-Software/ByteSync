using System.Threading.Tasks;
using ByteSync.Business.Communications;
using ByteSync.Common.Business.EndPoints;
using Microsoft.AspNetCore.SignalR.Client;

namespace ByteSync.Interfaces.Services.Communications;

public interface IConnectionService
{
    public IObservable<ConnectionStatuses> ConnectionStatus { get; }
    
    public ConnectionStatuses CurrentConnectionStatus { get; }
    
    public IObservable<HubConnection?> Connection { get; }
    
    ByteSyncEndpoint? CurrentEndPoint { get; set; }
    
    string? ClientInstanceId { get; }
    
    Task StartConnectionAsync();
    
    Task StopConnection();
}