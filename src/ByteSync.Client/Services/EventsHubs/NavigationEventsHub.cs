using System.Threading.Tasks;
using ByteSync.Business;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Events;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Interfaces.EventsHubs;

namespace ByteSync.Services.EventsHubs;

public class NavigationEventsHub : INavigationEventsHub
{
    public event EventHandler<TrustKeyDataRequestedArgs>? TrustKeyDataRequested;

    public event EventHandler<EventArgs>? CreateCloudSessionProfileRequested;
    
    // public event EventHandler<EventArgs>? CreateLocalSessionProfileRequested;

    public void RaiseTrustKeyDataRequested(PublicKeyCheckData publicKeyCheckData, TrustDataParameters trustDataParameters)
    {
        Task.Run(() => TrustKeyDataRequested?.Invoke(this, new TrustKeyDataRequestedArgs(publicKeyCheckData, trustDataParameters)));
    }

    public async Task RaiseCreateCloudSessionProfileRequested()
    {
        await Task.Run(() => CreateCloudSessionProfileRequested?.Invoke(this, EventArgs.Empty));
    }

    // public async Task RaiseCreateLocalSessionProfileRequested()
    // {
    //     await Task.Run(() => CreateLocalSessionProfileRequested?.Invoke(this, EventArgs.Empty));
    // }
}