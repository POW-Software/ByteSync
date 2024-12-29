using System.Reactive.Linq;
using System.Threading.Tasks;
using Autofac.Features.Indexed;
using Avalonia.Animation;
using Avalonia.Controls.Mixins;
using ByteSync.Assets.Resources;
using ByteSync.Business.Navigations;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Navigations;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Dialogs;
using ByteSync.ViewModels.Headers;
using ByteSync.ViewModels.Misc;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace ByteSync.ViewModels;

public partial class MainWindowViewModel : ActivatableViewModelBase, IScreen
{
    private readonly ISessionService _sessionService;
    private readonly ICloudSessionConnector _cloudSessionConnector;
    private readonly INavigationService _navigationService;
    private readonly IZoomService _zoomService;
    private readonly IIndex<NavigationPanel, IRoutableViewModel> _navigationPanelViewModels;
    private readonly IMessageBoxViewModelFactory _messageBoxViewModelFactory;

    public RoutingState Router { get; } = new RoutingState();

    public MainWindowViewModel()
    {

    }

    public MainWindowViewModel(ISessionService sessionService, ICloudSessionConnector cloudSessionConnector, INavigationService navigationService, 
        IZoomService zoomService, FlyoutContainerViewModel? flyoutContainerViewModel, HeaderViewModel headerViewModel, 
        IIndex<NavigationPanel, IRoutableViewModel> navigationPanelViewModels, IMessageBoxViewModelFactory messageBoxViewModelFactory)
    {
        PageTransition = null;

        _sessionService = sessionService;
        _cloudSessionConnector = cloudSessionConnector;
        _navigationService = navigationService;
        _zoomService = zoomService;
        _navigationPanelViewModels = navigationPanelViewModels;
        _messageBoxViewModelFactory = messageBoxViewModelFactory;
        
        FlyoutContainer = flyoutContainerViewModel!;
        Header = headerViewModel!;

        this.WhenActivated(disposables =>
        {
            _zoomService.ZoomLevel  
                .Select(zoomLevel => (1d / 100) * zoomLevel)
                .ToPropertyEx(this, x => x.ZoomLevel)
                .DisposeWith(disposables);

            _navigationService.CurrentPanel
                .Subscribe(request =>
                {
                    var panel = _navigationPanelViewModels[request.NavigationPanel];
                    Router.NavigateAndReset.Execute(panel);
                    
                    InitPageTransition();
                })
                .DisposeWith(disposables);
            
            // Observable.FromEventPattern<EventArgs>(_navigationEventsHub, nameof(_navigationEventsHub.NavigateToCloudSynchronizationRequested))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(_ => OnNavigateToCloudSynchronizationRequested())
            //     .DisposeWith(disposables);
            //
            // Observable.FromEventPattern<EventArgs>(_navigationEventsHub, nameof(_navigationEventsHub.NavigateToLocalSynchronizationRequested))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(_ => OnNavigateToLocalSynchronizationRequested())
            //     .DisposeWith(disposables);
            
            // Observable.FromEventPattern<EventArgs>(_navigationEventsHub, nameof(_navigationEventsHub.NavigateToLobbyRequested))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(_ => OnNavigateToLobbyRequested())
            //     .DisposeWith(disposables);
            
            // Observable.FromEventPattern<EventArgs>(_navigationEventsHub, nameof(_navigationEventsHub.NavigateToProfileDetailsRequested))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(_ => OnNavigateToProfileDetailsRequested())
            //     .DisposeWith(disposables);
            
            // Observable.FromEventPattern<LogInSucceededEventArgs>(_navigationEventsHub, nameof(_navigationEventsHub.LogInSucceeded))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(_ => OnLogInSucceeded())
            //     .DisposeWith(disposables);
            
            // Observable.FromEventPattern<EventArgs>(_navigationEventsHub, nameof(_navigationEventsHub.LogOutRequested))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(_ => OnLogOutRequested())
            //     .DisposeWith(disposables);
            
            // Observable.FromEventPattern<EventArgs>(_navigationEventsHub, nameof(_navigationEventsHub.NavigateToHomeRequested))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(_ => OnNavigateToHomeRequested())
            //     .DisposeWith(disposables);
            
            // Observable.FromEventPattern<EventArgs>(_cloudSessionEventsHub, nameof(_cloudSessionEventsHub.CloudSessionQuitted))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(_ => OnCloudSessionQuitted())
            //     .DisposeWith(disposables);
            
            // ApplyZoomLevel(_applicationSettingsManager.GetCurrentApplicationSettings().ZoomLevel);
        });
    }

    [Reactive]
    public ViewModelBase Header { get; set; }

    [Reactive]
    public FlyoutContainerViewModel FlyoutContainer { get; set; }
    
    public extern double ZoomLevel { [ObservableAsProperty] get; }

    [Reactive]
    public IPageTransition? PageTransition { get; set; }
    
    private void InitPageTransition()
    {
        // La PageTransition est initialement nulle pour éviter le Crossfade lors du chargement initial de l'application
        // Dès que l'utilisateur sera connecté, on active les transitions sur la base de la valeur par défaut
        // https://github.com/AvaloniaUI/Avalonia/blob/master/src/Avalonia.ReactiveUI/TransitioningContentControl.cs
        if (PageTransition == null)
        {
            var crossFade = new CrossFade(TimeSpan.FromSeconds(0.5));
            PageTransition = crossFade;
        }
    }

