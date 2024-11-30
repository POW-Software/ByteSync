using System.Reactive.Linq;
using System.Threading.Tasks;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications.SignalR;
using ByteSync.Interfaces.Services.Communications;
using Microsoft.AspNetCore.SignalR.Client;
using Polly.Retry;
using Serilog;

namespace ByteSync.Services.Communications.SignalR;

public class HubInvoker : IHubInvoker
{
    private readonly IPolicyFactory _policyFactory;

    public HubInvoker(IConnectionService connectionService, IPolicyFactory policyFactory)
    {
        connectionService.Connection.Where(c => c != null)
            .Subscribe(c => Connection = c!);
        
        _policyFactory = policyFactory;
    }
    
    private HubConnection Connection { get; set; }

    public async Task Invoke(string methodName)
    {
        var policy = BuildPolicy();

        var attempt = 0;
        await policy.ExecuteAsync(async () =>
            {
                Log.Debug("{Caller}: Attempt {Attempt}", methodName, ++attempt);
                await Connection.InvokeAsync(methodName);
            }
        );
    }
    
    public async Task Invoke(string methodName, object? arg1)
    {
        var policy = BuildPolicy();

        var attempt = 0;
        await policy.ExecuteAsync(async () =>
            {
                Log.Debug("{Caller}: Attempt {Attempt}", methodName, ++attempt);
                await Connection.InvokeAsync(methodName, arg1);
            }
        );
    }
    
    public async Task Invoke(string methodName, object? arg1, object? arg2)
    {
        var policy = BuildPolicy();

        var attempt = 0;
        await policy.ExecuteAsync(async () =>
            {
                Log.Debug("{Caller}: Attempt {Attempt}", methodName, ++attempt);
                await Connection.InvokeAsync(methodName, arg1, arg2);
            }
        );
    }
    
    public async Task Invoke(string methodName, object? arg1, object? arg2, object? arg3)
    {
        var policy = BuildPolicy();

        var attempt = 0;
        await policy.ExecuteAsync(async () =>
            {
                Log.Debug("{Caller}: Attempt {Attempt}", methodName, ++attempt);
                await Connection.InvokeAsync(methodName, arg1, arg2, arg3);
            }
        );
    }
    
    public async Task Invoke(string methodName, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        var policy = BuildPolicy();

        var attempt = 0;
        await policy.ExecuteAsync(async () =>
            {
                Log.Debug("{Caller}: Attempt {Attempt}", methodName, ++attempt);
                await Connection.InvokeAsync(methodName, arg1, arg2, arg3, arg4);
            }
        );
    }
    
    public async Task<T> Invoke<T>(string methodName)
    {
        var policy = BuildPolicy();

        var attempt = 0;
        // T result = default;
        return await policy.ExecuteAsync(async () =>
            {
                Log.Debug("{Caller}: Attempt {Attempt}", methodName, ++attempt);
                return await Connection.InvokeAsync<T>(methodName);
            }
        );
    }
    
    public async Task<T> Invoke<T>(string methodName, object? arg1)
    {
        var policy = BuildPolicy();

        var attempt = 0;
        // T result = default;
        return await policy.ExecuteAsync(async () =>
            {
                Log.Debug("{Caller}: Attempt {Attempt}", methodName, ++attempt);
                return await Connection.InvokeAsync<T>(methodName, arg1);
            }
        );
    }
    
    public async Task<T> Invoke<T>(string methodName, object? arg1, object? arg2)
    {
        var policy = BuildPolicy();

        var attempt = 0;
        // T result = default;
        return await policy.ExecuteAsync(async () =>
            {
                Log.Debug("{Caller}: Attempt {Attempt}", methodName, ++attempt);
                return await Connection.InvokeAsync<T>(methodName, arg1, arg2);
            }
        );
    }
    
    public async Task<T> Invoke<T>(string methodName, object? arg1, object? arg2, object? arg3)
    {
        var policy = BuildPolicy();

        var attempt = 0;
        // T result = default;
        return await policy.ExecuteAsync(async () =>
            {
                Log.Debug("{Caller}: Attempt {Attempt}", methodName, ++attempt);
                return await Connection.InvokeAsync<T>(methodName, arg1, arg2, arg3);
            }
        );
    }
    
    public async Task<T> Invoke<T>(string methodName, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        var policy = BuildPolicy();

        var attempt = 0;
        // T result = default;
        return await policy.ExecuteAsync(async () =>
            {
                Log.Debug("{Caller}: Attempt {Attempt}", methodName, ++attempt);
                return await Connection.InvokeAsync<T>(methodName, arg1, arg2, arg3, arg4);
            }
        );
    }

    private AsyncRetryPolicy BuildPolicy()
    {
        return _policyFactory.BuildHubPolicy();
    }
}