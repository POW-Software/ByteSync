using Avalonia.Data.Converters;
using System.Globalization;

namespace ByteSync.Services.Converters;

public class IntToBoolConverter : IValueConverter
{
    public static readonly IntToBoolConverter GreaterThanOne = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue)
        {
            return intValue > 1;
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