    // private void OnNavigateToCloudSynchronizationRequested()
    // {
    //     // _sessionService.SessionMode = SessionModes.Cloud;
    //     
    //     Router.NavigateAndReset.Execute(new SessionMainViewModel(this));
    //         
    //     // NavigationInfos navigationInfos = new NavigationInfos
    //     // { IconName = "RegularAnalyse", 
    //     //     TitleLocalizationName = nameof(Resources.OperationSelection_CloudSynchronization), 
    //     //     IsHome = false };
    //     //
    //     // _navigationEventsHub.RaiseNavigated(navigationInfos);
    // }
    //
    // private void OnNavigateToLocalSynchronizationRequested()
    // {
    //     Router.NavigateAndReset.Execute(new SessionMainViewModel(this));
    //         
    //     // NavigationInfos navigationInfos = new NavigationInfos
    //     // { IconName = "RegularRotateLeft", 
    //     //     TitleLocalizationName = nameof(Resources.OperationSelection_LocalSynchronization), 
    //     //     IsHome = false };
    //     //
    //     // _navigationEventsHub.RaiseNavigated(navigationInfos);
    // }
    
    // private void OnNavigateToLobbyRequested()
    // {
    //     Router.NavigateAndReset.Execute(new LobbyMainViewModel(this));
    //     
    //     // await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(delegate
    //     // {
    //     //     Router.NavigateAndReset.Execute(new LobbyMainViewModel(this));
    //     //     
    //     //     NavigationInfos navigationInfos = new NavigationInfos
    //     //     { IconName = "RegularUnite", 
    //     //         TitleLocalizationName = nameof(Resources.OperationSelection_Lobby), 
    //     //         IsHome = false };
    //     //
    //     //     _navigationEventsHub.RaiseNavigated(navigationInfos);
    //     // });
    // }
    //
    //
    // private void OnNavigateToProfileDetailsRequested()
    // {
    //     Router.NavigateAndReset.Execute(new LobbyMainViewModel(this));
    //     
    //     // await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(delegate
    //     // {
    //     //     Router.NavigateAndReset.Execute(new LobbyMainViewModel(this));
    //     //     
    //     //     NavigationInfos navigationInfos = new NavigationInfos
    //     //     { IconName = "RegularDetail", 
    //     //         TitleLocalizationName = nameof(Resources.OperationSelection_ProfileDetails), 
    //     //         IsHome = false };
    //     //
    //     //     _navigationEventsHub.RaiseNavigated(navigationInfos);
    //     // });
    // }

    // private void OnLogOutRequested()
    // {
    //     Avalonia.Threading.Dispatcher.UIThread.Post(delegate
    //     {
    //         Router.NavigateAndReset.Execute(new LoginViewModel(this));
    //         
    //         FlyoutContainer.CloseFlyout();
    //         // IsFlyoutContainerVisible = false;
    //         // FlyoutContainer.Content = null;
    //         
    //         NavigationInfos navigationInfos = new NavigationInfos { IsLogin = true };
    //         
    //         _navigationEventsHub.RaiseNavigated(navigationInfos);
    //         // _eventAggregator.GetEvent<Navigated>().Publish(navigationInfos);
    //     });
    // }

    // private void ApplyZoomLevel(int zoomLevel)
    // {
    //     ZoomLevel = (1d / 100) * zoomLevel;
    // }

    public async Task<bool> OnCloseWindowRequested(bool isCtrlDown)
    {
        // Le FlyoutContainer ne peut pas être fermé (ex: MAJ en cours) et sortie de l'application non forcée par isCtrlDown
        if (!FlyoutContainer.CanCloseCurrentFlyout && !isCtrlDown)
        {
            return false;
        }
        
        var canLogOutOrShutdown = await _cloudSessionConnector.CanLogOutOrShutdown.FirstOrDefaultAsync();

        var canQuit = isCtrlDown || canLogOutOrShutdown;
        
        if (!canQuit)
        {
            // Si on est dans une session, on indique à l'utilisateur qu'il doit quitter au préalable.
            
            var messageBoxViewModel = _messageBoxViewModelFactory.CreateMessageBoxViewModel(
                nameof(Resources.MainWindow_OnClose_ConfirmTitle), nameof(Resources.MainWindow_OnClose_ConfirmMessage));
            messageBoxViewModel.ShowOK = true;

            await FlyoutContainer.ShowMessageBoxAsync(messageBoxViewModel);

            return false;
        }
        else
        {
            if (!canLogOutOrShutdown && isCtrlDown)
            {
                // Sortie forcée car isCtrlDown
                Log.Warning("Forced closing of the application because the Ctrl key is pressed...");
            }
            
            try
            {
                if (_sessionService.SessionObservable != null)
                {
                    // On doit quitter la session
                    await _cloudSessionConnector.QuitSession();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "OnCloseWindowRequested.QuitSession");
            }
            
            Log.Information("Shutting down the application...");
            await Log.CloseAndFlushAsync();

            return true;
        }
    }
}