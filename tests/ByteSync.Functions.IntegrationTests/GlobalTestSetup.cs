using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using ByteSync.Functions.IntegrationTests.TestHelpers.Autofac;
using ByteSync.ServerCommon.Business.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ByteSync.Functions.IntegrationTests;

[SetUpFixture]
public class GlobalTestSetup
{
    public static IContainer Container { get; private set; } = null!;

    public static IConfiguration Configuration { get; private set; } = null!;

    public static Assembly ByteSyncServerCommonAssembly { get; private set; } = null!;

    [OneTimeSetUp]
    public void RunBeforeAnyTests()
    {
        ByteSyncServerCommonAssembly = GetReferencedAssemblyByName("ByteSync.ServerCommon");
        
        Configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Secret.json", optional: true) // Secret configuration
            .Build();
        
        var builder = new ContainerBuilder();
        
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        
        builder.Populate(serviceCollection);
        builder.RegisterModule(new ServicesModule());
        builder.RegisterModule(new LoggingModule());
        builder.RegisterModule(new FactoriesModule());
        
        Container = builder.Build();
    }

    [OneTimeTearDown]
    public void RunAfterAllTests()
    {
        Container.Dispose();
    }
    
    private void ConfigureServices(IServiceCollection services)
    {
        // Configure your settings objects here
        services.Configure<RedisSettings>(Configuration.GetSection("Redis"));
        services.Configure<BlobStorageSettings>(Configuration.GetSection("BlobStorage"));
        services.Configure<SignalRSettings>(Configuration.GetSection("SignalR"));
        services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));
    }
    
    private static Assembly GetReferencedAssemblyByName(string assemblyName)
    {
        var referencedAssemblies = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
        foreach (var refAssembly in referencedAssemblies)
        {
            if (refAssembly.Name!.Equals(assemblyName, StringComparison.OrdinalIgnoreCase))
            {
                return Assembly.Load(refAssembly);
            }
        }
        
        throw new Exception($"Assembly {assemblyName} not found");
    }
}