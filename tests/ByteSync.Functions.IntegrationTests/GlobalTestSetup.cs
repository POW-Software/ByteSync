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
            .AddJsonFile("functions-integration-tests.local.settings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        bool hasAzureStorage = !string.IsNullOrWhiteSpace(Configuration["AzureBlobStorage:AccountKey"]) &&
            !string.IsNullOrWhiteSpace(Configuration["AzureBlobStorage:AccountName"]) &&
            !string.IsNullOrWhiteSpace(Configuration["AzureBlobStorage:Endpoint"]) &&
            !string.IsNullOrWhiteSpace(Configuration["AzureBlobStorage:Container"]);

        bool hasCloudflare = !string.IsNullOrWhiteSpace(Configuration["CloudflareR2:AccessKeyId"]) &&
            !string.IsNullOrWhiteSpace(Configuration["CloudflareR2:SecretAccessKey"]) &&
            !string.IsNullOrWhiteSpace(Configuration["CloudflareR2:Endpoint"]) &&
            !string.IsNullOrWhiteSpace(Configuration["CloudflareR2:BucketName"]);

        if (!hasAzureStorage || !hasCloudflare)
        {
            Assert.Ignore("Functions integration tests require Azure Blob Storage and Cloudflare R2 settings.");
        }

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
        Container?.Dispose();
    }
    
    private void ConfigureServices(IServiceCollection services)
    {
        // Configure your settings objects here
        services.Configure<RedisSettings>(Configuration.GetSection("Redis"));
        services.Configure<AzureBlobStorageSettings>(Configuration.GetSection("AzureBlobStorage"));
        services.Configure<CloudflareR2Settings>(Configuration.GetSection("CloudflareR2"));
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