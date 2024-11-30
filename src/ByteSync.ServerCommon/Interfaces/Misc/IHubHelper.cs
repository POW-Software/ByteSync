using System.Runtime.CompilerServices;
using ByteSync.ServerCommon.Business.Auth;
using Microsoft.AspNetCore.SignalR;

namespace ByteSync.ServerCommon.Interfaces.Misc;

public interface IHubHelper<T> where T : class
{
    Task SetHub(Hub<T> hub);

    Task CheckAndExecute(Func<Client, Task> func, bool skipCheck, [CallerMemberName] string caller = "");

    Task CheckAndExecute(Func<Client, Task> func, string sessionId, [CallerMemberName] string caller = "");

    // Task<T2> CheckAndExecute<T2>(Func<ByteSyncEndpoint, Task<T2>> func, bool skipCheck, [CallerMemberName] string caller = "");
    //
    // Task<T2> CheckAndExecute<T2>(Func<ByteSyncEndpoint, Task<T2>> func, string sessionId, [CallerMemberName] string caller = "");

    // Task OnConnectedAsync();
    //
    // Task OnDisconnectedAsync(Exception exception);
    
    
    Task<T2> CheckAndExecute<T2>(Func<Client, Task<T2>> func, bool skipCheck, [CallerMemberName] string caller = "");
    
    Task<T2> CheckAndExecute<T2>(Func<Client, Task<T2>> func, string sessionId, [CallerMemberName] string caller = "");
}