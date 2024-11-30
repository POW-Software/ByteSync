using ByteSync.Interfaces.Controls.Bootstrapping;

namespace ByteSync.Interfaces.Factories;

public interface IBootstrapperFactory
{
    IBootstrapper CreateBootstrapper();
}