using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;

namespace ByteSync.Services.Bootstrappers;

public class CommandLineBootstrapper : BaseBootstrapper
{
    public CommandLineBootstrapper(IEnvironmentService environmentService, 
        ILocalApplicationDataManager localApplicationDataManager, ILogger<CommandLineBootstrapper> logger) 
        : base(environmentService, logger, localApplicationDataManager)
    {

    }
    
    public override void Start()
    {

    }
    
    public override void AfterFrameworkInitializationCompleted()
    {

    }
}