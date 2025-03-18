using Microsoft.AspNetCore.SignalR.Client;

namespace ByteSync.Interfaces.Factories;

public interface IHubConnectionFactory
{
    Task<HubConnection> BuildConnection();
}