using System.Globalization;
using Autofac;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using ByteSync.Business.Inventories;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;

namespace ByteSync.Services.Converters;

public class LocalInventoryPartStatusToTextConverter : IValueConverter
{
    private ILocalizationService _localizationService;

    public LocalInventoryPartStatusToTextConverter()
    {
        if (!Design.IsDesignMode)
        {
            _localizationService = ContainerProvider.Container.Resolve<ILocalizationService>();
        }
    }
    
    public LocalInventoryPartStatusToTextConverter(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }
    
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var inventoryProcessStatus = value as LocalInventoryPartStatus?;
        var startKey = parameter as string;
        
        if (inventoryProcessStatus == null || startKey.IsNullOrEmpty())
        {
            return "";
        }

        string key = startKey + inventoryProcessStatus;
        
        var result = _localizationService[key];
        // switch (inventoryProcessStatus)
        // {
        //     case InventoryProcessStatuses.Error:
        //         key = startKey + "Error";
        //         
        //     case InventoryProcessStatuses.Cancelled:
        //     case InventoryProcessStatuses.NotLaunched:
        //         key = "SolidXCircle";
        //         break;
        //     case InventoryProcessStatuses.Success:
        //         key = "SolidCheckCircle";
        //         break;
        //     case InventoryProcessStatuses.Pending:
        //         key = "None";
        //         break;
        //     case InventoryProcessStatuses.Running:
        //         key = "None";
        //         break;
        //     default:
        //         throw new ApplicationException("Unhandled case");
        // }

        return result;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}