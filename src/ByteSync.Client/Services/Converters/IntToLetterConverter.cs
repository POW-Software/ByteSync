using System.Globalization;
using Avalonia.Data.Converters;

namespace ByteSync.Services.Converters;

public class IntToLetterConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        int iValue = System.Convert.ToInt32(value);
        
        return ((char) ('A' + iValue)).ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string strValue && strValue.Length == 1)
        {
            char letter = strValue[0];
            if (char.IsLetter(letter))
            {
                return char.ToUpper(letter) - 'A';
            }
        }

        throw new ArgumentOutOfRangeException();
    }
}