using System.Reflection;
using System.Text.Json;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using ByteSync.Common.Controls.Json;
using ByteSync.Functions.Helpers.Loaders;
using ByteSync.Functions.Helpers.Middlewares;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Commands.Inventories;
using ByteSync.ServerCommon.Helpers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureAppConfiguration((hostingContext, builder) =>
    {
        builder.AddUserSecrets<Program>(optional: true, reloadOnChange: false);
        builder.AddEnvironmentVariables();
        
        IConfiguration configuration = builder.Build();

        Console.WriteLine($"Current Environment: {hostingContext.HostingEnvironment.EnvironmentName}");
        
        builder.AddAzureAppConfiguration(hostingContext, configuration);
        builder.AddJsonFile("local.settings.json", optional: true);
        builder.AddUserSecrets<Program>(optional: true, reloadOnChange: false);
        
        builder.Build();
    })
    .ConfigureFunctionsWorkerDefaults(builder =>
    {
        builder.UseMiddleware<TelemetryAndLoggingMiddleware>();
        
        builder.UseMiddleware<ErrorHandlingMiddleware>();
        
        builder.UseWhen<JwtMiddleware>((context) =>
        {
            return context.FunctionDefinition.InputBindings.Values
                .First(a => a.Type.EndsWith("Trigger")).Type == "httpTrigger";
        });
    })
    .ConfigureContainer<ContainerBuilder>(builder =>
    {
        builder.LoadDependencyInjection();
    })
    .ConfigureServices((_, services) =>
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
        
        services.Configure<JsonSerializerOptions>(options =>
        {
            JsonSerializerOptionsHelper.SetOptions(options);
        });
        
        var serviceProvider = services.BuildServiceProvider();
        
        var configuration = serviceProvider.GetService<IConfiguration>()!;
        var appSettingsSection = configuration.GetSection("AppSettings");
        services.Configure<RedisSettings>(configuration.GetSection("Redis"));
        services.Configure<BlobStorageSettings>(configuration.GetSection("BlobStorage"));
        services.Configure<SignalRSettings>(configuration.GetSection("SignalR"));
        services.Configure<AppSettings>(appSettingsSection);
        var appSettings = appSettingsSection.Get<AppSettings>();
        
        services.AddClaimAuthorization();
        services.AddJwtAuthentication(appSettings!.Secret);
        
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly(), typeof(AddDataSourceRequest).Assembly));
    })
    .Build();

host.Run();
