using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using ByteSync.Assets.Resources;
using ByteSync.Business;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Dialogs;
using ByteSync.ViewModels.Misc;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace ByteSync.ViewModels.TrustedNetworks;

public class TrustedNetworkViewModel : FlyoutElementViewModel
{
    private readonly IPublicKeysManager _publicKeysManager;
    private readonly IApplicationSettingsRepository _applicationSettingsRepository;
    private readonly IMessageBoxViewModelFactory _messageBoxViewModelFactory;

    public TrustedNetworkViewModel()
    {
    #if DEBUG
        if (Design.IsDesignMode)
        {
            var publicKeyFormatter = new PublicKeyFormatter();
            MyPublicKey = publicKeyFormatter.Format(new byte[]{2, 5, 8, 9});

            MyClientId = "ABCD-EFG-HIJ";
        }
    #endif

        TrustedPublicKeys = new TrustedPublicKeysViewModel();
    }

    public TrustedNetworkViewModel(IPublicKeysManager publicKeysManager, 
        IApplicationSettingsRepository applicationSettingsManager, TrustedPublicKeysViewModel trustedPublicKeysViewModel,
        IMessageBoxViewModelFactory messageBoxViewModelFactory) 
    {
    #if DEBUG
        if (Design.IsDesignMode)
        {
            return;
        }
    #endif

        TrustedPublicKeys = trustedPublicKeysViewModel;

        _publicKeysManager = publicKeysManager;
        _applicationSettingsRepository = applicationSettingsManager;
        _messageBoxViewModelFactory = messageBoxViewModelFactory;
        
        var canRun = new BehaviorSubject<bool>(true);
        RenewPublicKeyCommand = ReactiveCommand.CreateFromTask(RenewPublicKey, canRun);
        DeleteTrustedPublicKeyCommand = ReactiveCommand.CreateFromTask<TrustedPublicKeyViewModel>(DeleteTrustedPublicKey, canRun);
        Observable.Merge(RenewPublicKeyCommand.IsExecuting, DeleteTrustedPublicKeyCommand.IsExecuting)
            .Select(executing => !executing).Subscribe(canRun);
        
        SetMyClient();
    }

    [Reactive]
    internal string? MyClientId { get; set; }

    [Reactive]
    internal string MyPublicKey { get; set; }

    [Reactive]
    internal TrustedPublicKeysViewModel TrustedPublicKeys { get; set; }
    
    [Reactive]
    internal MessageBoxViewModel? ConfirmationQuestion { get; set; }
    
    public ReactiveCommand<Unit, Unit> RenewPublicKeyCommand { get; }
    
    public ReactiveCommand<TrustedPublicKeyViewModel, Unit> DeleteTrustedPublicKeyCommand { get; set; }
    
    private async Task RenewPublicKey()
    {
        await AskAndDo(nameof(Resources.TrustedPublicKeyView_RenewPublicKey_Title), nameof(Resources.TrustedPublicKeyView_RenewPublicKey_Message),
            null, () => _publicKeysManager.InitializeRsaAndTrustedPublicKeys());

        SetMyClient();
        
        TrustedPublicKeys.Refresh();
    }
    
    private async Task DeleteTrustedPublicKey(TrustedPublicKeyViewModel trustedPublicKeyViewModel)
    {
        await AskAndDo(nameof(Resources.TrustedPublicKeyView_DeleteTrustedPublicKey_Title), 
            nameof(Resources.TrustedPublicKeyView_DeleteTrustedPublicKey_Message), 
            new string[] {trustedPublicKeyViewModel.ClientId, trustedPublicKeyViewModel.TrustedPublicKey.PublicKeyHash},
            () => _publicKeysManager.Delete(trustedPublicKeyViewModel.TrustedPublicKey));

        TrustedPublicKeys.Refresh();
    }

    private async Task AskAndDo(string title, string message, string[]? messageArguments, Action action, 
        [CallerMemberName] string caller = "")
    {
        try
        {
            ConfirmationQuestion = _messageBoxViewModelFactory.CreateMessageBoxViewModel(title, message, messageArguments);
            ConfirmationQuestion.ShowYesNo = true;

            var result = await WaitForResponse(ConfirmationQuestion);

            if (result == MessageBoxResult.Yes)
            {
                action.Invoke();
            }
        }
        catch (Exception ex)
        {
            // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
            Log.Error(ex, caller);
        }
    }
    
    private void SetMyClient()
    {
        MyClientId = _applicationSettingsRepository.GetCurrentApplicationSettings().ClientId;
        
        var publicKeyFormatter = new PublicKeyFormatter();
        MyPublicKey = publicKeyFormatter.Format(_publicKeysManager.GetMyPublicKeyInfo().PublicKey);
    }

    private async Task<MessageBoxResult?> WaitForResponse(MessageBoxViewModel messageBoxViewModel)
    {
        var result = await messageBoxViewModel.WaitForResponse();
        
        await Dispatcher.UIThread.InvokeAsync(() => ConfirmationQuestion = null);

        return result;
    }
}