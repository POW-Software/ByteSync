using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.Mixins;
using ByteSync.Business.Arguments;
using ByteSync.Business.Events;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.EventsHubs;
using ByteSync.ViewModels.Sessions.Settings;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace ByteSync.ViewModels.Sessions.Cloud.Managing;

public class CurrentCloudSessionViewModel : ActivableViewModelBase
{
    private readonly ISessionService _sessionService;
    private readonly ICloudSessionEventsHub _cloudSessionEventsHub;
    private readonly ISessionInterruptor _sessionInterruptor;
    private readonly INavigationEventsHub _navigationEventsHub;
    private readonly IDataInventoryStarter _dataInventoryStarter;

    public CurrentCloudSessionViewModel() 
    {
    }

    public CurrentCloudSessionViewModel(ISessionService sessionService, ICloudSessionEventsHub cloudSessionEventsHub,
        ISessionInterruptor sessionInterruptor, INavigationEventsHub navigationEventsHub,
        IDataInventoryStarter dataInventoryStarter, SessionSettingsEditViewModelFactory sessionSettingsEditViewModel)
    {
        _sessionService = sessionService;
        _cloudSessionEventsHub = cloudSessionEventsHub;
        _sessionInterruptor = sessionInterruptor;
        _navigationEventsHub = navigationEventsHub;
        _dataInventoryStarter = dataInventoryStarter;
        
        SessionSettingsEditViewModel = sessionSettingsEditViewModel!.Invoke(null);

        CopyCommand = ReactiveCommand.CreateFromTask<string>(Copy);

        QuitSessionCommand = ReactiveCommand.CreateFromTask(QuitSession);
        
        // var canRestartSession = this.WhenAnyValue(x => x.IsCloudSessionActivated, (bool isCloudSessionActivated) => isCloudSessionActivated) ;
        // RestartSessionCommand = ReactiveCommand.CreateFromTask(RestartSession, canRestartSession);

        CreateCloudSessionProfileCommand = ReactiveCommand.CreateFromTask(CreateCloudSessionProfile);

        // this.WhenAnyValue(
        //         x => x.IsSessionCreatedByMe, x => x.IsProfileSession,
        //         (isSessionCreatedByMe, isProfileSession) 
        //             => (isSessionCreatedByMe && !isProfileSession))
        //     .ToPropertyEx(this, x => x.ShowRestartSessionAndSaveProfile);
        
        IsCloudSessionFatalError = false;
        
        RestartSessionCommand = ReactiveCommand.CreateFromTask(RestartSession,
            _sessionService.SessionStatusObservable
                .Select(ss =>
                    ss.In(SessionStatus.Preparation, SessionStatus.Inventory, SessionStatus.Comparison,
                        SessionStatus.Synchronization, SessionStatus.RegularEnd))
                .ObserveOn(RxApp.MainThreadScheduler));

        this.WhenActivated(disposables =>
        {
            _dataInventoryStarter.CanCurrentUserStartInventory()
                .ToPropertyEx(this, x => x.ShowRestartSessionAndSaveProfile)
                .DisposeWith(disposables);

            Observable.FromEventPattern<GenericEventArgs<CloudSessionFatalError>>(_cloudSessionEventsHub, nameof(_cloudSessionEventsHub.CloudSessionOnFatalError))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(evt => OnCloudSessionOnFatalError(evt.EventArgs.Value))
                .DisposeWith(disposables);
            
            // Observable.FromEventPattern<EventArgs>(_cloudSessionEventsHub, nameof(_cloudSessionEventsHub.SessionActivated))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(_ => OnSessionActivated())
            //     .DisposeWith(disposables);
            //
            // Observable.FromEventPattern<EventArgs>(_cloudSessionEventsHub, nameof(_cloudSessionEventsHub.MemberQuittedSession))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(_ => OnMemberQuittedSession())
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
        SessionId = _sessionService.SessionId;
        SessionPassword = _sessionService.CloudSessionPassword;
        // IsSessionCreatedByMe = _sessionService.IsCloudSessionCreatedByMe;
        // IsCloudSessionActivated = _sessionService.IsSessionActivated;
        // IsProfileSession = _sessionService.IsProfileSession;
        ProfileName = _sessionService.CurrentRunSessionProfileInfo?.ProfileName;
        
#if DEBUG
        if (Environment.GetCommandLineArgs().Contains(DebugArguments.COPY_SESSION_CREDENTIALS_TO_CLIPBOARD))
        {
            var credentials = $"{SessionId}||{SessionPassword}";
                
    #pragma warning disable CS4014
            Copy(credentials);
    #pragma warning restore CS4014
        }
    
        if (Environment.GetCommandLineArgs().Contains(DebugArguments.SHOW_DEMO_DATA))
        {
            SessionId = "123abc456";
            SessionPassword = "PASS";
        }
    #endif
    }

    public ReactiveCommand<string, Unit> CopyCommand { get; set; }

    public ReactiveCommand<Unit, Unit> QuitSessionCommand { get; set; }
    
    public ReactiveCommand<Unit, Unit> RestartSessionCommand { get; set; }
    
    public ReactiveCommand<Unit, Unit> CreateCloudSessionProfileCommand { get; set; }

    [Reactive]
    public string? SessionId { get; set; }

    [Reactive]
    public string? SessionPassword { get; set; }
    
    [Reactive]
    public bool IsCloudSessionFatalError { get; set; }
    
    // [Reactive]
    // public bool IsSessionCreatedByMe { get; set; }
    
    // [Reactive]
    // public bool IsCloudSessionActivated { get; set; }
    
    // [Reactive]
    // public bool IsProfileSession { get; set; }
    
    public extern bool ShowRestartSessionAndSaveProfile { [ObservableAsProperty] get; }
        
    [Reactive]
    public SessionSettingsEditViewModel SessionSettingsEditViewModel { get; set; }
    
    [Reactive]
    public string? ProfileName { get; set; }

    private async Task Copy(string dataToCopy)
    {
        try
        {
            var clipboard = Application.Current?.Clipboard;

            if (clipboard != null)
            {
                await clipboard.SetTextAsync(dataToCopy);
            }
            else
            {
                Log.Warning("Copy: unable to acess clipboard");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "copy error");
        }
    }

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
    
    private async Task CreateCloudSessionProfile()
    {
        var sessionId = _sessionService.SessionId;

        if (sessionId != null)
        {
            await _navigationEventsHub.RaiseCreateCloudSessionProfileRequested();
        }
    }
    
    private void OnCloudSessionOnFatalError(CloudSessionFatalError cloudSessionFatalError)
    {
        IsCloudSessionFatalError = true;
    }
    
    // private void OnSessionActivated()
    // {
    //     IsCloudSessionActivated = true;
    // }
    //
    // private void OnSessionResetted()
    // {
    //     IsCloudSessionActivated = false;
    // }
    
    // private void OnMemberQuittedSession()
    // {
    //     IsSessionCreatedByMe = _sessionService.IsCloudSessionCreatedByMe;
    // }
}