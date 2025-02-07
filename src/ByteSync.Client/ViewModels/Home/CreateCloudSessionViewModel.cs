using System.Reactive.Disposables;
using System.Reactive.Linq;
using ByteSync.Assets.Resources;
using ByteSync.Business.Sessions;
using ByteSync.Business.Sessions.Connecting;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Interfaces.Services.Sessions.Connecting;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Unit = System.Reactive.Unit;

namespace ByteSync.ViewModels.Home;

public class CreateCloudSessionViewModel : ActivatableViewModelBase
{
    private readonly ICreateSessionService _createSessionService;
    private readonly ICloudSessionConnectionRepository _cloudSessionConnectionRepository;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<CreateCloudSessionViewModel> _logger;
    
    public CreateCloudSessionViewModel()
    {
    }

    public CreateCloudSessionViewModel(ICreateSessionService createSessionService, ICloudSessionConnectionRepository cloudSessionConnectionRepository, 
        ILocalizationService localizationService, ILogger<CreateCloudSessionViewModel> logger)
    {
        _createSessionService = createSessionService;
        _cloudSessionConnectionRepository = cloudSessionConnectionRepository;
        _localizationService = localizationService;
        _logger = logger;
        
        CreateCloudSessionCommand = ReactiveCommand.CreateFromTask(CreateCloudSession);
        CancelCloudSessionCreationCommand = ReactiveCommand.CreateFromTask(CancelCreateCloudSession);
        
        this.WhenActivated(disposables =>
        {
            _cloudSessionConnectionRepository.CreateSessionErrorObservable
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(OnCreateSessionError)
                .DisposeWith(disposables);
            
            _cloudSessionConnectionRepository.ConnectionStatusObservable
                .Select(x => x == SessionConnectionStatus.CreatingSession)
                .ToPropertyEx(this, x => x.IsCreatingCloudSession)
                .DisposeWith(disposables);
            
            _cloudSessionConnectionRepository.ConnectionStatusObservable
                .Select(x => x == SessionConnectionStatus.JoiningSession)
                .ToPropertyEx(this, x => x.IsJoiningCloudSession)
                .DisposeWith(disposables);
        });
    }

    public ReactiveCommand<Unit, Unit>? CreateCloudSessionCommand { get; set; }
    
    public ReactiveCommand<Unit, Unit>? CancelCloudSessionCreationCommand { get; set; }
    
    public extern bool IsCreatingCloudSession { [ObservableAsProperty] get; }
    
    public extern bool IsJoiningCloudSession { [ObservableAsProperty] get; }
    
    [Reactive]
    public string? ErrorMessage { get; set; }

    [Reactive]
    public string? ErrorMessageSource { get; set; }
    
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

    private void OnCreateSessionError(CreateSessionError? createSessionError)
    {
        if (createSessionError == null)
        {
            UpdateErrorMessage(null);
            return;
        }
        
        switch (createSessionError.Status)
        {
            case CreateSessionStatus.Error:
                UpdateErrorMessage(nameof(Resources.CreateCloudSession_UnknownError), createSessionError.Exception);
                break;
        }
    }
    
    private void UpdateErrorMessage(string? errorMessageSource, Exception? exception = null)
    {
        ErrorMessageSource = errorMessageSource;
        if (ErrorMessageSource.IsNotEmpty())
        {
            string errorMessage = _localizationService[ErrorMessageSource!];
            if (exception != null)
            {
                errorMessage += $" ({exception.Message})";
            }

            ErrorMessage = errorMessage;
        }
        else
        {
            ErrorMessage = "";
        }
    }
}