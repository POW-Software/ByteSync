using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.Mixins;
using ByteSync.Assets.Resources;
using ByteSync.Business.Events;
using ByteSync.Business.Navigations;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Navigations;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.EventsHubs;
using ByteSync.Interfaces.Services.Sessions.Connecting;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace ByteSync.ViewModels.Sessions.Cloud.Managing;

public class JoinCloudSessionViewModel : ActivatableViewModelBase
{
    private readonly ICloudSessionConnector _cloudSessionConnector;
    private readonly ICloudSessionEventsHub _cloudSessionEventsHub;
    private readonly ILocalizationService _localizationService;
    private readonly INavigationService _navigationService;

    public JoinCloudSessionViewModel()
    {
        
    }
    
    public JoinCloudSessionViewModel(ICloudSessionConnector cloudSessionConnector, ICloudSessionEventsHub cloudSessionEventsHub, 
        INavigationService navigationService, ILocalizationService localizationService)
    {
        _cloudSessionConnector = cloudSessionConnector;
        _cloudSessionEventsHub = cloudSessionEventsHub;
        _navigationService = navigationService;
        _localizationService = localizationService;

        SessionId = "";
        SessionPassword = "";

        AreControlsEnabled = true;

        var canJoin = this.WhenAnyValue(x => x.AreControlsEnabled, (bool areControlsEnabled) => areControlsEnabled) ;
        JoinCommand = ReactiveCommand.CreateFromTask(Join, canJoin);
        CancelCommand = ReactiveCommand.Create(() => { _navigationService.NavigateTo(NavigationPanel.Home); });
        
        this.WhenActivated(disposables =>
        {
            Observable.FromEventPattern<GenericEventArgs<JoinSessionResult>>(_cloudSessionEventsHub, nameof(_cloudSessionEventsHub.JoinCloudSessionFailed))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(evt => OnCloudSessionJoinError(evt.EventArgs.Value))
                .DisposeWith(disposables);
            
            _localizationService.CurrentCultureObservable
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => OnLocaleChanged())
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
    
    [Reactive]
    public bool AreControlsEnabled { get; set; }

    private async Task Join()
    {
        try
        {
            UpdateErrorMessage(null);
        #if DEBUG

            if (SessionId.IsNullOrEmpty() && SessionPassword.IsNullOrEmpty())
            {
                var clipboard = Application.Current?.Clipboard;

                if (clipboard != null)
                {
                    var clipBoardText = await clipboard.GetTextAsync();

                    if (clipBoardText.IsNotEmpty() && clipBoardText.Contains("||") && clipBoardText.Length == 16)
                    {
                        SessionId = clipBoardText.Substring(0, 9);
                        SessionPassword = clipBoardText.Substring(11);
                    }
                }
            }

        #endif
            if (SessionId.IsNullOrEmpty())
            {
                UpdateErrorMessage(nameof(Resources.JoinSession_LoginMissing));
                return;
            }

            if (SessionPassword.IsNullOrEmpty())
            {
                UpdateErrorMessage(nameof(Resources.JoinSession_PasswordMissing));
                return;
            }

            AreControlsEnabled = false;

            await _cloudSessionConnector.JoinSession(SessionId, SessionPassword, null);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Can not join session");
            UpdateErrorMessage(nameof(Resources.JoinSession_ErrorMessage));
        }
        finally
        {
            AreControlsEnabled = true;
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

    private void OnCloudSessionJoinError(JoinSessionResult joinSessionResult)
    {
        switch (joinSessionResult.Status)
        {
            case JoinSessionStatuses.SessionNotFound:
                UpdateErrorMessage(nameof(Resources.JoinSession_SessionNotFound));
                break;
            
            case JoinSessionStatuses.ServerError:
                UpdateErrorMessage(nameof(Resources.JoinSession_ServerError));
                break;
            
            case JoinSessionStatuses.TransientError:
                UpdateErrorMessage(nameof(Resources.JoinSession_TransientError));
                break;
            
            case JoinSessionStatuses.TooManyMembers:
                UpdateErrorMessage(nameof(Resources.JoinSession_TooManyMembers));
                break;
            
            case JoinSessionStatuses.SessionAlreadyActivated:
                UpdateErrorMessage(nameof(Resources.JoinSession_SessionAlreadyActivated));
                break;
            
            case JoinSessionStatuses.TrustCheckFailed:
                UpdateErrorMessage(nameof(Resources.JoinSession_TrustCheckFailed));
                break;
            
            default:
                UpdateErrorMessage(nameof(Resources.JoinSession_UnkownError));
                break;
        }
    }
}