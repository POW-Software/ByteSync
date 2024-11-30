using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ByteSync.Business.Sessions;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.EventsHubs;
using ByteSync.ViewModels.Sessions.Settings;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Splat;

namespace ByteSync.ViewModels.Sessions.Local;

public class LocalSessionManagementViewModel : ActivableViewModelBase
{
    private readonly ISessionInterruptor _sessionInterruptor;
    private readonly INavigationEventsHub _navigationEventsHub;
    private readonly ICloudSessionEventsHub _cloudSessionEventsHub;
    private readonly ISessionService _sessionService;

    public LocalSessionManagementViewModel() : this (null)
    {
        
    }
    
    internal LocalSessionManagementViewModel(ISessionInterruptor? sessionQuitChecker = null, INavigationEventsHub? navigationEventsHub = null,
        ICloudSessionEventsHub? cloudSessionEventsHub = null, ISessionService? sessionDataHolder = null, 
        SessionSettingsEditViewModelFactory? sessionSettingsEditViewModelFactory = null)
    {
        _sessionInterruptor = sessionQuitChecker ?? Locator.Current.GetService<ISessionInterruptor>()!;
        _navigationEventsHub = navigationEventsHub ?? Locator.Current.GetService<INavigationEventsHub>()!;
        _cloudSessionEventsHub = cloudSessionEventsHub ?? Locator.Current.GetService<ICloudSessionEventsHub>()!;
        _sessionService = sessionDataHolder ?? Locator.Current.GetService<ISessionService>()!;

        SessionSettingsEditViewModel = sessionSettingsEditViewModelFactory.Invoke(null);
        
        QuitSessionCommand = ReactiveCommand.CreateFromTask(QuitSession);
        
        // var canRestartSession = this.WhenAnyValue(x => x.IsSessionActivated, (bool isSessionActivated) => isSessionActivated) ;
        // RestartSessionCommand = ReactiveCommand.CreateFromTask(RestartSession, canRestartSession);

        CreateLocalSessionProfileCommand = ReactiveCommand.CreateFromTask(CreateLocalSessionProfile);
        
        this.WhenActivated(disposables =>
        {
            RestartSessionCommand = ReactiveCommand.CreateFromTask(RestartSession,
                _sessionService.SessionStatusObservable
                    .Select(ss => ss.In(SessionStatus.Preparation, SessionStatus.Synchronization, SessionStatus.RegularEnd))
                    .ObserveOn(RxApp.MainThreadScheduler));
            
            // Observable.FromEventPattern<EventArgs>(_cloudSessionEventsHub, nameof(_cloudSessionEventsHub.SessionActivated))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(_ => OnSessionActivated())
            //     .DisposeWith(disposables);
            //
            // Observable.FromEventPattern<EventArgs>(_cloudSessionEventsHub, nameof(_cloudSessionEventsHub.SessionResetted))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(_ => OnSessionResetted())
            //     .DisposeWith(disposables);
            
            this.HandleActivation(disposables);
        });
    }

    private void HandleActivation(IDisposable compositeDisposable)
    {
        IsSessionActivated = _sessionService.IsSessionActivated;
        IsProfileSession = _sessionService.IsProfileSession;
        ProfileName = _sessionService.CurrentRunSessionProfileInfo?.ProfileName;
    }

    public ReactiveCommand<Unit, Unit> QuitSessionCommand { get; set; }
    
    public ReactiveCommand<Unit, Unit> RestartSessionCommand { get; set; }
    
    public ReactiveCommand<Unit, Unit> CreateLocalSessionProfileCommand { get; set; }
    
    [Reactive]
    public SessionSettingsEditViewModel SessionSettingsEditViewModel { get; set; }
    
    [Reactive]
    public bool IsSessionActivated { get; set; }
    
    [Reactive]
    public bool IsProfileSession { get; set; }
    
    [Reactive]
    public string? ProfileName { get; set; }
    
    private async Task QuitSession()
    {
        try
        {
            await _sessionInterruptor.RequestQuitSession();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "QuitSession error");
        }
    }
    
    private async Task RestartSession()
    {
        try
        {
            await _sessionInterruptor.RequestRestartSession();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "QuitSession error");
        }
    }
    
    private async Task CreateLocalSessionProfile()
    {
        await _navigationEventsHub.RaiseCreateLocalSessionProfileRequested();
    }
    
    private void OnSessionActivated()
    {
        IsSessionActivated = true;
    }

    private void OnSessionResetted()
    {
        IsSessionActivated = false;
    }
}