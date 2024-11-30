using Autofac;
using ByteSync.Interfaces.Communications;
using ByteSync.Interfaces.Controls.Bootstrapping;

namespace ByteSync.Services.Bootstrappers;

public class PushReceiversStarter : IPushReceiversStarter
{
    private readonly ILifetimeScope _scope;

    public PushReceiversStarter(ILifetimeScope  scope)
    {
        _scope = scope;
    }

    public void Start()
    {
        _scope.Resolve<IEnumerable<IPushReceiver>>();
    }
}