using System.Threading.Tasks;
using ByteSync.Business;
using ByteSync.Interfaces.Dialogs;
using ByteSync.ViewModels.Misc;

namespace ByteSync.Services.Dialogs;

public class DialogService : IDialogService
{
    private readonly IDialogView _dialogView;
    private readonly IMessageBoxViewModelFactory _messageBoxViewModelFactory;

    public DialogService(IDialogView dialogView, IMessageBoxViewModelFactory messageBoxViewModelFactory)
    {
        _dialogView = dialogView;
        _messageBoxViewModelFactory = messageBoxViewModelFactory;
    }
    
    public MessageBoxViewModel CreateMessageBoxViewModel(string titleKey, string? messageKey = null, params string[]? messageArguments)
    {
        return _messageBoxViewModelFactory.CreateMessageBoxViewModel(titleKey, messageKey, messageArguments);
    }

    public Task<MessageBoxResult?> ShowMessageBoxAsync(MessageBoxViewModel messageBoxViewModel)
    {
        return _dialogView.ShowMessageBoxAsync(messageBoxViewModel);
    }
    
    public void ShowFlyout(string titleKey, bool toggle, FlyoutElementViewModel flyout)
    {
        _dialogView.ShowFlyout(titleKey, toggle, flyout);
    }
}