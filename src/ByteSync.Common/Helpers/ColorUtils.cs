using System;
using System.Drawing;

namespace ByteSync.Common.Helpers;

public static class ColorUtils
{
    // https://stackoverflow.com/questions/321370/how-can-i-convert-a-hex-string-to-a-byte-array
    public static Color FromHex(string hexColor)
    {
        hexColor = hexColor.Trim('#');
        
        if (hexColor.Length % 2 == 1)
        {
            throw new Exception("The binary key cannot have an odd number of digits");
        }

        byte[] bytes = new byte[hexColor.Length >> 1];

        for (int i = 0; i < hexColor.Length >> 1; ++i)
        {
            bytes[i] = (byte)((GetHexVal(hexColor[i << 1]) << 4) + (GetHexVal(hexColor[(i << 1) + 1])));
        }

        Color color;
        if (bytes.Length == 3)
        {
            color = Color.FromArgb(bytes[0], bytes[1], bytes[2]);
        }
        else if (bytes.Length == 4)
        {
            color = Color.FromArgb(bytes[0], bytes[1], bytes[2], bytes[3]);
        }
        else
        {
            throw new Exception("The binary key does not represent a valid color");
        }
        
        return color;
    }

    public static string ToHex(Color color)
    {
        return "#" + color.A.ToString("X2") + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
    }

    public static int GetHexVal(char hex) 
    {
        int val = (int)hex;
        //For uppercase A-F letters:
        //return val - (val < 58 ? 48 : 55);
        //For lowercase a-f letters:
        //return val - (val < 58 ? 48 : 87);
        //Or the two combined, but a bit slower:
        return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
    }


    // http://csharphelper.com/blog/2016/08/convert-between-rgb-and-hls-color-models-in-c/

    public static void RgbToHls(Color color, out double h, out double l, out double s)
    {
        RgbToHls(color.R, color.G, color.B, out h, out l, out s);
    }

    public static void RgbToHls(int r, int g, int b, out double h, out double l, out double s)
    {
        // Convert RGB to a 0.0 to 1.0 range.
        double double_r = r / 255.0;
        double double_g = g / 255.0;
        double double_b = b / 255.0;

        // Get the maximum and minimum RGB components.
        double max = double_r;
        if (max < double_g) max = double_g;
        if (max < double_b) max = double_b;

        double min = double_r;
        if (min > double_g) min = double_g;
        if (min > double_b) min = double_b;

        double diff = max - min;
        l = (max + min) / 2;
        if (Math.Abs(diff) < 0.00001)
        {
            s = 0;
            h = 0;  // H is really undefined.
        }
        else
        {
            if (l <= 0.5) s = diff / (max + min);
            else s = diff / (2 - max - min);

            double r_dist = (max - double_r) / diff;
            double g_dist = (max - double_g) / diff;
            double b_dist = (max - double_b) / diff;

            if (double_r == max) h = b_dist - g_dist;
            else if (double_g == max) h = 2 + r_dist - b_dist;
            else h = 4 + g_dist - r_dist;

            h = h * 60;
            if (h < 0) h += 360;
        }
    }

    public static Color HlsToRgb(double h, double l, double s)
    {
        byte r;
        byte g;
        byte b;

        HlsToRgb(h, l, s, out r, out g, out b);

        Color color = Color.FromArgb(r, g, b);

        return color;
    }

    // Convert an HLS value into an RGB value.
    public static void HlsToRgb(double h, double l, double s, out byte r, out byte g, out byte b)
    {
        double p2;
        if (l <= 0.5) p2 = l * (1 + s);
        else p2 = l + s - l * s;

        double p1 = 2 * l - p2;
        double double_r, double_g, double_b;
        if (s == 0)
        {
            double_r = l;
            double_g = l;
            double_b = l;
        }
        else
        {
            double_r = QqhToRgb(p1, p2, h + 120);
            double_g = QqhToRgb(p1, p2, h);
            double_b = QqhToRgb(p1, p2, h - 120);
        }

        // Convert RGB to the 0 to 255 range.
        r = (byte)(double_r * 255.0);
        g = (byte)(double_g * 255.0);
        b = (byte)(double_b * 255.0);
    }
    
