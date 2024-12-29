using System.Globalization;
using Autofac;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using ByteSync.Assets.Resources;
using ByteSync.Interfaces;

namespace ByteSync.Services.Converters;

public class BoolToDeploymentModeConverter : IValueConverter
{
    private ILocalizationService _localizationService;

    public BoolToDeploymentModeConverter()
    {
        if (!Design.IsDesignMode)
        {
            _localizationService = ContainerProvider.Container.Resolve<ILocalizationService>();
        }
    }
    
    public BoolToDeploymentModeConverter(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }
    
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (Design.IsDesignMode)
        {
            return new object();
        }
        
        var isPortableApplication = value as bool?;
        
        if (isPortableApplication == null)
        {
            return "";
        }

        string result;
        switch (isPortableApplication)
        {
            case true:
                result = _localizationService[nameof(Resources.DeploymentMode_Portable)];
                break;
            case false:
                result = _localizationService[nameof(Resources.DeploymentMode_Installation)];
                break;
            default:
                throw new ApplicationException("Unhandled case");
        }

        return result;
    }

    public object? ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}