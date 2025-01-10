using System.Runtime.InteropServices;
using ByteSync.Business.Arguments;
using ByteSync.Business.Misc;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Bootstrapping;

namespace ByteSync.Services.Bootstrappers;

public abstract class BaseBootstrapper : IBootstrapper
{
    protected readonly IEnvironmentService _environmentService;
    protected readonly ILogger<BaseBootstrapper> _logger;
    protected readonly ILocalApplicationDataManager _localApplicationDataManager;

    public BaseBootstrapper(IEnvironmentService environmentService, ILogger<BaseBootstrapper> logger,
        ILocalApplicationDataManager localApplicationDataManager)
    {
        _environmentService = environmentService;
        _logger = logger;
        _localApplicationDataManager = localApplicationDataManager;
    }
    
    public Action? AttachConsole { get; set; }
    
    public abstract void Start();

    public abstract void AfterFrameworkInitializationCompleted();
}