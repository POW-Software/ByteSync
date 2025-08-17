using System.Globalization;
using Autofac;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using ByteSync.Assets.Resources;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Converters;
using ByteSync.Interfaces.Services.Localizations;

namespace ByteSync.Services.Converters;

public class FormatKbSizeConverter : IValueConverter, IFormatKbSizeConverter
{
    private readonly ILocalizationService _localizationService = null!;

    public FormatKbSizeConverter()
    {
        if (!Design.IsDesignMode)
        {
            _localizationService = ContainerProvider.Container.Resolve<ILocalizationService>();
        }
    }
        
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        if (Design.IsDesignMode)
        {
            return new object();
        }
            
        var number = System.Convert.ToInt64(value);

        string format = "N1";
        bool convertUntilTeraBytes = false;
        if (parameter is string sParameter && sParameter.Equals("true//N0", StringComparison.CurrentCultureIgnoreCase))
        {
            // A parser plus tard
            parameter = new FormatKbSizeConverterParameters { ConvertUntilTeraBytes = true, Format = "N0" };
        }
        if (parameter is FormatKbSizeConverterParameters formatKbSizeConverterParameters)
        {
            if (formatKbSizeConverterParameters.Format.IsNotEmpty(true))
            {
                format = formatKbSizeConverterParameters.Format!;
            }

            convertUntilTeraBytes = formatKbSizeConverterParameters.ConvertUntilTeraBytes;
        }
    
        string result;

        if (convertUntilTeraBytes && number >= (long)1024 * 1024 * 1024 * 1024)
        {
            var preValue = (number / 1024.0 / 1024 / 1024 / 1024).ToString(format);
            result = String.Format(_localizationService[nameof(Resources.Misc_SizeUnitTemplate_TeraByte)], preValue);
        }
        else if (number >= 1024 * 1024 * 1024)
        {
            var preValue = (number / 1024.0 / 1024 / 1024).ToString(format);
            result = String.Format(_localizationService[nameof(Resources.Misc_SizeUnitTemplate_GigaByte)], preValue);
        }
        else if (number >= 1024 * 1024)
        {
            var preValue = (number / 1024.0 / 1024).ToString(format);
            result = String.Format(_localizationService[nameof(Resources.Misc_SizeUnitTemplate_MegaByte)], preValue);
        }
        else if (number >= 1024)
        {
            var preValue = (number / 1024.0).ToString(format);
            result = String.Format(_localizationService[nameof(Resources.Misc_SizeUnitTemplate_KiloByte)], preValue);
        }
        else
        {
            var preValue = number;
            result = String.Format(_localizationService[nameof(Resources.Misc_SizeUnitTemplate_Byte)], preValue);
        }

        return result;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }

    public string Convert(long? value)
    {
        return (string)Convert(value, typeof(string), null, null);
    }
}

public class FormatKbSizeConverterParameters
{
    public string? Format { get; set; }
        
    public bool ConvertUntilTeraBytes { get; set; }
}