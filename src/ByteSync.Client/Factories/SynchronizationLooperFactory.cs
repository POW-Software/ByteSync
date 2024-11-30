using Autofac;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Factories;

namespace ByteSync.Factories;

public class SynchronizationLooperFactory : ISynchronizationLooperFactory
{
    private readonly IComponentContext _context;

    public SynchronizationLooperFactory(IComponentContext context)
    {
        _context = context;
    }
    
    public ISynchronizationLooper CreateSynchronizationLooper()
    {
        var result = _context.Resolve<ISynchronizationLooper>();

        return result;
    }
}