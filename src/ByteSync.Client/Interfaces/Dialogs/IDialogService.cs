using System.Threading.Tasks;
using ByteSync.Business;
using ByteSync.ViewModels.Misc;

namespace ByteSync.Interfaces.Dialogs;

public interface IDialogService
{
    MessageBoxViewModel CreateMessageBoxViewModel(string titleKey, string? messageKey = null, params string[]? messageArguments);
        
    Task<MessageBoxResult?> ShowMessageBoxAsync(MessageBoxViewModel messageBoxViewModel);

    void ShowFlyout(string titleKey, bool toggle, FlyoutElementViewModel flyout);
    
    void CloseFlyout();
}