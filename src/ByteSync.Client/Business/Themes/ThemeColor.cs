namespace ByteSync.Business.Themes;

public class ThemeColor
{
    public ThemeColor(string hexaColor)
    {
        HexaColor = hexaColor;
        SystemColor = ColorUtils.FromHex(HexaColor);
        AvaloniaColor = FromSystemColor(SystemColor);
        InitializeHSV();
    }

    public ThemeColor(System.Drawing.Color systemColor)
    {
        SystemColor = systemColor;
        HexaColor = ColorUtils.ToHex(systemColor);
        AvaloniaColor = FromSystemColor(SystemColor);
        InitializeHSV();
    }
    
    private void InitializeHSV()
    {
        ColorUtils.ColorToHSV(SystemColor, out double hue, out double saturation, out double value);
        Hue = hue;
        Saturation = saturation;
        Value = value;
    }

    public string HexaColor { get; }
    
    public Avalonia.Media.Color AvaloniaColor { get; }
    
    public System.Drawing.Color SystemColor { get; }
    
    public double Hue { get; private set; }
    
    public double Saturation { get; private set; }
    
    public double Value { get; private set; }
    

    private Avalonia.Media.Color FromSystemColor(System.Drawing.Color color)
    {
        return Avalonia.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
    }
    
    public ThemeColor WithHue(double hue)
    {
        var newSystemColor = ColorUtils.ColorFromHSV(hue, this.Saturation, this.Value);
        return new ThemeColor(newSystemColor);
    }

    public ThemeColor WithSaturationValue(double saturation, double value)
    {
        var newSystemColor = ColorUtils.ColorFromHSV(this.Hue, saturation, value);
        return new ThemeColor(newSystemColor);
    }
    
    public ThemeColor AdjustSaturationValue(double saturationPercent, double valuePercent)
    {
        double newSaturation = ComputeAdjustedValue(this.Saturation, saturationPercent);
        double newValue = ComputeAdjustedValue(this.Value, valuePercent);
        
        var newSystemColor = ColorUtils.ColorFromHSV(this.Hue, newSaturation, newValue);
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