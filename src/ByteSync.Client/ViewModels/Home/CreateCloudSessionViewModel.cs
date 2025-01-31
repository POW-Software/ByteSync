using System.Threading.Tasks;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces.Controls.Navigations;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Interfaces.Services.Sessions.Connecting;
using ReactiveUI;
using Serilog;
using Unit = System.Reactive.Unit;

namespace ByteSync.ViewModels.Home;

public class CreateCloudSessionViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly ICloudSessionConnector _cloudSessionConnector;
    private readonly ICreateSessionService _createSessionService;

    public CreateCloudSessionViewModel()
    {
    }

    public CreateCloudSessionViewModel(INavigationService navigationService, 
        ICloudSessionConnector cloudSessionConnector, ICreateSessionService createSessionService)
    {
        _navigationService = navigationService;
        _cloudSessionConnector = cloudSessionConnector;
        _createSessionService = createSessionService;
        
        CreateCloudSessionCommand = ReactiveCommand.CreateFromTask(CreateSession);
        CancelCloudSessionCreationCommand = ReactiveCommand.CreateFromTask(CancelCloudSessionCreation);
        
        // CancelCommand = ReactiveCommand.Create(() => _navigationService.NavigateTo(NavigationPanel.Home));
    }

    public ReactiveCommand<Unit, Unit>? CreateCloudSessionCommand { get; set; }
    
    public ReactiveCommand<Unit, Unit>? CancelCloudSessionCreationCommand { get; set; }
    
    private async Task CreateSession()
    {
        // CloudSessionManagement = StartCloudSessionViewModel;
        // await StartCloudSessionViewModel!.CreateSession();
        
        try
        {
            await _createSessionService.Process(new CreateSessionRequest(null));
        }
        catch (Exception ex)
        {
            // IsProgressActive = false;
            // IsError = true;
            Log.Error(ex, "CreateSession");
        }
    }
    
    private Task CancelCloudSessionCreation()
    {
        return Task.CompletedTask;
    }
}