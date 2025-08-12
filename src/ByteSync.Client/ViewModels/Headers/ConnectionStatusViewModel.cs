using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using ByteSync.Assets.Resources;
using ByteSync.Business.Communications;
using ByteSync.Business.Themes;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Services.Communications;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Headers;

public class ConnectionStatusViewModel : ActivatableViewModelBase
{
    private readonly IConnectionService _connectionService;
    private readonly ILocalizationService _localizationService;
    private readonly IThemeService _themeService;
    
    private IBrush? _connectedBrush;
    private IBrush? _connectingBrush;
    private IBrush? _connectionFailedBrush;
    private IBrush? _notConnectedBrush;
    private IBrush? _retryConnectingSoonBrush;

    public ConnectionStatusViewModel()
    {

    }

    public ConnectionStatusViewModel(IConnectionService connectionService, ILocalizationService localizationService,
        IThemeService themeService)
    {
        _connectionService = connectionService;
        _localizationService = localizationService;
        _themeService = themeService;

        this.WhenActivated(disposables =>
        {
            _themeService.SelectedTheme
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(theme =>
                {
                    InitializeBrushes(theme);
                    SetBrush();
                })
                .DisposeWith(disposables);
            
            _connectionService.ConnectionStatus
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    SetText();
                    SetBrush();
                })
                .DisposeWith(disposables);
            
            _localizationService.CurrentCultureObservable
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => SetText())
                .DisposeWith(disposables);
        }); 
    }

    private void InitializeBrushes(Theme theme)
    {
        _connectedBrush = _themeService.GetBrush("HomeCloudSynchronizationBackGround");
        _connectingBrush = _themeService.GetBrush("SystemControlForegroundBaseHighBrush");
        _connectionFailedBrush = _themeService.GetBrush("HomeLocalSynchronizationBackGround");
        _notConnectedBrush = _connectionFailedBrush;
        _retryConnectingSoonBrush = _connectionFailedBrush;
    }

    private void SetBrush()
    {
        var connectionStatus = _connectionService.CurrentConnectionStatus;
        
        BadgeBrush = connectionStatus switch
        {
            ConnectionStatuses.Connected => ConnectedBadgeBrush,
            ConnectionStatuses.Connecting => ConnectingBadgeBrush,
            ConnectionStatuses.ConnectionFailed => ConnectionFailedBadgeBrush,
            ConnectionStatuses.NotConnected => NotConnectedBadgeBrush,
            ConnectionStatuses.RetryConnectingSoon => RetryConnectingSoonBadgeBrush,
            _ => UnknownBadgeBrush
        };
    }

    private IBrush ConnectedBadgeBrush => _connectedBrush!;
    
    private IBrush ConnectingBadgeBrush => _connectingBrush!;
    
    private IBrush ConnectionFailedBadgeBrush => _connectionFailedBrush!;
    
    private IBrush NotConnectedBadgeBrush => _notConnectedBrush!;
    
    private IBrush RetryConnectingSoonBadgeBrush => _retryConnectingSoonBrush!;
    
    private IBrush UnknownBadgeBrush => _retryConnectingSoonBrush!;

    private void SetText()
    {
        var connectionStatus = _connectionService.CurrentConnectionStatus;
        
        Text = connectionStatus switch
        {
            ConnectionStatuses.Connected => _localizationService[nameof(Resources.ConnectionStatus_Connected)],
            ConnectionStatuses.Connecting => _localizationService[nameof(Resources.ConnectionStatus_Connecting)],
            ConnectionStatuses.ConnectionFailed => _localizationService[nameof(Resources.ConnectionStatus_ConnectionFailed)],
            ConnectionStatuses.NotConnected => _localizationService[nameof(Resources.ConnectionStatus_NotConnected)],
            ConnectionStatuses.RetryConnectingSoon => _localizationService[nameof(Resources.ConnectionStatus_RetryConnectingSoon)],
            _ => _localizationService[nameof(Resources.ConnectionStatus_Unkown)]
        };
    }
    
    [Reactive]
    public string Text { get; set; }
    
    [Reactive]
    public IBrush BadgeBrush { get; set; }
}