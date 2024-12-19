using System.IO;
using System.IO.Abstractions;
using System.Reflection;
using Autofac;
using ByteSync.Business.Configurations;
using ByteSync.Business.Lobbies;
using ByteSync.Business.Misc;
using ByteSync.Business.Navigations;
using ByteSync.Business.PathItems;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Controls;
using ByteSync.Common.Interfaces;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Communications;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Bootstrapping;
using ByteSync.Interfaces.Controls.TimeTracking;
using ByteSync.Interfaces.Converters;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Repositories;
using ByteSync.Repositories;
using ByteSync.Services;
using ByteSync.Services.Actions;
using ByteSync.Services.Applications;
using ByteSync.Services.Automating;
using ByteSync.Services.Bootstrappers;
using ByteSync.Services.Communications;
using ByteSync.Services.Communications.Api;
using ByteSync.Services.Communications.SignalR;
using ByteSync.Services.Communications.Transfers;
using ByteSync.Services.Communications.Transfers.AfterTransfers;
using ByteSync.Services.Comparisons;
using ByteSync.Services.Configurations;
using ByteSync.Services.Converters;
using ByteSync.Services.Encryptions;
using ByteSync.Services.Inventories;
using ByteSync.Services.Lobbies;
using ByteSync.Services.Misc;
using ByteSync.Services.Navigations;
using ByteSync.Services.Profiles;
using ByteSync.Services.Sessions;
using ByteSync.Services.Synchronizations;
using ByteSync.Services.TimeTracking;
using ByteSync.Services.Updates;
using ByteSync.ViewModels;
using ByteSync.ViewModels.Headers;
using ByteSync.ViewModels.Home;
using ByteSync.ViewModels.Lobbies;
using ByteSync.ViewModels.Misc;
using ByteSync.ViewModels.Profiles;
using ByteSync.ViewModels.Sessions;
using ByteSync.ViewModels.Sessions.Cloud.Managing;
using ByteSync.ViewModels.Sessions.Inventories;
using ByteSync.ViewModels.Sessions.Settings;
using Microsoft.Extensions.Configuration;
using Prism.Events;
using ReactiveUI;
using Serilog.Extensions.Autofac.DependencyInjection;
using Splat.Autofac;


namespace ByteSync;

public delegate AnalysisModeViewModel AnalysisModeViewModelFactory(AnalysisModes analysisMode);
public delegate SessionSettingsEditViewModel SessionSettingsEditViewModelFactory(SessionSettings? sessionSettings);
public delegate DataTypeViewModel DataTypeViewModelFactory(DataTypes dataType);
public delegate LinkingKeyViewModel LinkingKeyViewModelFactory(LinkingKeys linkingKey);
public delegate LobbyMemberViewModel LobbyMemberViewModelFactory(LobbyMember lobbyMember);

public static class ServicesRegistrer
{
    public static IContainer RegisterComponents()
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
        
        var builder = new ContainerBuilder();
        
        builder.RegisterInstance(configuration).As<IConfiguration>();
        
        var fileSystem = new FileSystem();
        builder.RegisterInstance<IFileSystem>(fileSystem).SingleInstance();
        
        var environmentService = new EnvironmentService();
        builder.RegisterInstance(environmentService).As<IEnvironmentService>();
        
        var localApplicationDataManager = new LocalApplicationDataManager(environmentService);
        builder.RegisterInstance(localApplicationDataManager).As<ILocalApplicationDataManager>().SingleInstance();

        var loggerService = new SerilogConfigurationFactory(localApplicationDataManager, environmentService);
        var loggerConfiguration = loggerService.BuildLoggerConfiguration();
        builder.RegisterSerilog(loggerConfiguration);


        builder.RegisterType<FileSystemAccessor>().AsImplementedInterfaces();


        var configurationReader = new ConfigurationReader<ApplicationSettings>(fileSystem);
        builder.RegisterInstance<IConfigurationReader<ApplicationSettings>>(configurationReader);

        var configurationWriter = new ConfigurationWriter<ApplicationSettings>(fileSystem);
        builder.RegisterInstance<IConfigurationWriter<ApplicationSettings>>(configurationWriter);

        var applicationSettingsRepository = new ApplicationSettingsRepository(
            localApplicationDataManager, configurationReader, configurationWriter);
        var applicationSettings = applicationSettingsRepository.GetCurrentApplicationSettings();
        environmentService.SetClientId(applicationSettings.ClientId);

        builder.RegisterInstance(applicationSettingsRepository).As<IApplicationSettingsRepository>();
        
        builder.RegisterType<GraphicalUserInterfaceBootstrapper>().Keyed<IBootstrapper>(OperationMode.GraphicalUserInterface);
        builder.RegisterType<CommandLineBootstrapper>().Keyed<IBootstrapper>(OperationMode.CommandLine);
        

        
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
        builder.RegisterType<DatesSetter>().AsImplementedInterfaces();
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

        builder.RegisterType<FlyoutContainerViewModel>()
            .SingleInstance()
            .As<IDialogView>();
        
        // obsolete - start
        builder.RegisterType<EventAggregator>().As<IEventAggregator>();
        builder.RegisterType<UIHelper>().AsImplementedInterfaces();
        // obsolete - end

