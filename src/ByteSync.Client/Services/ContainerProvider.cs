using Autofac;

namespace ByteSync.Services;

public static class ContainerProvider
{
    public static IContainer Container { get; set; } = null!;
}