using System.Globalization;
using Autofac;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Services.Localizations;

namespace ByteSync.Services.Converters;

public class ExecutionModeConverter : IValueConverter
{
    private ILocalizationService? _localizationService;
    
    public ExecutionModeConverter()
    {
        if (!Design.IsDesignMode)
        {
            _localizationService = ContainerProvider.Container.Resolve<ILocalizationService>();
        }
    }
    
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (Design.IsDesignMode)
        {
            return new object();
        }
        
        /*_localizationService ??= Locator.Current.GetService<ILocalizationService>()!;
        
        var executionMode = value as ExecutionModes?;
        
        if (executionMode == null)
        {
            return "";
        }

        string status;
        switch (executionMode)
        {
            case ExecutionModes.LoadOnly:
                status = _localizationService[nameof(Resources.ExecutionMode_LoadOnly)];
                break;
            case ExecutionModes.RunInventory:
                status = _localizationService[nameof(Resources.ExecutionMode_RunInventory)];
                break;
            case ExecutionModes.RunSynchronization:
                status = _localizationService[nameof(Resources.ExecutionMode_RunSynchronization)];
                break;
            default:
                throw new ApplicationException("Unhandled case");
        }

        return status;*/

        return "";
    }

    public object? ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}