using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using ByteSync.Business;
using ByteSync.Business.Communications;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.EventsHubs;
using ByteSync.Services.Communications;
using ByteSync.ViewModels.Misc;
using ByteSync.ViewModels.TrustedNetworks;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Splat;

namespace ByteSync.ViewModels.Sessions.Cloud.Members;

public class AddTrustedClientViewModel : FlyoutElementViewModel
{
    private readonly IPublicKeysManager _publicKeysManager;
    private readonly INavigationEventsHub _navigationEventsHub;
    private readonly IApplicationSettingsRepository _applicationSettingsRepository;
    private readonly IPublicKeysTruster _publicKeysTruster;

    public AddTrustedClientViewModel() : this(null, new TrustDataParameters())
    {
    #if DEBUG
        if (Design.IsDesignMode)
        {
            // BuildIntroduction();
            MyClientId = Environment.MachineName;
            IsWaitingForOtherParty = true;
            IsJoinerSide = true;
            // IpAddress = "0.0.0.1";

            var safetyKey = "";
            for (var i = 0; i < 16; i++)
            {
                safetyKey += "string_" + i + " ";
            }

            SafetyKeyParts = safetyKey.Split(" ", StringSplitOptions.RemoveEmptyEntries);

            IsClipboardCheckError = true;
        }
    #endif
    }

    public AddTrustedClientViewModel(PublicKeyCheckData? publicKeyCheckData, TrustDataParameters trustDataParameters,
        IPublicKeysManager? publicKeysManager = null, INavigationEventsHub? navigationEventsHub = null, 
        IApplicationSettingsRepository? applicationSettingsManager = null, IPublicKeysTruster? publicKeysTruster = null) 
    {
    #if DEBUG
        if (Design.IsDesignMode)
        {
            return;
        }
    #endif
        
        _publicKeysManager = publicKeysManager ?? Locator.Current.GetService<IPublicKeysManager>()!;
        _navigationEventsHub = navigationEventsHub ?? Locator.Current.GetService<INavigationEventsHub>()!;
        _applicationSettingsRepository = applicationSettingsManager ?? Locator.Current.GetService<IApplicationSettingsRepository>()!;
        _publicKeysTruster = publicKeysTruster ?? Locator.Current.GetService<IPublicKeysTruster>()!;

        if (publicKeyCheckData == null)
        {
            throw new ArgumentNullException(nameof(publicKeyCheckData));
        }
        
        MyClientId = _applicationSettingsRepository.GetCurrentApplicationSettings().ClientId;
        OtherClientId = publicKeyCheckData.IssuerPublicKeyInfo.ClientId;
        
        var publicKeyFormatter = new PublicKeyFormatter();
        MyPublicKey = publicKeyFormatter.Format(_publicKeysManager.GetMyPublicKeyInfo().PublicKey);
        OtherClientKey = publicKeyFormatter.Format(publicKeyCheckData.IssuerPublicKeyInfo.PublicKey);
        
        ShowSuccess = false;
        ShowError = false;

        TrustedPublicKey = _publicKeysManager.BuildTrustedPublicKey(publicKeyCheckData);
        
        SafetyKey = TrustedPublicKey.SafetyKey;
        SafetyKeyParts = BuildSafetyWords();
        
        PublicKeyCheckData = publicKeyCheckData;
        IsJoinerSide = trustDataParameters.IsJoinerSide;
        TrustDataParameters = trustDataParameters;

        var canRun = new BehaviorSubject<bool>(true);
        CopyToClipboardCommand = ReactiveCommand.CreateFromTask(CopyToClipboard, canRun);
        CheckClipboardCommand = ReactiveCommand.CreateFromTask(CheckClipboard, canRun);
        ValidateClientCommand = ReactiveCommand.CreateFromTask(ValidateClient, canRun);
        RejectClientCommand = ReactiveCommand.CreateFromTask(RejectClient, canRun);
        
        CancelCommand = ReactiveCommand.CreateFromTask(Cancel);
        
        Observable.Merge(CopyToClipboardCommand.IsExecuting, CheckClipboardCommand.IsExecuting, 
                ValidateClientCommand.IsExecuting, RejectClientCommand.IsExecuting,
                CancelCommand.IsExecuting)
                    .Select(executing => !executing).Subscribe(canRun);
    }

    public TrustedPublicKey TrustedPublicKey { get; set; }

    public PublicKeyCheckData? PublicKeyCheckData { get; }
    
    public bool IsJoinerSide { get; set; }
    
    public TrustDataParameters TrustDataParameters { get; set; }

    public ReactiveCommand<Unit, Unit> CopyToClipboardCommand { get; }
    
    public ReactiveCommand<Unit, Unit> CheckClipboardCommand { get; }
    
    public ReactiveCommand<Unit, Unit> ValidateClientCommand { get; }
    
    public ReactiveCommand<Unit, Unit> RejectClientCommand { get; }
    
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    [Reactive]
    public string SafetyKey { get; set; }
    
    [Reactive]
    public string[] SafetyKeyParts { get; set; }
    
    [Reactive]
    public string? MyClientId { get; set; }    
    
    [Reactive]
    internal string MyPublicKey { get; set; }
    
    [Reactive]
    public string? OtherClientId { get; set; }
    
    [Reactive]
    internal string OtherClientKey { get; set; }

    [Reactive]
    public bool ShowSuccess { get; set; }
    
