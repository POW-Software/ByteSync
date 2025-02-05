using ByteSync.Business.Arguments;
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
        if (_environmentService.Arguments.Contains(RegularArguments.VERSION))
        {
            Console.WriteLine(_environmentService.ApplicationVersion);
            
            Environment.Exit(0);
        }
    }
    
    public override void AfterFrameworkInitializationCompleted()
    {

    }
}