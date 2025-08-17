using Avalonia.Controls;
using Avalonia.Data.Converters;
using Autofac;
using ByteSync.Interfaces;
using System.Globalization;

namespace ByteSync.Services.Converters;

public class ItemCountToLocalizedTextConverter : IValueConverter
{
    private readonly ILocalizationService _localizationService;

    // Constructor for DI (tests)
    public ItemCountToLocalizedTextConverter(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    // Parameterless constructor for XAML/Container resolution
    public ItemCountToLocalizedTextConverter()
    {
        if (!Design.IsDesignMode)
        {
            _localizationService = ContainerProvider.Container.Resolve<ILocalizationService>();
        }
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (Design.IsDesignMode || _localizationService == null)
        {
            return "item:";
        }
        
        if (value is not int count)
        {
            // Fallback to singular form for invalid input
            return _localizationService["ValidationFailure_ItemSingular"];
        }

        var key = count <= 1 ? "ValidationFailure_ItemSingular" : "ValidationFailure_ItemPlural";
        return _localizationService[key];
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
