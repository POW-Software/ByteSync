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
    private ILocalizationService _localizationService;

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

        if (value is not DeploymentMode mode)
        {
            return string.Empty;
        }

        return mode switch
        {
            DeploymentMode.Portable => _localizationService[nameof(Resources.DeploymentMode_Portable)],
            DeploymentMode.SetupInstallation => _localizationService[nameof(Resources.DeploymentMode_Installation)],
            DeploymentMode.MsixInstallation => _localizationService["DeploymentMode_MSIX"],
            _ => string.Empty
        };
    }

    public object? ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}