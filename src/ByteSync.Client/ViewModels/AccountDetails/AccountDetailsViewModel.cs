using System.Reactive;
using System.Reactive.Linq;
using ByteSync.Business.Arguments;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Services.Sessions.Connecting;
using ByteSync.ViewModels.Misc;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.AccountDetails;

public class AccountDetailsViewModel : FlyoutElementViewModel
{
    private readonly IApplicationSettingsRepository _applicationSettingsRepository;
    private readonly ICloudSessionConnector _cloudSessionConnector;

    public AccountDetailsViewModel() 
    {

    }

    public AccountDetailsViewModel(IApplicationSettingsRepository userSettingsManager,
        ICloudSessionConnector cloudSessionConnector, UsageStatisticsViewModel usageStatisticsViewModel)
    {
        _applicationSettingsRepository = userSettingsManager;
        _cloudSessionConnector = cloudSessionConnector;
        
        UsageStatistics = usageStatisticsViewModel;
        
        LogOutCommand = ReactiveCommand.Create(LogOut, _cloudSessionConnector.CanLogOutOrShutdown);

        ShowCanNotLogOutMessage = false;

        var userSettings = _applicationSettingsRepository.GetCurrentApplicationSettings();
        AccountEmail = userSettings.DecodedEmail;
        SerialNumber = userSettings.DecodedSerial;

        ProductName = _applicationSettingsRepository.ProductSerialDescription?.ProductName ?? "";
        Subscription = _applicationSettingsRepository.ProductSerialDescription?.Subscription ?? "";
        AllowedCloudSynchronizationVolumeInBytes = _applicationSettingsRepository.ProductSerialDescription?.AllowedCloudSynchronizationVolumeInBytes ?? 0;
        
        this.WhenActivated(HandleActivation);
    }

    private void HandleActivation(Action<IDisposable> disposables)
    {
        _cloudSessionConnector.CanLogOutOrShutdown
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToPropertyEx(this, x => x.CanLogOutOrShutdown);
    }

    public ReactiveCommand<Unit, Unit> LogOutCommand { get; }
    
    public extern bool CanLogOutOrShutdown { [ObservableAsProperty] get; }

    private void LogOut()
    {
        // if (!_cloudSessionConnector.CanLogOutOrShutdown())
        // {
        //     ShowCanNotLogOutMessage = true;
        // }
        // else
        // {
        //     _navigationEventsHub.RaiseLogOutRequested();
        // }
     
        // todo 220523
        // _navigationEventsHub.RaiseLogOutRequested();
    }

    [Reactive]
    public string? AccountEmail { get; set; }

    [Reactive]
    public string? SerialNumber { get; set; }
    
    [Reactive]
    public string ProductName { get; set; }

    [Reactive]
    public string Subscription { get; set; }
    
    [Reactive]
    public long AllowedCloudSynchronizationVolumeInBytes { get; set; }

    [Reactive]
    public bool ShowCanNotLogOutMessage { get; set; }

    public UsageStatisticsViewModel UsageStatistics { get; }
}