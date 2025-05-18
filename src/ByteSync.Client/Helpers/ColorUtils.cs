using Avalonia.Media;

namespace ByteSync.Helpers;

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
            color = Color.FromArgb(255, bytes[0], bytes[1], bytes[2]);
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
        return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
    }

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
    
    public static Color BlendWithTransparency(Color baseColor, Color overlayColor, double opacity)
    {
        opacity = Math.Clamp(opacity, 0, 1);
    
        // Normalize RGB values between 0 and 1
        double baseR = baseColor.R / 255.0;
        double baseG = baseColor.G / 255.0;
        double baseB = baseColor.B / 255.0;
    
        double overlayR = overlayColor.R / 255.0;
        double overlayG = overlayColor.G / 255.0;
        double overlayB = overlayColor.B / 255.0;
    
        // Apply the blending formula with transparency
        // For each component: result = base + (overlay - base) * opacity
        double resultR = baseR + (overlayR - baseR) * opacity;
        double resultG = baseG + (overlayG - baseG) * opacity;
        double resultB = baseB + (overlayB - baseB) * opacity;
    
        // Convert to values 0-255
        byte r = (byte)(resultR * 255);
        byte g = (byte)(resultG * 255);
        byte b = (byte)(resultB * 255);
    
        return Color.FromArgb(baseColor.A, r, g, b);
    }
    
    public static void ColorToHsv(Color color, out double hue, out double saturation, out double value)
    {
        int max = Math.Max(color.R, Math.Max(color.G, color.B));
        int min = Math.Min(color.R, Math.Min(color.G, color.B));

        hue = color.ToSystemColor().GetHue();
        saturation = (max == 0) ? 0 : 1d - (1d * min / max);
        value = max / 255d;
    }

    public static System.Drawing.Color ToSystemColor(this Color color)
    {
        return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
    }

    public static Color ColorFromHsv(double hue, double saturation, double value)
    {
        int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
        double f = hue / 60 - Math.Floor(hue / 60);

        value = value * 255;
        int v = Convert.ToInt32(value);
        int p = Convert.ToInt32(value * (1 - saturation));
        int q = Convert.ToInt32(value * (1 - f * saturation));
        int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

        if (hi == 0)
            return Color.FromArgb(255, (byte) v, (byte) t, (byte) p);
        else if (hi == 1)
            return Color.FromArgb(255, (byte) q, (byte) v, (byte) p);
        else if (hi == 2)
            return Color.FromArgb(255, (byte) p, (byte) v, (byte) t);
        else if (hi == 3)
            return Color.FromArgb(255, (byte) p, (byte) q, (byte) v);
        else if (hi == 4)
            return Color.FromArgb(255, (byte) t, (byte) p, (byte) v);
        else
            return Color.FromArgb(255, (byte) v, (byte) p, (byte) q);
    }
}