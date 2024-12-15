using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Mixins;
using ByteSync.Business.Navigations;
using ByteSync.Common.Business.Versions;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Navigations;
using ByteSync.Interfaces.EventsHubs;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Updates;
using ByteSync.ViewModels.Misc;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Headers;

public class HeaderViewModel : ActivableViewModelBase
{
    private readonly INavigationEventsHub _navigationEventsHub;
    private readonly IWebAccessor _webAccessor;
    private readonly IAvailableUpdateRepository _availableUpdateRepository;
    
    private readonly ILocalizationService _localizationService;
    private readonly INavigationService _navigationService;


    // public HeaderViewModel(FlyoutContainerViewModel flyoutContainerViewModel) : this (flyoutContainerViewModel, null, null, null, null, null)
    // {
    //         
    // }

    public HeaderViewModel()
    {
        
    }
        
    public HeaderViewModel(FlyoutContainerViewModel flyoutContainerViewModel, ConnectionStatusViewModel connectionStatusViewModel,
        INavigationEventsHub navigationEventsHub, IWebAccessor webAccessor, IAvailableUpdateRepository availableAvailableUpdateRepository,
        ILocalizationService localizationService, INavigationService navigationService)
    {
        _navigationEventsHub = navigationEventsHub;
        _webAccessor = webAccessor;
        _availableUpdateRepository = availableAvailableUpdateRepository;
        _localizationService = localizationService;
        _navigationService = navigationService;

        FlyoutContainer = flyoutContainerViewModel;
        ConnectionStatus = connectionStatusViewModel;

        IconName = "None";
        
        var canView = this.WhenAnyValue(x => x.FlyoutContainer.CanCloseCurrentFlyout);
        ViewAccountCommand = ReactiveCommand.Create(ViewAccount, canView);
        ViewTrustedNetworkCommand = ReactiveCommand.Create(ViewTrustedNetwork, canView);
        ViewGeneralSettingsCommand = ReactiveCommand.Create(ViewGeneralSettings, canView);
        ShowUpdateCommand = ReactiveCommand.Create(ShowUpdate, canView);

        OpenSupportCommand = ReactiveCommand.Create(OpenSupport);
        GoHomeCommand = ReactiveCommand.Create(() => _navigationService.NavigateTo(NavigationPanel.Home));
        DebugForceDisconnectionCommand = ReactiveCommand.CreateFromTask(DebugForceDisconnection);

        // NextAvailableVersions = new List<SoftwareVersion>();

        IsNavigateToHomeVisible = false;
        IsAccountVisible = true;
        IsDebugForceDisconnectionVisible = false;
    #if DEBUG
        IsDebugForceDisconnectionVisible = true;
    #endif

        this.WhenActivated(disposables =>
        {
            _navigationService.CurrentPanel
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(OnNavigated)
                .DisposeWith(disposables);

            _availableUpdateRepository.ObservableCache
                .Connect()
                .Filter(softwareVersion => softwareVersion.Level == PriorityLevel.Minimal)
                .QueryWhenChanged(query => query.Count)
                .Select(c => c > 0)
                .ToPropertyEx(this, x => x.IsAVersionMandatory)
                .DisposeWith(disposables);

            _availableUpdateRepository.ObservableCache
                .Connect()
                .QueryWhenChanged(query => query.Count)
                .Select(c => c > 0)
                .ToPropertyEx(this, x => x.ShowUpdateObservable)
                .DisposeWith(disposables);
            
            _localizationService.CurrentCultureObservable
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => OnLocaleChanged())
                .DisposeWith(disposables);
        });
    }
    
    private FlyoutContainerViewModel FlyoutContainer { get; }

    public ReactiveCommand<Unit, Unit> ViewAccountCommand { get; private set; }
    
    public ReactiveCommand<Unit, Unit> ViewTrustedNetworkCommand { get; private set; }

    public ReactiveCommand<Unit, Unit> ViewGeneralSettingsCommand { get; private set; }
        
    public ReactiveCommand<Unit, Unit> GoHomeCommand { get; private set; }

    public ReactiveCommand<Unit, Unit> OpenSupportCommand { get; set; }
        
    public ReactiveCommand<Unit, Unit> ShowUpdateCommand { get; set; }
    
    public ReactiveCommand<Unit, Unit> DebugForceDisconnectionCommand { get; set; }
    
    [Reactive]
    public ViewModelBase ConnectionStatus { get; set; }

    [Reactive]
    public string IconName { get; set; }

    [Reactive]
    public string Title { get; set; }

    [Reactive]
    public bool IsNavigateToHomeVisible { get; set; }

    [Reactive]
    public bool IsAccountVisible { get; set; }

    [Reactive]
    public string? TitleLocalizationName { get; private set; }

    [Reactive]
    public bool IsDebugForceDisconnectionVisible { get; set; }
    
    public extern bool ShowUpdateObservable { [ObservableAsProperty] get; }
    
    public extern bool IsAVersionMandatory { [ObservableAsProperty] get; }

    private void ViewAccount()
    {
        _navigationEventsHub.RaiseViewAccountRequested();
    }
    
    private void ViewTrustedNetwork()
    {
        _navigationEventsHub.RaiseViewTrustedNetworkRequested();
    }


    private void ViewGeneralSettings()
    {
        _navigationEventsHub.RaiseViewGeneralSettingsRequested();
    }

    private void OpenSupport()
    {
        _webAccessor.OpenSupportUrl();
    }
        
    private void ShowUpdate()
    {
        _navigationEventsHub.RaiseViewUpdateDetailsRequested();
    }

    private void OnNavigated(NavigationDetails navigationDetails)
    {
        IconName = navigationDetails.ApplicableIconName;
        TitleLocalizationName = navigationDetails.TitleLocalizationName;

        if (TitleLocalizationName.IsNotEmpty())
        {
            Title = _localizationService[TitleLocalizationName!];
        }
        else
        {
            Title = "";
        }
            
        IsNavigateToHomeVisible = !navigationDetails.IsHome;
    }

    private void OnLocaleChanged()
    {
        if (TitleLocalizationName.IsNotEmpty())
        {
            Title = _localizationService[TitleLocalizationName!];
        }
    }

    private Task DebugForceDisconnection()
    {
        return Task.CompletedTask;
        
        // var connectionManager = Locator.Current.GetService<IConnectionManager>()!;
        //
        // try
        // {
        //     await connectionManager.DebugForceDisconnection();
        // }
        // catch (Exception ex)
        // {
        //     Log.Error(ex, "QuitSession error");
        // }
    }
}