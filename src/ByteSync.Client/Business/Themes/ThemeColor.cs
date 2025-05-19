using Avalonia.Media;
using ByteSync.Helpers;

namespace ByteSync.Business.Themes;

public class ThemeColor
{
    public ThemeColor(string hexaColor)
    {
        HexaColor = hexaColor;
        AvaloniaColor = ColorUtils.FromHex(HexaColor);
        InitializeHsv();
    }

    public ThemeColor(Color color)
    {
        HexaColor = ColorUtils.ToHex(color);
        AvaloniaColor = color;
        InitializeHsv();
    }
    
    private void InitializeHsv()
    {
        ColorUtils.ColorToHsv(AvaloniaColor, out double hue, out double saturation, out double value);
        Hue = hue;
        Saturation = saturation;
        Value = value;
    }

    public string HexaColor { get; }
    
    public Color AvaloniaColor { get; }
    
    public double Hue { get; private set; }
    
    public double Saturation { get; private set; }
    
    public double Value { get; private set; }
    
    
    
    public ThemeColor WithHue(double hue)
    {
        var newSystemColor = ColorUtils.ColorFromHsv(hue, Saturation, Value);
        return new ThemeColor(newSystemColor);
    }

    public ThemeColor WithSaturationValue(double saturation, double value)
    {
        var newSystemColor = ColorUtils.ColorFromHsv(Hue, saturation, value);
        return new ThemeColor(newSystemColor);
    }
    
    public ThemeColor AdjustSaturationValue(double saturationPercent, double valuePercent)
    {
        double newSaturation = ComputeAdjustedValue(Saturation, saturationPercent);
        double newValue = ComputeAdjustedValue(Value, valuePercent);
        
        var newSystemColor = ColorUtils.ColorFromHsv(Hue, newSaturation, newValue);
        return new ThemeColor(newSystemColor);
    }
    
    private static double ComputeAdjustedValue(double current, double percent)
    {
        if (percent < 0)
        {
            // Ex: -0.2 (reduce by 20%) keeps 80% of previous value
            return (1 + percent) * current;
        }
        else
        {
            // Ex: 0.3 (increase by 30%) of distance between current and 1
            var delta = (1 - current) * percent;
            return current + delta;
        }
    }
}