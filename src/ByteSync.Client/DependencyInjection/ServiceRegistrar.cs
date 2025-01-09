using System.Net.Http;
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using ByteSync.DependencyInjection.Modules;
using ByteSync.Helpers;
using ByteSync.Services;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using ReactiveUI;
using Splat.Autofac;

namespace ByteSync.DependencyInjection;

public static class ServiceRegistrar
{
    public static IContainer RegisterComponents()
    {
        var builder = new ContainerBuilder();

        // Charger les modules Autofac
        
        // builder.RegisterModule<AutoScanningModule>();
        // builder.RegisterModule<GenericTypesModule>();
        // builder.RegisterModule<KeyedTypesModule>();
        // builder.RegisterModule<SingletonsModule>();
        
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        builder.Populate(serviceCollection);
        
        // builder.RegisterModule<GenericTypesModule>();
        builder.RegisterModule<ConfigurationModule>();
        builder.RegisterModule<EnvironmentModule>();
        
        
        builder.RegisterModule<AutoScanningModule>();
        builder.RegisterModule<GenericTypesModule>();
        builder.RegisterModule<KeyedTypesModule>();
        builder.RegisterModule<SingletonsModule>();
        
        // builder.RegisterModule<LoggingModule>();
        // builder.RegisterModule<ServicesModule>();
        // builder.RegisterModule<RepositoriesModule>();
        builder.RegisterModule<ViewModelsModule>();
        builder.RegisterModule<PoliciesModule>();
        // builder.RegisterModule<CommunicationModule>(); // Exemple de module supplémentaire

        // Configuration supplémentaire
        // var serviceCollection = new ServiceCollection();
        // ConfigureServices(serviceCollection);
        // builder.Populate(serviceCollection);
        //
        var autofacResolver = builder.UseAutofacDependencyResolver();
        builder.RegisterInstance(autofacResolver);
        
        autofacResolver.InitializeReactiveUI();

        var container = builder.Build();
        ContainerProvider.Container = container;
        
        // using (var scope = container.BeginLifetimeScope())
        // {
        //     var logger = scope.Resolve<ILogger<AutoScanningModule>>();
        //     logger.LogInformation("Test de log après la construction du conteneur.");
        // }

        return container;
    }
    
