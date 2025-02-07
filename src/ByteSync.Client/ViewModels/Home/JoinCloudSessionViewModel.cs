using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ByteSync.Assets.Resources;
using ByteSync.Business.Events;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions.Connecting;
using ByteSync.Interfaces.Services.Sessions.Connecting.Joining;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ByteSync.ViewModels.Home;

public class JoinCloudSessionViewModel : ActivatableViewModelBase
{
    private readonly IJoinSessionService _joinSessionService;
    private readonly ILocalizationService _localizationService;
    private readonly ICloudSessionConnectionRepository _cloudSessionConnectionRepository;
    private readonly ILogger<JoinCloudSessionViewModel> _logger;
    
    public JoinCloudSessionViewModel()
    {
        
    }
    
    public JoinCloudSessionViewModel(IJoinSessionService joinSessionService, 
        ILocalizationService localizationService, ICloudSessionConnectionRepository cloudSessionConnectionRepository, 
        ILogger<JoinCloudSessionViewModel> logger)
    {
        _joinSessionService = joinSessionService;
        _localizationService = localizationService;
        _cloudSessionConnectionRepository = cloudSessionConnectionRepository;
        _logger = logger;

        SessionId = "";
        SessionPassword = "";
        
        JoinCommand = ReactiveCommand.CreateFromTask(JoinCloudSession);
        CancelCommand =  ReactiveCommand.CreateFromTask(CancelJoinCloudSession);
        
        this.WhenActivated(disposables =>
        {
            _cloudSessionConnectionRepository.JoinSessionErrorObservable
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(OnCloudSessionJoinError)
                .DisposeWith(disposables);           
            
            _localizationService.CurrentCultureObservable
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => OnLocaleChanged())
                .DisposeWith(disposables);
            
            _cloudSessionConnectionRepository.ConnectionStatusObservable
                .Select(x => x == SessionConnectionStatus.JoiningSession)
                .ToPropertyEx(this, x => x.IsJoiningCloudSession)
                .DisposeWith(disposables);
            
            _cloudSessionConnectionRepository.ConnectionStatusObservable
                .Select(x => x == SessionConnectionStatus.CreatingSession)
                .ToPropertyEx(this, x => x.IsCreatingCloudSession)
                .DisposeWith(disposables);
            
        });
    }

    public ReactiveCommand<Unit, Unit> JoinCommand { get; set; }
        
    public ReactiveCommand<Unit, Unit> CancelCommand { get; set; }

    [Reactive]
    public string SessionId { get; set; }

    [Reactive]
    public string SessionPassword { get; set; }
    
    [Reactive]
    public string? ErrorMessage { get; set; }

    [Reactive]
    public string? ErrorMessageSource { get; set; }
    
    public extern bool IsJoiningCloudSession { [ObservableAsProperty] get; }
        
    public extern bool IsCreatingCloudSession { [ObservableAsProperty] get; }
    
    private async Task JoinCloudSession()
    {
        try
        {
            UpdateErrorMessage(null);

            if (SessionId.IsNullOrEmpty())
            {
                UpdateErrorMessage(nameof(Resources.JoinCloudSession_LoginMissing));
                return;
            }

            if (SessionPassword.IsNullOrEmpty())
            {
                UpdateErrorMessage(nameof(Resources.JoinCloudSession_PasswordMissing));
                return;
            }

            await _joinSessionService.JoinSession(SessionId, SessionPassword, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot join Cloud Session");
            UpdateErrorMessage(nameof(Resources.JoinCloudSession_ErrorMessage));
        }
    }
    
    private async Task CancelJoinCloudSession()
    {
        try
        {
            await _joinSessionService.CancelJoinCloudSession();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CancelJoinCloudSession");
        }
    }
    
    private void OnLocaleChanged()
    {
        if (ErrorMessageSource.IsNotEmpty())
        {
            ErrorMessage = _localizationService[ErrorMessageSource!];
        }
    }

    private void UpdateErrorMessage(string? errorMessageSource)
    {
        ErrorMessageSource = errorMessageSource;
        if (ErrorMessageSource.IsNotEmpty())
        {
            ErrorMessage = _localizationService[ErrorMessageSource!];
        }
        else
        {
            ErrorMessage = "";
        }
    }

    private void OnCloudSessionJoinError(JoinSessionResult? joinSessionResult)
    {
        if (joinSessionResult == null)
        {
            UpdateErrorMessage(null);
            return;
        }
        
        switch (joinSessionResult.Status)
        {
            case JoinSessionStatus.SessionNotFound:
                UpdateErrorMessage(nameof(Resources.JoinCloudSession_SessionNotFound));
                break;
            
            case JoinSessionStatus.WrongPassword:
                UpdateErrorMessage(nameof(Resources.JoinCloudSession_WrongPassword));
                break;

            case JoinSessionStatus.ServerError:
                UpdateErrorMessage(nameof(Resources.JoinCloudSession_ServerError));
                break;

            case JoinSessionStatus.TransientError:
                UpdateErrorMessage(nameof(Resources.JoinCloudSession_TransientError));
                break;

            case JoinSessionStatus.TooManyMembers:
                UpdateErrorMessage(nameof(Resources.JoinCloudSession_TooManyMembers));
                break;

            case JoinSessionStatus.SessionAlreadyActivated:
                UpdateErrorMessage(nameof(Resources.JoinCloudSession_SessionAlreadyActivated));
                break;

            case JoinSessionStatus.TrustCheckFailed:
                UpdateErrorMessage(nameof(Resources.JoinCloudSession_TrustCheckFailed));
                break;

            default:
                UpdateErrorMessage(nameof(Resources.JoinCloudSession_UnkownError));
                break;
        }
    }
}