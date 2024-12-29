using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Mixins;
using ByteSync.Assets.Resources;
using ByteSync.Business.Navigations;
using ByteSync.Common.Business.Versions;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Navigations;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.EventsHubs;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Updates;
using ByteSync.ViewModels.Misc;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Headers;

public class HeaderViewModel : ActivatableViewModelBase
{
    private readonly IWebAccessor _webAccessor;
    private readonly IAvailableUpdateRepository _availableUpdateRepository;
    
    private readonly ILocalizationService _localizationService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IFlyoutElementViewModelFactory _flyoutElementViewModelFactory;

    public HeaderViewModel()
    {
        
    }
        
    public HeaderViewModel(FlyoutContainerViewModel flyoutContainerViewModel, ConnectionStatusViewModel connectionStatusViewModel,
        IWebAccessor webAccessor, IAvailableUpdateRepository availableAvailableUpdateRepository,
        ILocalizationService localizationService, INavigationService navigationService, IDialogService dialogService,
        IFlyoutElementViewModelFactory flyoutElementViewModelFactory)
    {
        _webAccessor = webAccessor;
        _availableUpdateRepository = availableAvailableUpdateRepository;
        _localizationService = localizationService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _flyoutElementViewModelFactory = flyoutElementViewModelFactory;

        FlyoutContainer = flyoutContainerViewModel;
        ConnectionStatus = connectionStatusViewModel;

        IconName = "None";
        
        var canView = this.WhenAnyValue(x => x.FlyoutContainer.CanCloseCurrentFlyout);
        ViewAccountCommand = ReactiveCommand.Create(ViewAccount, canView);
        ViewTrustedNetworkCommand = ReactiveCommand.Create(ViewTrustedNetwork, canView);
        ViewGeneralSettingsCommand = ReactiveCommand.Create(ViewGeneralSettings, canView);
        ViewAboutApplicationCommand = ReactiveCommand.Create(ViewAboutApplication, canView);
        ShowUpdateCommand = ReactiveCommand.Create(ViewUpdateDetails, canView);

        OpenSupportCommand = ReactiveCommand.Create(OpenSupport);
        GoHomeCommand = ReactiveCommand.Create(() => _navigationService.NavigateTo(NavigationPanel.Home));

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
    
    public ReactiveCommand<Unit, Unit> ViewAboutApplicationCommand { get; private set; }
        
    public ReactiveCommand<Unit, Unit> GoHomeCommand { get; private set; }

    public ReactiveCommand<Unit, Unit> OpenSupportCommand { get; set; }
        
    public ReactiveCommand<Unit, Unit> ShowUpdateCommand { get; set; }
    
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
        _dialogService.ShowFlyout(nameof(Resources.Shell_Account), true, _flyoutElementViewModelFactory.BuildAccountDetailsViewModel());
    }
    
    private void ViewTrustedNetwork()
    {
        _dialogService.ShowFlyout(nameof(Resources.Shell_TrustedNetwork), true, _flyoutElementViewModelFactory.BuildTrustedNetworkViewModel());
    }


    private void ViewGeneralSettings()
    {
        _dialogService.ShowFlyout(nameof(Resources.Shell_GeneralSettings), true, _flyoutElementViewModelFactory.BuildGeneralSettingsViewModel());
    }
    
    private void ViewAboutApplication()
    {
        _dialogService.ShowFlyout(nameof(Resources.Shell_AboutApplication), true, _flyoutElementViewModelFactory.BuildAboutApplicationViewModel());
    }
    
    private void ViewUpdateDetails()
    {
        _dialogService.ShowFlyout(nameof(Resources.Shell_Update), true, _flyoutElementViewModelFactory.BuildUpdateDetailsViewModel());
    }

    private void OpenSupport()
    {
        _webAccessor.OpenDocumentationUrl();
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
}