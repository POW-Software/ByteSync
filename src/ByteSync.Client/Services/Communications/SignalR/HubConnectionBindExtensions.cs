using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using ByteSync.Common.Interfaces.Hub;
using Microsoft.AspNetCore.SignalR.Client;

namespace ByteSync.Services.Communications.SignalR;

/// <summary>
/// https://medium.com/accurx-techblog/type-safety-with-c-clients-and-signalr-dcde5da20624
/// </summary>
static class HubConnectionBindExtensions
{
    public static IDisposable BindOnInterface<T>(this HubConnection connection, 
        Expression<Func<IHubByteSyncPush, Func<T, Task>>> boundMethod, Action<T> handler)
    {
        // var handler2 = async (T a) => { handler.Invoke(a); await Task.FromResult(0); };
        var taskedHandler = (T a) => { Task.Run(() => handler.Invoke(a)); };
            
        return connection.On<T>(_GetMethodName(boundMethod), taskedHandler);
    }

    public static IDisposable BindOnInterface<T1, T2>(this HubConnection connection, 
        Expression<Func<IHubByteSyncPush, Func<T1, T2, Task>>> boundMethod, Action<T1, T2> handler)
    {
        var taskedHandler = (T1 a, T2 b) => { Task.Run(() => handler.Invoke(a, b)); };
        return connection.On<T1, T2>(_GetMethodName(boundMethod), taskedHandler);
    }

    public static IDisposable BindOnInterface<T1, T2, T3>(this HubConnection connection, 
        Expression<Func<IHubByteSyncPush, Func<T1, T2, T3, Task>>> boundMethod, Action<T1, T2, T3> handler)
    {
        var taskedHandler = (T1 a, T2 b, T3 c) => { Task.Run(() => handler.Invoke(a, b, c)); };
        return connection.On<T1, T2, T3>(_GetMethodName(boundMethod), taskedHandler);
    }

    public static IDisposable BindOnInterface<T1, T2, T3, T4>(this HubConnection connection, 
        Expression<Func<IHubByteSyncPush, Func<T1, T2, T3, T4, Task>>> boundMethod, Action<T1, T2, T3, T4> handler)
    {
        var taskedHandler = (T1 a, T2 b, T3 c, T4 d) => { Task.Run(() => handler.Invoke(a, b, c, d)); };
        return connection.On<T1, T2, T3, T4>(_GetMethodName(boundMethod), taskedHandler);
    }

    private static string _GetMethodName<T>(Expression<T> boundMethod)
    {
        var unaryExpression = (UnaryExpression)boundMethod.Body;
        var methodCallExpression = (MethodCallExpression)unaryExpression.Operand;
        var methodInfoExpression = (ConstantExpression)methodCallExpression.Object;
        var methodInfo = (MethodInfo)methodInfoExpression.Value;
        return methodInfo.Name;
    }
}