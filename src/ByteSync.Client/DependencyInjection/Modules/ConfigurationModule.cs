using System.IO;
using System.Reflection;
using Autofac;
using Microsoft.Extensions.Configuration;
using Module = Autofac.Module;

namespace ByteSync.DependencyInjection.Modules;

public class ConfigurationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("ByteSync.local.settings.json");
        if (stream == null)
        {
            throw new FileNotFoundException("Embedded resource 'local.settings.json' not found.");
        }

        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();

        builder.RegisterInstance(configuration).As<IConfiguration>().SingleInstance();
    }
}