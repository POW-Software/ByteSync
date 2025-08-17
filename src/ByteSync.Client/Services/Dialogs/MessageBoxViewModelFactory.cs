using ByteSync.Interfaces;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.ViewModels.Misc;

namespace ByteSync.Services.Dialogs;

public class MessageBoxViewModelFactory : IMessageBoxViewModelFactory
{
    private readonly ILocalizationService _localizationService;

    public MessageBoxViewModelFactory(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }
    
    public MessageBoxViewModel CreateMessageBoxViewModel(string titleKey, string? messageKey, params string[]? messageArguments)
    {
        List<string>? arguments = null;
        if (messageArguments != null)
        {
            arguments = new List<string>(messageArguments);
        }

        var result = new MessageBoxViewModel(titleKey, messageKey, arguments, _localizationService);

        return result;
    }
}