using System.Threading.Tasks;
using ByteSync.Business;
using ByteSync.ViewModels.Misc;

namespace ByteSync.Interfaces.Dialogs;

public interface IDialogView
{
    Task<MessageBoxResult?> ShowMessageBoxAsync(MessageBoxViewModel messageBoxViewModel);

    void ShowFlyout(string titleKey, bool toggle, FlyoutElementViewModel flyout);
}