using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using ByteSync.ServerCommon.Business.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ByteSync.Client.IntegrationTests;

[SetUpFixture]
public class GlobalTestSetup
{
    public static IContainer Container { get; private set; } = null!;
    public static IConfiguration Configuration { get; private set; } = null!;

    [OneTimeSetUp]
    public void RunBeforeAnyTests()
    {
        Configuration = new ConfigurationBuilder()
            .AddJsonFile("client-integration-tests.local.settings.json", optional: false)
            .Build();

        var builder = new ContainerBuilder();

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        builder.Populate(serviceCollection);

        Container = builder.Build();
    }

    [OneTimeTearDown]
    public void RunAfterAllTests()
    {
        Container.Dispose();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.Configure<CloudflareR2Settings>(Configuration.GetSection("CloudflareR2"));
    }
}


