using ByteSync.Business.Themes;

namespace ByteSync.Interfaces.Controls.Themes;

public interface IThemeBuilder
{
    Theme BuildTheme(string themeName, string primaryColorHex, ThemeModes themeMode);
    
    Theme BuildTheme(string themeName, string primaryColorHex, ThemeModes themeMode, double secondaryColorHueOffset);
    
    Theme BuildDefaultTheme();
}