        // builder.RegisterType<ArgumentsService>().As<IArgumentsService>();
        
        var executingAssembly = Assembly.GetExecutingAssembly();

        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Service") && t.Name != "EnvironmentService")
            .SingleInstance()
            .AsImplementedInterfaces();
        
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Manager"))
            .AsImplementedInterfaces();
        
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Factory"))
            .AsImplementedInterfaces();
        
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Converter"))
            .AsImplementedInterfaces();
        
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Mapper"))
            .AsImplementedInterfaces();

        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Holder"))
            .SingleInstance()
            .AsImplementedInterfaces();
        
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Indexer"))
            .SingleInstance()
            .AsImplementedInterfaces();
        
        builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
            .AssignableTo<IPushReceiver>()
            .As<IPushReceiver>()
            .SingleInstance();
        
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Repository") && t.Name != "ApplicationSettingsRepository")
            .SingleInstance()
            .AsImplementedInterfaces();
        
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("Cache"))
            .SingleInstance()
            .AsImplementedInterfaces();
        
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("EventsHub"))
            .SingleInstance()
            .AsImplementedInterfaces();
        
        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("ApiClient"))
            .SingleInstance()
            .AsImplementedInterfaces();
        
        builder.RegisterType<InventoryDataTrackingStrategy>()
            .Keyed<IDataTrackingStrategy>(TimeTrackingComputerType.Inventory);
        builder.RegisterType<SynchronizationDataTrackingStrategy>()
            .Keyed<IDataTrackingStrategy>(TimeTrackingComputerType.Synchronization);

        builder.RegisterType<FormatKbSizeConverter>().As<IFormatKbSizeConverter>();
        
        builder.RegisterGeneric(typeof(SessionInvalidationCachePolicy<,>))
            .As(typeof(ISessionInvalidationSourceCachePolicy<,>))
            .InstancePerDependency();
        
        
        builder.RegisterType<AfterTransferSynchronizationSharedFile>().Keyed<IAfterTransferSharedFile>(SharedFileTypes.DeltaSynchronization);
        builder.RegisterType<AfterTransferSynchronizationSharedFile>().Keyed<IAfterTransferSharedFile>(SharedFileTypes.FullSynchronization);
        builder.RegisterType<AfterTransferInventorySharedFile>().Keyed<IAfterTransferSharedFile>(SharedFileTypes.BaseInventory);
        builder.RegisterType<AfterTransferInventorySharedFile>().Keyed<IAfterTransferSharedFile>(SharedFileTypes.FullInventory);


        builder.RegisterType<CloudProxy>().SingleInstance().AsImplementedInterfaces();
        builder.RegisterType<NavigationService>().SingleInstance().AsImplementedInterfaces();
        
        builder.RegisterType<ApiInvoker>().AsImplementedInterfaces();
        
        builder.RegisterType<HubPushHandler2>().SingleInstance().AsImplementedInterfaces();
        builder.RegisterType<PublicKeysTruster>().SingleInstance().AsImplementedInterfaces();

        builder.RegisterAssemblyTypes(executingAssembly)
            .Where(t => t.Name.EndsWith("ViewModel"))
            .AsSelf();

        builder.RegisterType<MainWindowViewModel>().SingleInstance().AsSelf().AsImplementedInterfaces();
        builder.RegisterType<FlyoutContainerViewModel>().SingleInstance().AsSelf().AsImplementedInterfaces();
        builder.RegisterType<HeaderViewModel>().SingleInstance().AsSelf();
        
        builder.RegisterType<ProfilesViewModel>().AsSelf();
        
        builder.RegisterType<HomeMainViewModel>().Keyed<IRoutableViewModel>(NavigationPanel.Home);
        builder.RegisterType<SessionMainViewModel>().Keyed<IRoutableViewModel>(NavigationPanel.CloudSynchronization);
        builder.RegisterType<SessionMainViewModel>().Keyed<IRoutableViewModel>(NavigationPanel.LocalSynchronization);
        builder.RegisterType<LobbyMainViewModel>().Keyed<IRoutableViewModel>(NavigationPanel.ProfileSessionLobby);
        builder.RegisterType<LobbyMainViewModel>().Keyed<IRoutableViewModel>(NavigationPanel.ProfileSessionDetails);

        builder.RegisterType<InventoryProcessViewModel>().InstancePerLifetimeScope().AsSelf();
        builder.RegisterType<InventoryMainStatusViewModel>().InstancePerLifetimeScope().AsSelf();
        builder.RegisterType<InventoryIdentificationViewModel>().InstancePerLifetimeScope().AsSelf();
        builder.RegisterType<InventoryAnalysisViewModel>().InstancePerLifetimeScope().AsSelf();
        
        builder.RegisterType<PathItemProxy>();
        builder.RegisterType<SoftwareVersionProxy>();

         builder.RegisterType<PushReceiversStarter>().As<IPushReceiversStarter>().SingleInstance();
        
        var autofacResolver = builder.UseAutofacDependencyResolver();

        // Register the resolver in Autofac so it can be later resolved
        builder.RegisterInstance(autofacResolver);

        // Initialize ReactiveUI components
        autofacResolver.InitializeReactiveUI();

        var container = builder.Build();
        
        ContainerProvider.Container = container;

        return container;
    }
}