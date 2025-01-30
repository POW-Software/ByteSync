using System.Reactive;
using System.Threading.Tasks;
using ByteSync.Business.Navigations;
using ByteSync.Interfaces.Controls.Navigations;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Services.Sessions.Connecting;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Splat;

namespace ByteSync.ViewModels.Sessions.Cloud.Managing;

public class StartCloudSessionViewModel : ViewModelBase
{
    private readonly ICloudSessionConnector _cloudSessionConnector;
    private readonly INavigationService _navigationService;

    public StartCloudSessionViewModel() : this(null, null)
    {
    }
    
    public StartCloudSessionViewModel(ICloudSessionConnector? cloudSessionConnector, INavigationService? navigationService)
    {
        _cloudSessionConnector = cloudSessionConnector ?? Locator.Current.GetService<ICloudSessionConnector>()!;
        _navigationService = navigationService ?? Locator.Current.GetService<INavigationService>()!;

        IsProgressActive = true;
        IsError = false;
        CancelCommand = ReactiveCommand.Create(() => _navigationService.NavigateTo(NavigationPanel.Home));
    }
    
    public ReactiveCommand<Unit, Unit> CancelCommand { get; set; }

    [Reactive]
    public bool IsProgressActive { get; set; }
        
    [Reactive]
    public bool IsError { get; set; }

    public async Task CreateSession()
    {
        try
        {
             // await _cloudSessionConnector.CreateSession(null);
        }
        catch (Exception ex)
        {
            IsProgressActive = false;
            IsError = true;
            Log.Error(ex, "CreateSession");
        }
    }
}