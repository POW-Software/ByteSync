using System.Reactive.Linq;
using Autofac.Features.Indexed;
using Avalonia.Animation;
using Avalonia.Controls.Mixins;
using ByteSync.Assets.Resources;
using ByteSync.Business.Navigations;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Navigations;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Interfaces.Services.Sessions.Connecting;
using ByteSync.ViewModels.Headers;
using ByteSync.ViewModels.Misc;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels;

public class MainWindowViewModel : ActivatableViewModelBase, IScreen
{
    private readonly ISessionService _sessionService;
    private readonly ICloudSessionConnectionService _cloudSessionConnectionService;
    private readonly INavigationService _navigationService;
    private readonly IZoomService _zoomService;
    private readonly IIndex<NavigationPanel, IRoutableViewModel> _navigationPanelViewModels;
    private readonly IMessageBoxViewModelFactory _messageBoxViewModelFactory;
    private readonly IQuitSessionService _quitSessionService;
    private readonly ICloudSessionConnectionRepository _cloudSessionConnectionRepository;
    private readonly ILogger<MainWindowViewModel> _logger;

    public RoutingState Router { get; } = new();

    public MainWindowViewModel()
    {

    }

    public MainWindowViewModel(ISessionService sessionService, ICloudSessionConnectionService cloudSessionConnectionService, INavigationService navigationService, 
        IZoomService zoomService, FlyoutContainerViewModel? flyoutContainerViewModel, HeaderViewModel headerViewModel, 
        IIndex<NavigationPanel, IRoutableViewModel> navigationPanelViewModels, IMessageBoxViewModelFactory messageBoxViewModelFactory,
        IQuitSessionService quitSessionService, ICloudSessionConnectionRepository cloudSessionConnectionRepository,
        ILogger<MainWindowViewModel> logger)
    {
        PageTransition = null;

        _sessionService = sessionService;
        _cloudSessionConnectionService = cloudSessionConnectionService;
        _navigationService = navigationService;
        _zoomService = zoomService;
        _navigationPanelViewModels = navigationPanelViewModels;
        _messageBoxViewModelFactory = messageBoxViewModelFactory;
        _quitSessionService = quitSessionService;
        _cloudSessionConnectionRepository = cloudSessionConnectionRepository;
        _logger = logger;
        
        FlyoutContainer = flyoutContainerViewModel!;
        Header = headerViewModel;

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
            
            _cloudSessionConnectionRepository.ConnectionStatusObservable
                .Where(sessionConnectionStatus => sessionConnectionStatus.In(SessionConnectionStatus.NoSession, SessionConnectionStatus.InSession))
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(sessionConnectionStatus =>
                {
                    if (sessionConnectionStatus == SessionConnectionStatus.NoSession)
                    {
                        _navigationService.NavigateTo(NavigationPanel.Home);
                    }
                    else if (sessionConnectionStatus == SessionConnectionStatus.InSession)
                    {
                        _navigationService.NavigateTo(NavigationPanel.CloudSynchronization);
                    }
                })
                .DisposeWith(disposables);
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
        // The PageTransition is initially null to avoid the Crossfade during the initial loading of the application
        // As soon as the user is connected, we activate the transitions based on the default value
        // https://github.com/AvaloniaUI/Avalonia/blob/master/src/Avalonia.ReactiveUI/TransitioningContentControl.cs
        
        if (PageTransition == null)
        {
            var crossFade = new CrossFade(TimeSpan.FromSeconds(0.5));
            PageTransition = crossFade;
        }
    }

    public async Task<bool> OnCloseWindowRequested(bool isCtrlDown)
    {
        if (!FlyoutContainer.CanCloseCurrentFlyout && !isCtrlDown)
        {
            return false;
        }
        
        var canLogOutOrShutdown = await _cloudSessionConnectionService.CanLogOutOrShutdown.FirstOrDefaultAsync();

        var canQuit = isCtrlDown || canLogOutOrShutdown;
        
        if (!canQuit)
        {
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
                _logger.LogWarning("Forced closing of the application because the Ctrl key is pressed...");
            }
            
            try
            {
                if (_sessionService.CurrentSession != null)
                {
                    await _quitSessionService.Process();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnCloseWindowRequested.QuitSession");
            }
            
            _logger.LogInformation("Shutting down the application...");

            return true;
        }
    }
}