using Autofac.Features.Indexed;
using ByteSync.Business.Misc;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Bootstrapping;
using ByteSync.Interfaces.Factories;

namespace ByteSync.Factories;

public class BootstrapperFactory : IBootstrapperFactory
{
    private readonly IEnvironmentService _environmentService;
    private readonly IIndex<OperationMode, IBootstrapper> _bootstrappers;

    public BootstrapperFactory(IEnvironmentService environmentService, IIndex<OperationMode, IBootstrapper> bootstrappers)
    {
        _environmentService = environmentService;
        _bootstrappers = bootstrappers;
    }
    
    public IBootstrapper CreateBootstrapper()
    {
        var bootstrapper = _bootstrappers[_environmentService.OperationMode];

        return bootstrapper;
    }
}