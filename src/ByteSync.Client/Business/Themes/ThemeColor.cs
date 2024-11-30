using ByteSync.Common.Helpers;

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
    
    // ReSharper disable once InconsistentNaming
    private void InitializeHSV()
    {
        double hue, saturation, value;
        ColorUtils.ColorToHSV(SystemColor, out hue, out saturation, out value);

        Hue = hue;
        Saturation = saturation;
        Value = value;
    }

    public string HexaColor { get; }
    
    public Avalonia.Media.Color AvaloniaColor { get; }
    
    public System.Drawing.Color SystemColor { get; }
    
    public double Hue { get; set; }

    public double Saturation { get; set; }

    public double Value { get; set; }

    private Avalonia.Media.Color FromSystemColor(System.Drawing.Color color)
    {
        return Avalonia.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
    }
    
    // public ThemeColor GetOpposite()
    // {
    //     var oppositeHue = (this.Hue + 60) % 360;
    //     
    //     var newSystemColor = ColorUtils.ColorFromHSV(oppositeHue, this.Saturation, this.Saturation);
    //     
    //     ThemeColor result = new ThemeColor(newSystemColor);
    //
    //     return result;
    // }
    
    public ThemeColor SetHue(double hue)
    {
        var newSystemColor = ColorUtils.ColorFromHSV(hue, this.Saturation, this.Value);

        ThemeColor result = new ThemeColor(newSystemColor);

        return result;
    }

    public ThemeColor SetSaturationValue(double saturation, double value)
    {
        var newSystemColor = ColorUtils.ColorFromHSV(this.Hue, saturation, value);

        ThemeColor result = new ThemeColor(newSystemColor);

        return result;
    }
    
    public ThemeColor PercentIncreaseSaturationValue(double saturationPercent, double valuePercent)
    {
        static double ComputeNew(double current, double percent)
        {
            double newSaturation1;
            if (percent < 0)
            {
                // exemple : -0.2, soit -20%, on garde 80% de la valeur précédente
                newSaturation1 = (1 + percent) * current;
            }
            else
            {
                // exemple : 0.3, soit + 30% entre valeur actuelle et 1
                // si valeur actuelle à 0.6, et augmentation de 50 %
                // 1 - 0.6 => 0.4. 50 % de 0.4 => 0.2. Nouvelle valeur 0.6 + 0.2 => 0.8
                var delta = (1 - current) * percent;
                newSaturation1 = current + delta;
            }

            return newSaturation1;
        }

        double newSaturation = ComputeNew(this.Saturation, saturationPercent);
        double newValue = ComputeNew(this.Value, valuePercent);
        
        var newSystemColor = ColorUtils.ColorFromHSV(this.Hue, newSaturation, newValue);

        ThemeColor result = new ThemeColor(newSystemColor);

        return result;
    }
}