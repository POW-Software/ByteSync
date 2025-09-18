using System.Globalization;
using Autofac;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using ByteSync.Assets.Resources;
using ByteSync.Common.Business.Misc;
using ByteSync.Interfaces.Services.Localizations;

namespace ByteSync.Services.Converters;

public class DeploymentModeToStringConverter : IValueConverter
{
    private ILocalizationService _localizationService = null!;
    
    public DeploymentModeToStringConverter()
    {
        if (!Design.IsDesignMode)
        {
            _localizationService = ContainerProvider.Container.Resolve<ILocalizationService>();
        }
    }
    
    public DeploymentModeToStringConverter(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }
    
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (Design.IsDesignMode)
        {
            return new object();
        }
        
        if (value is not DeploymentModes deploymentMode)
        {
            return string.Empty;
        }
        
        return deploymentMode switch
        {
            DeploymentModes.Portable => _localizationService[nameof(Resources.DeploymentMode_Portable)],
            DeploymentModes.SetupInstallation => _localizationService[nameof(Resources.DeploymentMode_Installation)],
            DeploymentModes.MsixInstallation => _localizationService["DeploymentMode_MSIX"],
            DeploymentModes.HomebrewInstallation => _localizationService["DeploymentMode_Homebrew"],
            _ => string.Empty
        };
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}