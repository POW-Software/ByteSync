using System.Reactive;
using System.Threading.Tasks;
using ByteSync.Business.Navigations;
using ByteSync.Interfaces.Controls.Navigations;
using ByteSync.Interfaces.Controls.Sessions;
using ReactiveUI;
using Serilog;

namespace ByteSync.ViewModels.Home;

public class CreateCloudSessionViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly ICloudSessionConnector _cloudSessionConnector;

    public CreateCloudSessionViewModel()
    {
    }

    public CreateCloudSessionViewModel(INavigationService navigationService, 
        ICloudSessionConnector cloudSessionConnector)
    {
        _navigationService = navigationService;
        _cloudSessionConnector = cloudSessionConnector;
        
        StartComparisonCommand = ReactiveCommand.CreateFromTask(CreateSession);
        
        // CancelCommand = ReactiveCommand.Create(() => _navigationService.NavigateTo(NavigationPanel.Home));
    }

    public ReactiveCommand<Unit, Unit>? StartComparisonCommand { get; set; }
    
    private async Task CreateSession()
    {
        // CloudSessionManagement = StartCloudSessionViewModel;
        // await StartCloudSessionViewModel!.CreateSession();
        
        try
        {
            await _cloudSessionConnector.CreateSession(null);
        }
        catch (Exception ex)
        {
            // IsProgressActive = false;
            // IsError = true;
            Log.Error(ex, "CreateSession");
        }
    }
}