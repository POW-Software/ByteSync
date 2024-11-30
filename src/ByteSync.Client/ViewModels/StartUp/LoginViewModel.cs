using System.Reactive;
using System.Threading.Tasks;
using ByteSync.Assets.Resources;
using ByteSync.Common.Business.Auth;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Updates;
using ByteSync.Services.Misc;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Splat;

namespace ByteSync.ViewModels.StartUp;

public class LoginViewModel : ViewModelBase, IRoutableViewModel 
{
    private readonly IApplicationSettingsRepository _applicationSettingsRepository;
    private readonly IWebAccessor _webAccessor;
    private readonly IUpdateService _updateService;
    private readonly ILocalizationService _localizationService;
    private readonly IEnvironmentService _environmentService;

    public LoginViewModel() : this (null)
    {
        
    }

    public LoginViewModel(IScreen? screen = null, IApplicationSettingsRepository? userSettingsManager = null,
        IWebAccessor? webAccessor = null, IUpdateService? updateManager = null, ILocalizationService? localizationService = null, 
        IEnvironmentService? environmentService = null)
    {
        HostScreen = screen ?? Locator.Current.GetService<IScreen>()!;
        
        _applicationSettingsRepository = userSettingsManager ?? Locator.Current.GetService<IApplicationSettingsRepository>()!;
        _webAccessor = webAccessor ?? Locator.Current.GetService<IWebAccessor>()!;
        _localizationService = localizationService ?? Locator.Current.GetService<ILocalizationService>()!;
        _environmentService = environmentService ?? Locator.Current.GetService<IEnvironmentService>()!;
            
        _updateService = updateManager ?? Locator.Current.GetService<IUpdateService>()!;
        // _dialogService = dialogService ?? Locator.Current.GetService<IDialogService>()!;
            
        var canSignIn = this.WhenAnyValue(x => x.AreControlsEnabled, x => x.AgreesBetaWarning,
            (areControlsEnabled, agreesBetaWarning) => 
                areControlsEnabled && agreesBetaWarning);
        SignInCommand = ReactiveCommand.Create(SignIn, canSignIn);

        OpenPricingCommand = ReactiveCommand.Create(OpenPricing);
        OpenPrivacyCommand = ReactiveCommand.Create(OpenPrivacy);
        OpenTermsOfUseCommand = ReactiveCommand.Create(OpenTermsOfUse);
        
        OpenCurrentVersionReleaseNotesCommand = ReactiveCommand.Create(OpenCurrentVersionReleaseNotes);
        OpenAboutTheOpenBetaCommand = ReactiveCommand.Create(OpenAboutTheOpenBeta);

        WarningContent = "";
        AreControlsEnabled = true;

        IsBetaVersion = false;
    #if DEBUG 
        // IsBetaVersion = false;
    #endif

        Version = VersionHelper.GetVersionString(_environmentService.CurrentVersion);

        var userSettings = _applicationSettingsRepository.GetCurrentApplicationSettings();
        Email = userSettings.DecodedEmail;
        Serial = userSettings.DecodedSerial;
        
        AgreesBetaWarning = userSettings.AgreesBetaWarning0;
    #if DEBUG
        AgreesBetaWarning = true;
    #endif

        WaitForNextVersion();
    }

