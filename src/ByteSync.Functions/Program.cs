﻿using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Azure.Identity;
using ByteSync.Common.Interfaces.Hub;
using ByteSync.Functions.Helpers;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Helpers;
using ByteSync.ServerCommon.Hubs;
using ByteSync.ServerCommon.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Misc;
using ByteSync.ServerCommon.Storage;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureAppConfiguration((hostingContext, builder) =>
    {
        builder.AddUserSecrets<Program>(optional: true, reloadOnChange: false);
        builder.AddEnvironmentVariables();
        
        IConfiguration settings = builder.Build();
        var azureAppConfigurationUrl = settings.GetValue<string>("AzureAppConfigurationUrl");

        Console.WriteLine($"Current Environment: {hostingContext.HostingEnvironment.EnvironmentName}");
        
        builder.AddAzureAppConfiguration(options =>
        {
            var credentials = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeAzureDeveloperCliCredential = true,
                ExcludeEnvironmentCredential = true,
                ExcludeAzurePowerShellCredential = true,
                ExcludeInteractiveBrowserCredential = true,
                ExcludeSharedTokenCacheCredential = true,
            });
            
            options.Connect(new Uri(azureAppConfigurationUrl!), credentials)
                .ConfigureStartupOptions(startupOptions =>
                {
                    startupOptions.Timeout = TimeSpan.FromSeconds(240);
                })
                // Load configuration values with no label
                .Select(KeyFilter.Any, LabelFilter.Null)
                // Override with any configuration values specific to current hosting env
                .Select(KeyFilter.Any, hostingContext.HostingEnvironment.EnvironmentName);

                options.ConfigureKeyVault(kv => kv.SetCredential(credentials));
        });
        
        builder.AddUserSecrets<Program>(optional: true, reloadOnChange: false);
        
        settings = builder.Build();
    })
    .ConfigureFunctionsWorkerDefaults(builder =>
    {
        builder.UseWhen<JwtMiddleware>((context) =>
        {
            // We want to use this middleware only for http trigger invocations.
            return context.FunctionDefinition.InputBindings.Values
                .First(a => a.Type.EndsWith("Trigger")).Type == "httpTrigger";
        });
    })
    .ConfigureContainer<ContainerBuilder>(builder =>
    {
        var executingAssembly = Assembly.GetExecutingAssembly();
        
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Service"))
            .InstancePerLifetimeScope()
            .AsImplementedInterfaces();

        // ByteSync.ServerCommon
        executingAssembly = typeof(IClientsRepository).Assembly;
        
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Repository"))
            .InstancePerLifetimeScope()
            .AsImplementedInterfaces();
        
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Service"))
            .InstancePerLifetimeScope()
            .AsImplementedInterfaces();
        
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Factory"))
            .InstancePerLifetimeScope()
            .AsImplementedInterfaces();
        
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Loader"))
            .InstancePerLifetimeScope()
            .AsImplementedInterfaces();
        
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Mapper"))
            .InstancePerLifetimeScope()
            .AsImplementedInterfaces();
        
        builder.RegisterType<ByteSyncClientCaller>()
            .InstancePerLifetimeScope()
            .AsImplementedInterfaces();
        
        builder.RegisterType<BlobContainerProvider>()
            .InstancePerLifetimeScope()
            .AsImplementedInterfaces();
        
        builder.Register(c => {
            var factory = c.Resolve<IHubContextFactory>();
            return factory.CreateHubContext();
        }).As<ServiceHubContext<IHubByteSyncPush>>().SingleInstance().AsImplementedInterfaces();
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        
        services.Configure<LoggerFilterOptions>(options =>
        {
            // https://stackoverflow.com/questions/52833645/azure-function-not-logging-to-application-insights
            // The Application Insights SDK adds a default logging filter that instructs ILogger to capture only Warning and more severe logs. Application Insights requires an explicit override.
            // Log levels can also be configured using appsettings.json. For more information, see https://learn.microsoft.com/en-us/azure/azure-monitor/app/worker-service#ilogger-logs
            // LoggerFilterRule toRemove = options.Rules.FirstOrDefault(rule => rule.ProviderName
            //                                                                  == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
            //
            // if (toRemove is not null)
            // {
            //     options.Rules.Remove(toRemove);
            // }
            
            // https://simonholman.dev/configure-serilog-for-logging-in-azure-functions
            // https://stackoverflow.com/questions/78394120/unable-to-see-my-log-messages-below-than-warning-on-azure-monitor/78407103#78407103
            // https://stackoverflow.com/questions/71034036/how-to-setup-serilog-with-azure-functions-v4-correctly
            
            // https://stackoverflow.com/questions/78161738/azure-function-app-net-8-not-logging-info-to-app-insights
            var appInsightsLoggerProvider = options.Rules.FirstOrDefault(rule => rule.ProviderName == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");

            if (appInsightsLoggerProvider != default) options.Rules.Remove(appInsightsLoggerProvider);
        });
        
        var serviceProvider = services.BuildServiceProvider();
        
        // var logger = serviceProvider.GetService<ILogger<Program>>();
        // logger?.LogInformation("Current Environment: {EnvironmentName}", hostContext.HostingEnvironment.EnvironmentName);
        
        var config = serviceProvider.GetService<IConfiguration>()!;
        var appSettingsSection = config.GetSection("AppSettings");
        services.Configure<RedisSettings>(config.GetSection("Redis"));
        services.Configure<BlobStorageSettings>(config.GetSection("BlobStorage"));
        services.Configure<SignalRSettings>(config.GetSection("SignalR"));
        services.Configure<CosmosDbSettings>(config.GetSection("CosmosDb"));
        services.Configure<AppSettings>(appSettingsSection);
        var appSettings = appSettingsSection.Get<AppSettings>();
        
        services.AddClaimAuthorization();
        services.AddJwtAuthentication(appSettings!.Secret);

        services.AddDbContext<ByteSyncDbContext>();
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ByteSyncDbContext>();
    await dbContext.InitializeCosmosDb();
}

host.Run();
