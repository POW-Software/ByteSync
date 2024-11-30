using System.Globalization;
using Avalonia.Data.Converters;

namespace ByteSync.Services.Converters;

public class CaseConverter : IValueConverter
{
    public CaseConverter(bool? upperCase)
    {
        UpperCase = upperCase;
    }
    
    public bool? UpperCase { get; }
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var input = System.Convert.ToString(value);

        if (input == null)
        {
            return null;
        }

        string result = input;
        if (UpperCase == true)
        {
            result = input.ToUpper();
        }

        return result;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}