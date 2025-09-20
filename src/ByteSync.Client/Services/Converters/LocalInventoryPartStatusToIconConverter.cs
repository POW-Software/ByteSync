using System.Globalization;
using Avalonia.Data.Converters;
using ByteSync.Business.Inventories;

namespace ByteSync.Services.Converters;

public class InventoryTaskStatusToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var inventoryProcessStatus = value as InventoryTaskStatus?;
        
        if (inventoryProcessStatus == null)
        {
            return "";
        }
        
        string status;
        switch (inventoryProcessStatus)
        {
            case InventoryTaskStatus.Error:
            case InventoryTaskStatus.Cancelled:
            case InventoryTaskStatus.NotLaunched:
                status = "SolidXCircle";
                
                break;
            case InventoryTaskStatus.Success:
                status = "SolidCheckCircle";
                
                break;
            case InventoryTaskStatus.Pending:
                status = "None";
                
                break;
            case InventoryTaskStatus.Running:
                status = "None";
                
                break;
            default:
                throw new ApplicationException("Unhandled case");
        }
        
        return status;
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}