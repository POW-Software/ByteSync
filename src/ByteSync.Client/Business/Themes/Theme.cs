using Avalonia.Styling;

namespace ByteSync.Business.Themes;

public class Theme
{
    public Theme(string themeName, ThemeModes mode, ThemeColor themeColor, ThemeColor secondaryThemeColor)
    {
        Name = themeName;
        Mode = mode;
        ThemeColor = themeColor;
        SecondaryThemeColor = secondaryThemeColor;
    }

    public ThemeColor SecondaryThemeColor { get; set; }
    
    public ThemeColor ThemeColor { get; set; }
    
    public string Name { get; }
    
    public ThemeModes Mode { get; }

    public string Key => $"{Mode}.{Name}";

    public bool IsDarkMode => Mode == ThemeModes.Dark;

    public ColorScheme ColorScheme { get; set; }
}