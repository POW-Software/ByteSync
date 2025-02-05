using System.Reactive;
using System.Reactive.Linq;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.EventsHubs;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.ViewModels.Sessions.Managing;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace ByteSync.ViewModels.Sessions.Local;

public class LocalSessionManagementViewModel : ActivatableViewModelBase
{
    private readonly ISessionInterruptor _sessionInterruptor;
    private readonly INavigationEventsHub _navigationEventsHub;
    private readonly ISessionService _sessionService;

    public LocalSessionManagementViewModel()
    {
        
    }
    
    internal LocalSessionManagementViewModel(ISessionInterruptor sessionQuitChecker, INavigationEventsHub navigationEventsHub,
        ISessionService sessionDataHolder, ISessionSettingsEditViewModelFactory sessionSettingsEditViewModelFactory)
    {
        _sessionInterruptor = sessionQuitChecker;
        _navigationEventsHub = navigationEventsHub;
        _sessionService = sessionDataHolder;

        SessionSettingsEditViewModel = sessionSettingsEditViewModelFactory.CreateSessionSettingsEditViewModel(null);
        
        QuitSessionCommand = ReactiveCommand.CreateFromTask(QuitSession);

        CreateLocalSessionProfileCommand = ReactiveCommand.CreateFromTask(CreateLocalSessionProfile);
        
        this.WhenActivated(disposables =>
        {
            RestartSessionCommand = ReactiveCommand.CreateFromTask(RestartSession,
                _sessionService.SessionStatusObservable
                    .Select(ss => ss.In(SessionStatus.Preparation, SessionStatus.Synchronization, SessionStatus.RegularEnd))
                    .ObserveOn(RxApp.MainThreadScheduler));
            
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