    public static IContainer RegisterComponents_bak()
    {
        // var assembly = Assembly.GetExecutingAssembly();
        // using var stream = assembly.GetManifestResourceStream("ByteSync.local.settings.json");
        // if (stream == null)
        // {
        //     throw new FileNotFoundException("Embedded resource 'local.settings.json' not found.");
        // }
        
        // IConfiguration configuration = new ConfigurationBuilder()
        //     .AddJsonStream(stream)
        //     .Build();
        
        var builder = new ContainerBuilder();
        
        var serviceCollection = new ServiceCollection();
        
        ConfigureServices(serviceCollection);
        
        builder.Populate(serviceCollection);
        
        // builder.RegisterInstance(configuration).As<IConfiguration>();
        
        // var fileSystem = new FileSystem();
        // builder.RegisterInstance<IFileSystem>(fileSystem).SingleInstance();
        //
        // var environmentService = new EnvironmentService();
        // builder.RegisterInstance(environmentService).As<IEnvironmentService>();
        
        // var localApplicationDataManager = new LocalApplicationDataManager(environmentService);
        // builder.RegisterInstance(localApplicationDataManager).As<ILocalApplicationDataManager>().SingleInstance();
        //
        // var loggerService = new SerilogConfigurationFactory(localApplicationDataManager, environmentService);
        // var loggerConfiguration = loggerService.BuildLoggerConfiguration();
        // builder.RegisterSerilog(loggerConfiguration);


        // builder.RegisterType<FileSystemAccessor>().AsImplementedInterfaces();
        //
        //
        // var configurationReader = new ConfigurationReader<ApplicationSettings>(fileSystem);
        // builder.RegisterInstance<IConfigurationReader<ApplicationSettings>>(configurationReader);
        //
        // var configurationWriter = new ConfigurationWriter<ApplicationSettings>(fileSystem);
        // builder.RegisterInstance<IConfigurationWriter<ApplicationSettings>>(configurationWriter);
        //
        // var applicationSettingsRepository = new ApplicationSettingsRepository(
        //     localApplicationDataManager, configurationReader, configurationWriter);
        // var applicationSettings = applicationSettingsRepository.GetCurrentApplicationSettings();
        // environmentService.SetClientId(applicationSettings.ClientId);
        //
        // builder.RegisterInstance(applicationSettingsRepository).As<IApplicationSettingsRepository>();
        //
        // builder.RegisterType<GraphicalUserInterfaceBootstrapper>().Keyed<IBootstrapper>(OperationMode.GraphicalUserInterface);
        // builder.RegisterType<CommandLineBootstrapper>().Keyed<IBootstrapper>(OperationMode.CommandLine);
        

        /*
        builder.RegisterType<WebAccessor>().AsImplementedInterfaces();
        builder.RegisterType<DataEncrypter>().AsImplementedInterfaces();
        builder.RegisterType<DataInventoryRunner>().AsImplementedInterfaces();
        builder.RegisterType<PostDownloadHandlerProxy>().AsImplementedInterfaces();
        
        builder.RegisterType<DataInventoryStarter>().SingleInstance().AsImplementedInterfaces();
        builder.RegisterType<CloudSessionConnector>().SingleInstance().AsImplementedInterfaces();
        builder.RegisterType<FileDownloaderCache>().SingleInstance().AsImplementedInterfaces();
        builder.RegisterType<CommandLineModeHandler>().SingleInstance().AsImplementedInterfaces();
        builder.RegisterType<SynchronizationStarter>().SingleInstance().AsImplementedInterfaces();
        
        
        builder.RegisterType<PathItemProxy>().AsImplementedInterfaces();
        builder.RegisterType<SynchronizationDownloadFinalizer>().AsImplementedInterfaces();
        builder.RegisterType<TemporaryFileManager>().AsImplementedInterfaces();
        builder.RegisterType<StatisticsService>().AsImplementedInterfaces();
        builder.RegisterType<SessionProfileManager>().AsImplementedInterfaces();
        builder.RegisterType<ApplicationRestarter>().AsImplementedInterfaces();
        builder.RegisterType<DownloadTargetBuilder>().AsImplementedInterfaces();
        builder.RegisterType<FileUploader>().AsImplementedInterfaces();
        builder.RegisterType<FileDownloader>().AsImplementedInterfaces();
        builder.RegisterType<DeltaManager>().AsImplementedInterfaces();
        builder.RegisterType<FileDatesSetter>().AsImplementedInterfaces();
        builder.RegisterType<SlicerEncrypter>().AsImplementedInterfaces();
        builder.RegisterType<MergerDecrypter>().AsImplementedInterfaces();
        builder.RegisterType<DownloadManager>().AsImplementedInterfaces(); 
        builder.RegisterType<ComparisonItemActionsManager>().AsImplementedInterfaces();
        builder.RegisterType<AtomicActionConsistencyChecker>().AsImplementedInterfaces();
        builder.RegisterType<SharedAtomicActionComputer>().AsImplementedInterfaces();
        builder.RegisterType<AvailableUpdatesLister>().AsImplementedInterfaces();
        builder.RegisterType<SharedActionsGroupComputer>().AsImplementedInterfaces();
        builder.RegisterType<SharedActionsGroupOrganizer>().AsImplementedInterfaces();
        builder.RegisterType<SynchronizationActionServerInformer>().AsImplementedInterfaces();
        builder.RegisterType<SynchronizationActionRemoteUploader>().AsImplementedInterfaces();
        builder.RegisterType<SynchronizationActionHandler>().AsImplementedInterfaces();
        // builder.RegisterType<SynchronizationManager>().AsImplementedInterfaces();
        builder.RegisterType<SessionInterruptor>().AsImplementedInterfaces();
        builder.RegisterType<PathItemChecker>().AsImplementedInterfaces();
        builder.RegisterType<DigitalSignatureComputer>().AsImplementedInterfaces();
        builder.RegisterType<DigitalSignaturesChecker>().AsImplementedInterfaces();
        builder.RegisterType<PublicKeysTruster>().AsImplementedInterfaces();
        builder.RegisterType<LobbyManager>().AsImplementedInterfaces();
        builder.RegisterType<ProfileAutoRunner>().AsImplementedInterfaces();
        builder.RegisterType<SynchronizationRuleMatcher>().AsImplementedInterfaces();
        builder.RegisterType<TimeTrackingComputer>().AsImplementedInterfaces();
        builder.RegisterType<SessionResetter>().AsImplementedInterfaces();
        builder.RegisterType<SynchronizationLooper>().AsImplementedInterfaces();
        builder.RegisterType<SynchronizationDataReceiver>().AsImplementedInterfaces();
        builder.RegisterType<SynchronizationDataLogger>().AsImplementedInterfaces();
        */

        // builder.RegisterType<FlyoutContainerViewModel>()
        //     .SingleInstance()
        //     .As<IDialogView>();
        
        // obsolete - start
        // builder.RegisterType<EventAggregator>().As<IEventAggregator>();
        // builder.RegisterType<UIHelper>().AsImplementedInterfaces();
        // obsolete - end

        // builder.RegisterType<ArgumentsService>().As<IArgumentsService>();
        
        var executingAssembly = Assembly.GetExecutingAssembly();

        // builder.RegisterAssemblyTypes(executingAssembly)
        //     .Where(t => t.Name.EndsWith("Service") && t.Name != "EnvironmentService")
        //     .SingleInstance()
        //     .AsImplementedInterfaces();
        //
        // builder.RegisterAssemblyTypes(executingAssembly)
        //     .Where(t => t.Name.EndsWith("Manager"))
        //     .AsImplementedInterfaces();
        
        // builder.RegisterAssemblyTypes(executingAssembly)
        //     .Where(t => t.Name.EndsWith("Factory"))
        //     .AsImplementedInterfaces();
        //
        // builder.RegisterAssemblyTypes(executingAssembly)
        //     .Where(t => t.Name.EndsWith("Converter"))
        //     .AsImplementedInterfaces();
        //
        // builder.RegisterAssemblyTypes(executingAssembly)
        //     .Where(t => t.Name.EndsWith("Mapper"))
        //     .AsImplementedInterfaces();
        //
        // builder.RegisterAssemblyTypes(executingAssembly)
        //     .Where(t => t.Name.EndsWith("Holder"))
        //     .SingleInstance()
        //     .AsImplementedInterfaces();
        //
        // builder.RegisterAssemblyTypes(executingAssembly)
        //     .Where(t => t.Name.EndsWith("Indexer"))
        //     .SingleInstance()
        //     .AsImplementedInterfaces();
        
        // builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
        //     .AssignableTo<IPushReceiver>()
        //     .As<IPushReceiver>()
        //     .SingleInstance();
        
        // builder.RegisterAssemblyTypes(executingAssembly)
        //     .Where(t => t.Name.EndsWith("Repository") && t.Name != "ApplicationSettingsRepository")
        //     .SingleInstance()
        //     .AsImplementedInterfaces();
        
        // builder.RegisterAssemblyTypes(executingAssembly)
        //     .Where(t => t.Name.EndsWith("Cache"))
        //     .SingleInstance()
        //     .AsImplementedInterfaces();
        //
        // builder.RegisterAssemblyTypes(executingAssembly)
        //     .Where(t => t.Name.EndsWith("EventsHub"))
        //     .SingleInstance()
        //     .AsImplementedInterfaces();
        //
        // builder.RegisterAssemblyTypes(executingAssembly)
        //     .Where(t => t.Name.EndsWith("ApiClient"))
        //     .SingleInstance()
        //     .AsImplementedInterfaces();
        //
        // builder.RegisterType<InventoryDataTrackingStrategy>()
        //     .Keyed<IDataTrackingStrategy>(TimeTrackingComputerType.Inventory);
        // builder.RegisterType<SynchronizationDataTrackingStrategy>()
        //     .Keyed<IDataTrackingStrategy>(TimeTrackingComputerType.Synchronization);

        // builder.RegisterType<FormatKbSizeConverter>().As<IFormatKbSizeConverter>();
        
        // builder.RegisterGeneric(typeof(SessionInvalidationCachePolicy<,>))
        //     .As(typeof(ISessionInvalidationSourceCachePolicy<,>))
        //     .InstancePerDependency();
        
        
        // builder.RegisterType<AfterTransferSynchronizationSharedFile>().Keyed<IAfterTransferSharedFile>(SharedFileTypes.DeltaSynchronization);
        // builder.RegisterType<AfterTransferSynchronizationSharedFile>().Keyed<IAfterTransferSharedFile>(SharedFileTypes.FullSynchronization);
        // builder.RegisterType<AfterTransferInventorySharedFile>().Keyed<IAfterTransferSharedFile>(SharedFileTypes.BaseInventory);
        // builder.RegisterType<AfterTransferInventorySharedFile>().Keyed<IAfterTransferSharedFile>(SharedFileTypes.FullInventory);


        // builder.RegisterType<CloudProxy>().SingleInstance().AsImplementedInterfaces();
        // builder.RegisterType<NavigationService>().SingleInstance().AsImplementedInterfaces();
        //
        // builder.RegisterType<ApiInvoker>().AsImplementedInterfaces();
        //
        // builder.RegisterType<HubPushHandler2>().SingleInstance().AsImplementedInterfaces();
        // builder.RegisterType<PublicKeysTruster>().SingleInstance().AsImplementedInterfaces();

        // builder.RegisterAssemblyTypes(executingAssembly)
        //     .Where(t => t.Name.EndsWith("ViewModel"))
        //     .AsSelf();

        // builder.RegisterType<MainWindowViewModel>().SingleInstance().AsSelf().AsImplementedInterfaces();
        // builder.RegisterType<FlyoutContainerViewModel>().SingleInstance().AsSelf().AsImplementedInterfaces();
        // builder.RegisterType<HeaderViewModel>().SingleInstance().AsSelf();
        
        // builder.RegisterType<ProfilesViewModel>().AsSelf();
        
        // builder.RegisterType<HomeMainViewModel>().Keyed<IRoutableViewModel>(NavigationPanel.Home);
        // builder.RegisterType<SessionMainViewModel>().Keyed<IRoutableViewModel>(NavigationPanel.CloudSynchronization);
        // builder.RegisterType<SessionMainViewModel>().Keyed<IRoutableViewModel>(NavigationPanel.LocalSynchronization);
        // builder.RegisterType<LobbyMainViewModel>().Keyed<IRoutableViewModel>(NavigationPanel.ProfileSessionLobby);
        // builder.RegisterType<LobbyMainViewModel>().Keyed<IRoutableViewModel>(NavigationPanel.ProfileSessionDetails);

        // builder.RegisterType<InventoryProcessViewModel>().InstancePerLifetimeScope().AsSelf();
        // builder.RegisterType<InventoryMainStatusViewModel>().InstancePerLifetimeScope().AsSelf();
        // builder.RegisterType<InventoryIdentificationViewModel>().InstancePerLifetimeScope().AsSelf();
        // builder.RegisterType<InventoryAnalysisViewModel>().InstancePerLifetimeScope().AsSelf();
        
        // builder.RegisterType<PathItemProxy>();
        // builder.RegisterType<SoftwareVersionProxy>();

         // builder.RegisterType<PushReceiversStarter>().As<IPushReceiversStarter>().SingleInstance();
        
        var autofacResolver = builder.UseAutofacDependencyResolver();

        // Register the resolver in Autofac so it can be later resolved
        builder.RegisterInstance(autofacResolver);

        // Initialize ReactiveUI components
        autofacResolver.InitializeReactiveUI();

        var container = builder.Build();
        
        ContainerProvider.Container = container;

        return container;
    }
    
    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpClient("ApiClient")
            .AddPolicyHandler((serviceProvider, request) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<RetryPolicyLogger>>();
                return GetRetryPolicy(request, logger);
            });
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(HttpRequestMessage request, ILogger<RetryPolicyLogger> logger)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, _) =>
                {
                    var requestInfo = $"{request.Method} {request.RequestUri}";

                    if (outcome.Exception != null)
                    {
                        logger.LogWarning(outcome.Exception, 
                            "Retry attempt {RetryCount} for {Request} after {Delay} seconds due to exception",
                            retryAttempt, requestInfo, timespan.TotalSeconds);
                    }
                    else
                    {
                        logger.LogWarning(
                            "Retry attempt {RetryCount} for {Request} after {Delay} seconds due to HTTP status code {StatusCode}",
                            retryAttempt, requestInfo, timespan.TotalSeconds, outcome.Result?.StatusCode);
                    }

                });
    }
}