    private static double QqhToRgb(double q1, double q2, double hue)
    {
        if (hue > 360) hue -= 360;
        else if (hue < 0) hue += 360;

        if (hue < 60) return q1 + (q2 - q1) * hue / 60;
        if (hue < 180) return q2;
        if (hue < 240) return q1 + (q2 - q1) * (240 - hue) / 60;
        return q1;
    }
    
    // https://stackoverflow.com/questions/3722307/is-there-an-easy-way-to-blend-two-system-drawing-color-values
    /// <summary>Blends the specified colors together.</summary>
    /// <param name="color">Color to blend onto the background color.</param>
    /// <param name="backColor">Color to blend the other color onto.</param>
    /// <param name="amount">How much of <paramref name="color"/> to keep,
    /// “on top of” <paramref name="backColor"/>.</param>
    /// <returns>The blended colors.</returns>
    public static Color Blend(Color color, Color backColor, double amount)
    {
        byte r = (byte) (color.R * amount + backColor.R * (1 - amount));
        byte g = (byte) (color.G * amount + backColor.G * (1 - amount));
        byte b = (byte) (color.B * amount + backColor.B * (1 - amount));
        return Color.FromArgb(r, g, b);
    }
    
    /// <summary>
    /// Creates color with corrected brightness.
    /// </summary>
    /// <param name="color">Color to correct.</param>
    /// <param name="correctionFactor">The brightness correction factor. Must be between -1 and 1. 
    /// Negative values produce darker colors.</param>
    /// <returns>
    /// Corrected <see cref="Color"/> structure.
    /// </returns>
    public static Color ChangeColorBrightness(Color color, float correctionFactor)
    {
        https://stackoverflow.com/questions/801406/c-create-a-lighter-darker-color-based-on-a-system-color
        
        float red = (float)color.R;
        float green = (float)color.G;
        float blue = (float)color.B;

        if (correctionFactor < 0)
        {
            correctionFactor = 1 + correctionFactor;
            red *= correctionFactor;
            green *= correctionFactor;
            blue *= correctionFactor;
        }
        else
        {
            red = (255 - red) * correctionFactor + red;
            green = (255 - green) * correctionFactor + green;
            blue = (255 - blue) * correctionFactor + blue;
        }

        return Color.FromArgb(color.A, (int)red, (int)green, (int)blue);
    }
    
    // Testé le 13/12/2022 : https://stackoverflow.com/questions/359612/how-to-convert-rgb-color-to-hsv
    // Même résultat qu'avec Gimp
    
    public static void ColorToHSV(Color color, out double hue, out double saturation, out double value)
    {
        int max = Math.Max(color.R, Math.Max(color.G, color.B));
        int min = Math.Min(color.R, Math.Min(color.G, color.B));

        hue = color.GetHue();
        saturation = (max == 0) ? 0 : 1d - (1d * min / max);
        value = max / 255d;
    }

    public static Color ColorFromHSV(double hue, double saturation, double value)
    {
        int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
        double f = hue / 60 - Math.Floor(hue / 60);

        value = value * 255;
        int v = Convert.ToInt32(value);
        int p = Convert.ToInt32(value * (1 - saturation));
        int q = Convert.ToInt32(value * (1 - f * saturation));
        int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

        if (hi == 0)
            return Color.FromArgb(255, v, t, p);
        else if (hi == 1)
            return Color.FromArgb(255, q, v, p);
        else if (hi == 2)
            return Color.FromArgb(255, p, v, t);
        else if (hi == 3)
            return Color.FromArgb(255, p, q, v);
        else if (hi == 4)
            return Color.FromArgb(255, t, p, v);
        else
            return Color.FromArgb(255, v, p, q);
    }
}