    public string? UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);
    public IScreen HostScreen { get; }

    public ReactiveCommand<Unit, Unit> SignInCommand { get; private set; }

    public ReactiveCommand<Unit, Unit> OpenPricingCommand { get; private set; }

    public ReactiveCommand<Unit, Unit> OpenPrivacyCommand { get; private set; }

    public ReactiveCommand<Unit, Unit> OpenTermsOfUseCommand { get; private set; }
    
    public ReactiveCommand<Unit, Unit> OpenCurrentVersionReleaseNotesCommand { get; private set; }
    
    public ReactiveCommand<Unit, Unit> OpenAboutTheOpenBetaCommand { get; private set; }

    [Reactive]
    public string? Email { get; set; }

    [Reactive]
    public string? Serial { get; set; }

    [Reactive]
    public string WarningContent { get; set; }

    [Reactive]
    public bool AreControlsEnabled { get; set; }

    [Reactive]
    public bool IsBetaVersion { get; set; }

    [Reactive]
    public bool AgreesBetaWarning { get; set; }

    [Reactive]
    public string Version { get; set; }

    private async void SignIn()
    {
        if (Email.IsNullOrEmpty() && Serial.IsNullOrEmpty())
        {
            ShowSigninWarning(_localizationService[nameof(Resources.LoginForm_EmailSerialEmpty)]);
        }
        else if (Email.IsNullOrEmpty())
        {
            ShowSigninWarning(_localizationService[nameof(Resources.LoginForm_EmailEmpty)]);
        }
        else if (Serial.IsNullOrEmpty())
        {
            ShowSigninWarning(_localizationService[nameof(Resources.LoginForm_SerialEmpty)]);
        }
        else if (!AgreesBetaWarning)
        {
            return;
        }
        else
        {
            WarningContent = "";
        }

        if (WarningContent.IsEmpty())
        {
            try
            {
                AreControlsEnabled = false;
                
                _applicationSettingsRepository.UpdateCurrentApplicationSettings(
                    settings => settings.AgreesBetaWarning0 = AgreesBetaWarning,
                    false);
            }
            catch (TaskCanceledException tce)
            {
                Log.Error(tce, "Unable to login");

                ShowSigninWarning(tce.Message);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to login");

                ShowSigninWarning(ex.Message);
            }
            finally
            {
                AreControlsEnabled = true;
            }
        }
    }

    private void ShowSigninWarning(InitialAuthenticationResponse initialAuthenticationResponse)
    {
        List<string> stringWarnings = new List<string>();

        switch (initialAuthenticationResponse.InitialConnectionStatus)
        {
            case InitialConnectionStatus.VersionNotAllowed:
                stringWarnings.Add(
                    _localizationService[nameof(Resources.Login_SigninError_VersionNotAllowed)]);
                break;
            case InitialConnectionStatus.UnknownOsPlatform: // need proper warning?
                stringWarnings.Add(
                    _localizationService[nameof(Resources.Login_SigninError_UnknownError)]);
                break;
            case InitialConnectionStatus.UnknownError:
                stringWarnings.Add(
                    _localizationService[nameof(Resources.Login_SigninError_UnknownError)]);
                break;
        }

        var reasons = string.Join(_localizationService[nameof(Resources.Misc_SeparatorComma)],
            stringWarnings);

        ShowSigninWarning(reasons);
    }

    private void ShowSigninWarning(string reason)
    {
        if (reason.IsNullOrEmpty())
        {
            reason = _localizationService[nameof(Resources.Login_SigninError_UnknownError)];
        }

        if (!reason!.EndsWith(_localizationService[nameof(Resources.Misc_SeparatorDot)].Trim()))
        {
            reason += _localizationService[nameof(Resources.Misc_SeparatorDot)];
            reason = reason.Trim();
        }

        reason = reason.LowercaseFirst();

        WarningContent = String.Format(_localizationService[nameof(Resources.Login_SigninError_Unable)], reason);
    }


    private void OpenPricing()
    {
        if (IsBetaVersion)
        {
            _webAccessor.OpenJoinBeta();
        }
        else
        {
            _webAccessor.OpenPricing();
        }
    }
    
    private void OpenPrivacy()
    {
        _webAccessor.OpenPrivacy();
    }

    private void OpenTermsOfUse()
    {
        _webAccessor.OpenTermsOfUse();
    }
    
    private void OpenCurrentVersionReleaseNotes()
    {
        _webAccessor.OpenReleaseNotes(_environmentService.CurrentVersion);
    }

    private void OpenAboutTheOpenBeta()
    {
        _webAccessor.OpenAboutOpenBeta();
    }

    private async void WaitForNextVersion()
    {
        try
        {
            await _updateService.SearchNextAvailableVersionsAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, nameof(WaitForNextVersion));
        }
    }
}