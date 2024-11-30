using ByteSync.Common.Interfaces.Hub;
using Microsoft.Azure.SignalR.Management;

namespace ByteSync.ServerCommon.Interfaces.Factories;

public interface IHubContextFactory
{
    // Task<ServiceHubContext<IHubByteSyncPush>> CreateHubContext();
   ServiceHubContext<IHubByteSyncPush> CreateHubContext();
}