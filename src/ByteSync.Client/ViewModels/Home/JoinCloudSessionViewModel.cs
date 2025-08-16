using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ByteSync.Assets.Resources;
using ByteSync.Business.Events;
using ByteSync.Business.Sessions;
using ByteSync.Business.Sessions.Connecting;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions.Connecting;
using ByteSync.Interfaces.Services.Sessions.Connecting.Joining;
using Microsoft.Extensions.Logging;
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
            UpdateErrorMessage(nameof(Resources.JoinCloudSession_ErrorMessage), ex);
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

    private void UpdateErrorMessage(string? errorMessageSource, Exception? exception = null)
    {
        ErrorMessageSource = errorMessageSource;
        if (ErrorMessageSource.IsNotEmpty())
        {
            string errorMessage = _localizationService[ErrorMessageSource!];
            if (exception != null)
            {
                _logger.LogError(exception, "Session join error occurred");
                // Don't expose exception details to the user for security reasons
            }

            ErrorMessage = errorMessage;
        }
        else
        {
            ErrorMessage = "";
        }
    }

    private void OnCloudSessionJoinError(JoinSessionError? joinSessionError)
    {
        if (joinSessionError == null)
        {
            UpdateErrorMessage(null);
            return;
        }
        
        switch (joinSessionError.Status)
        {
            case JoinSessionStatus.SessionNotFound:
                UpdateErrorMessage(nameof(Resources.JoinCloudSession_SessionNotFound), joinSessionError.Exception);
                break;
            
            case JoinSessionStatus.WrongPassword:
                UpdateErrorMessage(nameof(Resources.JoinCloudSession_WrongPassword), joinSessionError.Exception);
                break;

            case JoinSessionStatus.ServerError:
                UpdateErrorMessage(nameof(Resources.JoinCloudSession_ServerError), joinSessionError.Exception);
                break;

            case JoinSessionStatus.TransientError:
                UpdateErrorMessage(nameof(Resources.JoinCloudSession_TransientError), joinSessionError.Exception);
                break;

            case JoinSessionStatus.TooManyMembers:
                UpdateErrorMessage(nameof(Resources.JoinCloudSession_TooManyMembers), joinSessionError.Exception);
                break;

            case JoinSessionStatus.SessionAlreadyActivated:
                UpdateErrorMessage(nameof(Resources.JoinCloudSession_SessionAlreadyActivated), joinSessionError.Exception);
                break;

            case JoinSessionStatus.TrustCheckFailed:
                UpdateErrorMessage(nameof(Resources.JoinCloudSession_TrustCheckFailed), joinSessionError.Exception);
                break;
            
            case JoinSessionStatus.TimeoutError:
                UpdateErrorMessage(nameof(Resources.JoinCloudSession_TimeoutError), joinSessionError.Exception);
                break;
            
            case JoinSessionStatus.CanceledByUser:
                UpdateErrorMessage(nameof(Resources.JoinCloudSession_CanceledByUser));
                break;

            default:
                UpdateErrorMessage(nameof(Resources.JoinCloudSession_UnkownError), joinSessionError.Exception);
                break;
        }
    }
}