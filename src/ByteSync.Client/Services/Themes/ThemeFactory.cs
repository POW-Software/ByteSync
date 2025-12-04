using ByteSync.Business.Themes;
using ByteSync.Interfaces.Controls.Themes;

namespace ByteSync.Services.Themes;

public class ThemeFactory : IThemeFactory
{
    private readonly IThemeService _themeService;
    private readonly IThemeBuilder _themeBuilder;
    
    public ThemeFactory(IThemeService themeService, IThemeBuilder themeBuilder)
    {
        _themeService = themeService;
        _themeBuilder = themeBuilder;
    }
    
    public void BuildThemes()
    {
        BuildAndRegisterThemeVariants(ThemeConstants.BLUE, ThemeConstants.BLUE_HEX);
        BuildAndRegisterThemeVariants(ThemeConstants.GOLD, ThemeConstants.GOLD_HEX);
        BuildAndRegisterThemeVariants(ThemeConstants.GREEN, ThemeConstants.GREEN_HEX);
        BuildAndRegisterThemeVariants(ThemeConstants.RED, ThemeConstants.RED_HEX);
        BuildAndRegisterThemeVariants(ThemeConstants.PINK, ThemeConstants.PINK_HEX);
        BuildAndRegisterThemeVariants(ThemeConstants.PURPLE, ThemeConstants.PURPLE_HEX);
        
        _themeService.OnThemesRegistered();
    }
    
    private void BuildAndRegisterThemeVariants(string themeName, string primaryColorHex)
    {
        RegisterThemeVariant(themeName + "1", primaryColorHex, -60);
        RegisterThemeVariant(themeName + "2", primaryColorHex, +60);
    }
    
    private void RegisterThemeVariant(string themeName, string primaryColorHex, double secondaryColorHueOffset)
    {
        var lightTheme = _themeBuilder.BuildTheme(themeName, primaryColorHex, ThemeModes.Light, secondaryColorHueOffset);
        _themeService.RegisterTheme(lightTheme);
        
        var darkTheme = _themeBuilder.BuildTheme(themeName, primaryColorHex, ThemeModes.Dark, secondaryColorHueOffset);
        _themeService.RegisterTheme(darkTheme);
    }
}