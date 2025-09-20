using System.Globalization;
using Autofac;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using ByteSync.Business.Inventories;
using ByteSync.Interfaces.Services.Localizations;

namespace ByteSync.Services.Converters;

public class InventoryTaskStatusToTextConverter : IValueConverter
{
    private ILocalizationService _localizationService;
    
    public InventoryTaskStatusToTextConverter()
    {
        if (!Design.IsDesignMode)
        {
            _localizationService = ContainerProvider.Container.Resolve<ILocalizationService>();
        }
    }
    
    public InventoryTaskStatusToTextConverter(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }
    
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var inventoryProcessStatus = value as InventoryTaskStatus?;
        var startKey = parameter as string;
        
        if (inventoryProcessStatus == null || startKey.IsNullOrEmpty())
        {
            return "";
        }
        
        var key = startKey + inventoryProcessStatus;
        
        var result = _localizationService[key];
        
        return result;
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}