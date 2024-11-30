using System.Globalization;
using Avalonia.Data.Converters;
using ByteSync.Business.Inventories;

namespace ByteSync.Services.Converters;

public class LocalInventoryPartStatusToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var inventoryProcessStatus = value as LocalInventoryPartStatus?;
        
        if (inventoryProcessStatus == null)
        {
            return "";
        }

        string status;
        switch (inventoryProcessStatus)
        {
            case LocalInventoryPartStatus.Error:
            case LocalInventoryPartStatus.Cancelled:
            case LocalInventoryPartStatus.NotLaunched:
                status = "SolidXCircle";
                break;
            case LocalInventoryPartStatus.Success:
                status = "SolidCheckCircle";
                break;
            case LocalInventoryPartStatus.Pending:
                status = "None";
                break;
            case LocalInventoryPartStatus.Running:
                status = "None";
                break;
            default:
                throw new ApplicationException("Unhandled case");
        }

        return status;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}