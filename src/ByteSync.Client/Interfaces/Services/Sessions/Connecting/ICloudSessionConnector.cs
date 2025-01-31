using System.Threading.Tasks;
using ByteSync.Business.Sessions.RunSessionInfos;
using ByteSync.Common.Business.Sessions.Cloud.Connections;

namespace ByteSync.Interfaces.Services.Sessions.Connecting;

public interface ICloudSessionConnector
{
    public Task ClearConnectionData();

    public Task OnJoinSessionError(JoinSessionResult joinSessionResult);
    
    IObservable<bool> CanLogOutOrShutdown { get; }
}