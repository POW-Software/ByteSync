using System.Reactive.Linq;
using System.Threading.Tasks;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces.Controls.Navigations;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Interfaces.Services.Sessions.Connecting;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Unit = System.Reactive.Unit;

namespace ByteSync.ViewModels.Home;

public class CreateCloudSessionViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly ICloudSessionConnector _cloudSessionConnector;
    private readonly ICreateSessionService _createSessionService;
    private readonly ICloudSessionConnectionRepository _cloudSessionConnectionRepository;
    private readonly ILogger<CreateCloudSessionViewModel> _logger;


    public CreateCloudSessionViewModel()
    {
    }

    public CreateCloudSessionViewModel(INavigationService navigationService, ICloudSessionConnector cloudSessionConnector, 
        ICreateSessionService createSessionService, ICloudSessionConnectionRepository cloudSessionConnectionRepository, 
        ILogger<CreateCloudSessionViewModel> logger)
    {
        _navigationService = navigationService;
        _cloudSessionConnector = cloudSessionConnector;
        _createSessionService = createSessionService;
        _cloudSessionConnectionRepository = cloudSessionConnectionRepository;
        _logger = logger;
        
        CreateCloudSessionCommand = ReactiveCommand.CreateFromTask(CreateCloudSession);
        CancelCloudSessionCreationCommand = ReactiveCommand.CreateFromTask(CancelCreateCloudSession);
        
        _cloudSessionConnectionRepository.ConnectionStatusObservable
            .Select(x => x == SessionConnectionStatus.CreatingSession)
            .ToPropertyEx(this, x => x.IsCreatingCloudSession);
        
        // CancelCommand = ReactiveCommand.Create(() => _navigationService.NavigateTo(NavigationPanel.Home));
    }

    public ReactiveCommand<Unit, Unit>? CreateCloudSessionCommand { get; set; }
    
    public ReactiveCommand<Unit, Unit>? CancelCloudSessionCreationCommand { get; set; }
    
    public extern bool IsCreatingCloudSession { [ObservableAsProperty] get; }
    
    private async Task CreateCloudSession()
    {
        // CloudSessionManagement = StartCloudSessionViewModel;
        // await StartCloudSessionViewModel!.CreateSession();
        
        try
        {
            await _createSessionService.CreateCloudSession(new CreateCloudSessionRequest(null));
        }
        catch (Exception ex)
        {
            // IsProgressActive = false;
            // IsError = true;
            _logger.LogError(ex, "CreateSession");
        }
    }
    
    private async Task CancelCreateCloudSession()
    {
        try
        {
            await _createSessionService.CancelCreateCloudSession();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CancelCreateCloudSession");
        }
    }
}