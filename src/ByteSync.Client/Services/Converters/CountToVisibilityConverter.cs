using System.Globalization;
using Avalonia.Data.Converters;

namespace ByteSync.Services.Converters;

public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int count)
            return count > 0;
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new ApplicationException();
    }
}