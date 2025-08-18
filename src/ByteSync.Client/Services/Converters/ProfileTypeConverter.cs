using System.Globalization;
using Autofac;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using ByteSync.Assets.Resources;
using ByteSync.Business.Profiles;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Services.Localizations;

namespace ByteSync.Services.Converters;

public class ProfileTypeConverter : IValueConverter
{
    private ILocalizationService _localizationService;
    
    public ProfileTypeConverter()
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
        
        var profileType = value as ProfileTypes?;
        
        if (profileType == null)
        {
            return "";
        }

        string status;
        switch (profileType)
        {
            case ProfileTypes.Cloud:
                status = _localizationService[nameof(Resources.ProfileType_CloudSession)];
                break;
            case ProfileTypes.Local:
                status = _localizationService[nameof(Resources.ProfileType_LocalSession)];
                break;
            default:
                throw new ApplicationException("Unhandled case");
        }

        return status;
    }

    public object? ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (Design.IsDesignMode)
        {
            return new object();
        }
        
        if (value == null)
        {
            return null;
        }

        if (value.Equals(_localizationService[nameof(Resources.ProfileType_CloudSession)]))
        {
            return ProfileTypes.Cloud;
        }
        if (value.Equals(_localizationService[nameof(Resources.ProfileType_LocalSession)]))
        {
            return ProfileTypes.Local;
        }

        return null;
    }
}