    [Reactive]
    public bool ShowError { get; set; }
    
    [Reactive]
    public bool IsClipboardCheckOK { get; set; }
    
    [Reactive]
    public bool IsClipboardCheckError { get; set; }

    [Reactive]
    public bool IsCopyToClipboardOK { get; set; }
    
    [Reactive]
    public bool IsCopyToClipboardError { get; set; }

    [Reactive]
    public bool IsWaitingForOtherParty { get; set; }
    
    public override void OnDisplayed()
    {
        base.OnDisplayed();
        
    #if DEBUG
        if (Design.IsDesignMode)
        {
            return;
        }
    #endif

        Container.CanCloseCurrentFlyout = false;
    }

    private async Task CopyToClipboard()
    {
        IsClipboardCheckOK = false;
        IsClipboardCheckError = false;
        
        try
        {
            var clipboard = Application.Current?.Clipboard;

            if (clipboard != null)
            {
                await clipboard.SetTextAsync(SafetyKeyParts.JoinToString(" "));

                IsCopyToClipboardOK = true;
                IsClipboardCheckError = false;
            }
            else
            {
                Log.Warning("CopyToClipboard: unable to acess clipboard");
                
                IsCopyToClipboardOK = false;
                IsClipboardCheckError = true;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "CopyToClipboard error");
            
            IsCopyToClipboardOK = false;
            IsClipboardCheckError = true;
        }
    }
    
    private async Task CheckClipboard()
    {
        IsCopyToClipboardOK = false;
        IsCopyToClipboardError = false;
        
        try
        {
            var clipboard = Application.Current?.Clipboard;

            if (clipboard != null)
            {
                var clipBoardValue = await clipboard.GetTextAsync();

                var isSuccess = SafetyKeyParts.Length > 1 && SafetyKeyParts.JoinToString(" ").Equals(clipBoardValue);

                IsClipboardCheckOK = isSuccess;
                IsClipboardCheckError = !isSuccess;
            }
            else
            {
                Log.Warning("CheckClipboard: unable to acess clipboard");
                
                IsClipboardCheckOK = false;
                IsClipboardCheckError = true;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "CheckClipboard error");
            
            IsClipboardCheckOK = false;
            IsClipboardCheckError = true;
        }
    }

    
    private async Task ValidateClient()
    {
        try
        {
            await _publicKeysTruster.OnPublicKeyValidationFinished(PublicKeyCheckData!, TrustDataParameters, true);

            bool isSuccess;
            if (!TrustDataParameters.PeerTrustProcessData.OtherPartyHasFinished())
            {
                IsWaitingForOtherParty = true;

                isSuccess = await TrustDataParameters.PeerTrustProcessData.WaitForPeerTrustProcessFinished();

                // bool isSuccess = await _trustProcessPublicKeysHolder.WaitForPeerTrustProcessFinished(TrustDataParameters.SessionId);

                IsWaitingForOtherParty = false;
            }
            else
            {
                isSuccess = TrustDataParameters.PeerTrustProcessData.IsPeerTrustSuccess;
            }

            if (isSuccess)
            {
                _publicKeysManager.Trust(TrustedPublicKey);

                ShowSuccess = true;
                await Task.Delay(TimeSpan.FromSeconds(3));
                ShowSuccess = false;
            }
            else
            {
                ShowError = true;
                await Task.Delay(TimeSpan.FromSeconds(3));
                ShowError = false;
            }

            // 08/12/2022 : On ferme systématiquement (notamment pour permettre remplacement par nouveau flyout de validation)
            Container.CanCloseCurrentFlyout = true;
            _navigationEventsHub.RaiseCloseFlyoutRequested();
            // if (!isSuccess || TrustDataParameters.CurrentClientIndex == TrustDataParameters.ClientsCount)
            // {
            //     Container.CanCloseCurrentFlyout = true;
            //     _navigationEventsHub.RaiseCloseFlyoutRequested();
            // }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ValidateClient");
        }
    }
    
    private async Task RejectClient()
    {
        try
        {
            Log.Warning("Current user rejected Public Key {@publicKey}", TrustedPublicKey);
        
            var task = _publicKeysTruster.OnPublicKeyValidationFinished(PublicKeyCheckData!, TrustDataParameters, false);
            // On annule aussi, sinon, on continue à attendre la réponse de l'autre
            var task2 = _publicKeysTruster.OnPublicKeyValidationCanceled(PublicKeyCheckData!, TrustDataParameters);
        
            ShowError = true;
            await Task.Delay(TimeSpan.FromSeconds(3));
            ShowError = false;

            await Task.WhenAll(task, task2);
        
            Container.CanCloseCurrentFlyout = true;
            _navigationEventsHub.RaiseCloseFlyoutRequested();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "RejectClient");
        }
    }
    
    private async Task Cancel()
    {
        Log.Warning("Current user cancelled waiting for Public Key {@publicKey} cross check", TrustedPublicKey);
        
        await _publicKeysTruster.OnPublicKeyValidationCanceled(PublicKeyCheckData!, TrustDataParameters);
    }

    private string[] BuildSafetyWords()
    {
        var safetyWordsComputer = new SafetyWordsComputer(SafetyWordsValues.AVAILABLE_WORDS);

        return safetyWordsComputer.Compute(TrustedPublicKey.SafetyKey);
    }
}