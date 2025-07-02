using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Mixins;
using ByteSync.Business.Arguments;
using ByteSync.Business.Events;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.EventsHubs;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Views;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace ByteSync.ViewModels.Sessions.Managing;

public class CurrentCloudSessionViewModel : ActivatableViewModelBase
{
    private readonly ISessionService _sessionService;
    private readonly ISessionInterruptor _sessionInterruptor;
    private readonly INavigationEventsHub _navigationEventsHub;
    private readonly IDataInventoryStarter _dataInventoryStarter;
    private readonly MainWindow _mainWindow;

    public CurrentCloudSessionViewModel() 
    {
    }

    public CurrentCloudSessionViewModel(ISessionService sessionService, ISessionInterruptor sessionInterruptor, INavigationEventsHub navigationEventsHub,
        IDataInventoryStarter dataInventoryStarter, ISessionSettingsEditViewModelFactory sessionSettingsEditViewModel,
        MainWindow mainWindow)
    {
        _sessionService = sessionService;
        _sessionInterruptor = sessionInterruptor;
        _navigationEventsHub = navigationEventsHub;
        _dataInventoryStarter = dataInventoryStarter;
        _mainWindow = mainWindow;

        SessionSettingsEditViewModel = sessionSettingsEditViewModel.CreateSessionSettingsEditViewModel(null);

        CopyCommand = ReactiveCommand.CreateFromTask<string>(Copy);

        QuitSessionCommand = ReactiveCommand.CreateFromTask(QuitSession);

        CreateCloudSessionProfileCommand = ReactiveCommand.CreateFromTask(CreateCloudSessionProfile);
        
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
            
            this.HandleActivation(disposables);
        });
    }

    private void HandleActivation(IDisposable compositeDisposable)
    {
        SessionId = _sessionService.SessionId;
        SessionPassword = _sessionService.CloudSessionPassword;
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
    
    public extern bool ShowRestartSessionAndSaveProfile { [ObservableAsProperty] get; }
        
    [Reactive]
    public SessionSettingsEditViewModel SessionSettingsEditViewModel { get; set; }
    
    [Reactive]
    public string? ProfileName { get; set; }

    private async Task Copy(string dataToCopy)
    {
        try
        {
            var clipboard = TopLevel.GetTopLevel(_mainWindow)?.Clipboard;

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
    
    // private void OnCloudSessionOnFatalError(CloudSessionFatalError cloudSessionFatalError)
    // {
    //     IsCloudSessionFatalError = true;
    // }
}