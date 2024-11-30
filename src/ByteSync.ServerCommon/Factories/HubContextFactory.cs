using ByteSync.Common.Interfaces.Hub;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Factories;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Factories;

public class HubContextFactory : IHubContextFactory
{
    private readonly SignalRSettings _signalRSettings;
    private readonly ILoggerFactory _loggerFactory;

    public HubContextFactory(IOptions<SignalRSettings> signalRSettings, ILoggerFactory loggerFactory)
    {
        _signalRSettings = signalRSettings.Value;
        _loggerFactory = loggerFactory;
    }
    
    public ServiceHubContext<IHubByteSyncPush> CreateHubContext()
    {
        using var serviceManager = new ServiceManagerBuilder()
            .WithOptions(o=>o.ConnectionString = _signalRSettings.ConnectionString)
            .WithLoggerFactory(_loggerFactory)
            .BuildServiceManager();
            
        var task = serviceManager.CreateHubContextAsync<IHubByteSyncPush>("ByteSync", default);

        var hubContext = task.GetAwaiter().GetResult();
        
        return hubContext;
    }
}