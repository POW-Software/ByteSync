using ByteSync.ViewModels.Misc;

namespace ByteSync.Interfaces.Dialogs;

public interface IMessageBoxViewModelFactory
{
    MessageBoxViewModel CreateMessageBoxViewModel(string titleKey, string? messageKey, params string[]? messageArguments);
}