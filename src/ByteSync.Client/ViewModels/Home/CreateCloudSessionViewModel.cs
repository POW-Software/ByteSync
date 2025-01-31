using System.Reactive.Disposables;
using System.Reactive.Linq;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Unit = System.Reactive.Unit;

namespace ByteSync.ViewModels.Home;

public class CreateCloudSessionViewModel : ActivatableViewModelBase
{
    private readonly ICreateSessionService _createSessionService;
    private readonly ICloudSessionConnectionRepository _cloudSessionConnectionRepository;
    private readonly ILogger<CreateCloudSessionViewModel> _logger;


    public CreateCloudSessionViewModel()
    {
    }

    public CreateCloudSessionViewModel(ICreateSessionService createSessionService, ICloudSessionConnectionRepository cloudSessionConnectionRepository, 
        ILogger<CreateCloudSessionViewModel> logger)
    {
        _createSessionService = createSessionService;
        _cloudSessionConnectionRepository = cloudSessionConnectionRepository;
        _logger = logger;
        
        CreateCloudSessionCommand = ReactiveCommand.CreateFromTask(CreateCloudSession);
        CancelCloudSessionCreationCommand = ReactiveCommand.CreateFromTask(CancelCreateCloudSession);
        
        this.WhenActivated(disposables =>
        {
            _cloudSessionConnectionRepository.ConnectionStatusObservable
                .Select(x => x == SessionConnectionStatus.CreatingSession)
                .ToPropertyEx(this, x => x.IsCreatingCloudSession)
                .DisposeWith(disposables);
        });
    }

    public ReactiveCommand<Unit, Unit>? CreateCloudSessionCommand { get; set; }
    
    public ReactiveCommand<Unit, Unit>? CancelCloudSessionCreationCommand { get; set; }
    
    public extern bool IsCreatingCloudSession { [ObservableAsProperty] get; }
    
    private async Task CreateCloudSession()
    {
        try
        {
            await _createSessionService.CreateCloudSession(new CreateCloudSessionRequest(null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot create Cloud Session");
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