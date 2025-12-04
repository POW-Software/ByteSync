using Avalonia.Media;
using ByteSync.Business.Themes;
using ByteSync.Helpers;
using ByteSync.Interfaces.Controls.Themes;

namespace ByteSync.Services.Themes;

public class ThemeBuilder : IThemeBuilder
{
    private static readonly ColorSchemeConfigBuilder _colorSchemeConfigBuilder = new();
    
    private readonly ColorSchemeConfig DarkConfig = _colorSchemeConfigBuilder.CreateDarkConfig();
    
    private readonly ColorSchemeConfig LightConfig = _colorSchemeConfigBuilder.CreateLightConfig();
    
    private const double EPSILON = 0.0001;
    
    public Theme BuildTheme(string themeName, string primaryColorHex, ThemeModes themeMode)
    {
        return BuildTheme(themeName, primaryColorHex, themeMode, -60);
    }
    
    public Theme BuildTheme(string themeName, string primaryColorHex, ThemeModes themeMode, double secondaryColorHueOffset)
    {
        var themeColor = new ThemeColor(primaryColorHex);
        var secondaryColorHue = themeColor.Hue + secondaryColorHueOffset;
        var secondarySystemColor = ColorUtils.ColorFromHsv(secondaryColorHue, themeColor.Saturation, themeColor.Value);
        var secondaryThemeColor = new ThemeColor(secondarySystemColor);
        
        var theme = new Theme(themeName, themeMode, themeColor, secondaryThemeColor);
        theme.ColorScheme = BuildColorScheme(themeColor, secondaryColorHue, themeMode);
        
        return theme;
    }
    
    public Theme BuildDefaultTheme()
    {
        return BuildTheme(ThemeConstants.BLUE + "1", ThemeConstants.BLUE_HEX, ThemeModes.Light);
    }
    
    private ColorScheme BuildColorScheme(ThemeColor themeColor, double secondaryColorHue, ThemeModes themeMode)
    {
        var colorScheme = new ColorScheme(themeMode);
        var config = themeMode == ThemeModes.Dark ? DarkConfig : LightConfig;
        
        ApplyColorSchemeConfig(colorScheme, themeColor, secondaryColorHue, config);
        ComputeSecondaryColors(colorScheme, secondaryColorHue, themeMode);
        
        colorScheme.StatusMainBackGroundBrush = new SolidColorBrush(colorScheme.StatusMainBackGround.AvaloniaColor);
        colorScheme.StatusSecondaryBackGroundBrush = new SolidColorBrush(colorScheme.StatusSecondaryBackGround.AvaloniaColor);
        colorScheme.VeryLightGrayBrush = new SolidColorBrush(colorScheme.VeryLightGray);
        
        colorScheme.TextControlSelectionHighlightColor = colorScheme.BsAccentButtonBackGround;
        
        return colorScheme;
    }
    
    private void ApplyColorSchemeConfig(ColorScheme colorScheme, ThemeColor themeColor, double secondaryColorHue, ColorSchemeConfig config)
    {
        colorScheme.MainAccentColor = GetMainAccentColor(themeColor, config);
        colorScheme.MainSecondaryColor = colorScheme.MainAccentColor.WithHue(secondaryColorHue);
        
        colorScheme.AccentTextForeGround = GetAccentTextForeground(themeColor, config);
        
        colorScheme.HomeCloudSynchronizationBackGround =
            GetColorFromSaturationValue(themeColor, config.HomeCloudSyncSaturation, config.HomeCloudSyncValue);
        colorScheme.HomeCloudSynchronizationPointerOverBackGround = GetColorFromSaturationValue(themeColor,
            config.HomeCloudSyncPointerOverSaturation, config.HomeCloudSyncPointerOverValue);
        colorScheme.HomeLocalSynchronizationBackGround = GetColorFromSaturationValue(colorScheme.MainSecondaryColor,
            config.HomeCloudSyncSaturation, config.HomeCloudSyncValue);
        colorScheme.HomeLocalSynchronizationPointerOverBackGround = GetColorFromSaturationValue(colorScheme.MainSecondaryColor,
            config.HomeCloudSyncPointerOverSaturation, config.HomeCloudSyncPointerOverValue);
        
        colorScheme.ChartsMainBarColor = GetColorFromSaturationValue(themeColor, config.ChartsBarSaturation, config.ChartsBarValue);
        colorScheme.ChartsAlternateBarColor =
            GetColorFromSaturationValue(colorScheme.MainSecondaryColor, config.ChartsBarSaturation, config.ChartsBarValue);
        colorScheme.ChartsMainLineColor = colorScheme.MainAccentColor.AvaloniaColor;
        
        colorScheme.CurrentMemberBackGround = themeColor.WithSaturationValue(config.CurrentMemberSaturation, config.CurrentMemberValue);
        colorScheme.DisabledMemberBackGround = new ThemeColor(config.DisabledMemberBackGround);
        colorScheme.OtherMemberBackGround = new ThemeColor(config.DisabledMemberBackGround);
        
        colorScheme.ConnectedMemberLetterBackGround =
            themeColor.WithSaturationValue(config.ConnectedMemberLetterSaturation, config.ConnectedMemberLetterValue);
        colorScheme.DisabledMemberLetterBackGround = new ThemeColor(config.DisabledMemberLetterBackGround);
        
        colorScheme.ConnectedMemberLetterBorder =
            themeColor.WithSaturationValue(config.ConnectedMemberLetterBorderSaturation, config.ConnectedMemberLetterBorderValue);
        colorScheme.DisabledMemberLetterBorder = new ThemeColor(config.DisabledMemberLetterBorder);
        
        colorScheme.BsAccentButtonBackGround =
            GetColorFromSaturationValue(themeColor, config.BsAccentButtonSaturation, config.BsAccentButtonValue);
        colorScheme.BsAccentButtonPointerOverBackGround = GetColorFromSaturationValue(themeColor,
            config.BsAccentButtonPointerOverSaturation, config.BsAccentButtonPointerOverValue);
        
        colorScheme.SecondaryButtonBackGround = GetColorFromSaturationValue(colorScheme.MainSecondaryColor, config.BsAccentButtonSaturation,
            config.BsAccentButtonValue);
        colorScheme.SecondaryButtonPointerOverBackGround = GetColorFromSaturationValue(colorScheme.MainSecondaryColor,
            config.BsAccentButtonPointerOverSaturation, config.BsAccentButtonPointerOverValue);
        
        colorScheme.StatusMainBackGround =
            themeColor.WithSaturationValue(config.StatusMainBackGroundSaturation, config.StatusMainBackGroundValue);
        
        ComputeAttenuations(colorScheme);
        AssignAccentColors(colorScheme, config.UseDarkAccents);
        
        ApplyStaticColors(colorScheme, config);
    }
    
    private static ThemeColor GetMainAccentColor(ThemeColor themeColor, ColorSchemeConfig config)
    {
        return Math.Abs(config.MainAccentSaturation - 1.0) < EPSILON && Math.Abs(config.MainAccentValue - 1.0) < EPSILON
            ? themeColor
            : themeColor.WithSaturationValue(config.MainAccentSaturation, config.MainAccentValue);
    }
    
    private static Color GetAccentTextForeground(ThemeColor themeColor, ColorSchemeConfig config)
    {
        return Math.Abs(config.AccentTextSaturation - 1.0) < EPSILON && Math.Abs(config.AccentTextValue - 1.0) < EPSILON
            ? themeColor.AvaloniaColor
            : themeColor.WithSaturationValue(config.AccentTextSaturation, config.AccentTextValue).AvaloniaColor;
    }
    
    private static Color GetColorFromSaturationValue(ThemeColor themeColor, double saturation, double value)
    {
        return themeColor.WithSaturationValue(saturation, value).AvaloniaColor;
    }
    
    private static void ApplyStaticColors(ColorScheme colorScheme, ColorSchemeConfig config)
    {
        colorScheme.VeryLightGray = config.VeryLightGray;
        colorScheme.GenericButtonBorder = config.GenericButtonBorder;
        colorScheme.Gray1 = config.Gray1;
        colorScheme.Gray2 = config.Gray2;
        colorScheme.Gray5 = config.Gray5;
        colorScheme.Gray7 = config.Gray7;
        colorScheme.Gray8 = config.Gray8;
        colorScheme.SettingsHeaderColor = config.SettingsHeaderColor;
        colorScheme.BlockBackColor = config.BlockBackColor;
        colorScheme.MainWindowTopColor = config.MainWindowTopColor;
        colorScheme.MainWindowBottomColor = config.MainWindowBottomColor;
    }
    
    private void ComputeSecondaryColors(ColorScheme colorScheme, double secondaryColorHue, ThemeModes themeMode)
    {
        for (var i = 1; i <= 3; i++)
        {
            var basePercent = -0.10;
            var secondary = colorScheme.MainSecondaryColor.AdjustSaturationValue(0, basePercent * i);
            colorScheme.SecondaryColors.Add(secondary);
        }
        
        colorScheme.CurrentMemberSecondaryBackGround = colorScheme.CurrentMemberBackGround
            .WithHue(secondaryColorHue);
        
        colorScheme.OtherMemberSecondaryBackGround = colorScheme.OtherMemberBackGround
            .WithHue(secondaryColorHue);
        
        if (themeMode == ThemeModes.Dark)
        {
            colorScheme.StatusSecondaryBackGround = colorScheme.StatusMainBackGround
                .WithHue(secondaryColorHue)
                .AdjustSaturationValue(0, +0.20);
        }
        else
        {
            colorScheme.StatusSecondaryBackGround = colorScheme.StatusMainBackGround
                .WithHue(secondaryColorHue);
        }
    }
    
    private static void ComputeAttenuations(ColorScheme colorScheme)
    {
        const double step = 0.10;
        var darkColors = new ThemeColor[5];
        var lightColors = new ThemeColor[5];
        
        for (var i = 0; i < 5; i++)
        {
            var offset = step * (i + 1);
            darkColors[i] = colorScheme.MainAccentColor.AdjustSaturationValue(0, -offset);
            lightColors[i] = colorScheme.MainAccentColor.AdjustSaturationValue(0, offset);
        }
        
        colorScheme.SystemAccentColorDark1 = darkColors[0];
        colorScheme.SystemAccentColorDark2 = darkColors[1];
        colorScheme.SystemAccentColorDark3 = darkColors[2];
        colorScheme.SystemAccentColorDark4 = darkColors[3];
        colorScheme.SystemAccentColorDark5 = darkColors[4];
        
        colorScheme.SystemAccentColorLight1 = lightColors[0];
        colorScheme.SystemAccentColorLight2 = lightColors[1];
        colorScheme.SystemAccentColorLight3 = lightColors[2];
        colorScheme.SystemAccentColorLight4 = lightColors[3];
        colorScheme.SystemAccentColorLight5 = lightColors[4];
    }
    
    private static void AssignAccentColors(ColorScheme colorScheme, bool useDarkAccents)
    {
        var sources = new[]
        {
            useDarkAccents ? colorScheme.SystemAccentColorDark1 : colorScheme.SystemAccentColorLight1,
            useDarkAccents ? colorScheme.SystemAccentColorDark2 : colorScheme.SystemAccentColorLight2,
            useDarkAccents ? colorScheme.SystemAccentColorDark3 : colorScheme.SystemAccentColorLight3,
            useDarkAccents ? colorScheme.SystemAccentColorDark4 : colorScheme.SystemAccentColorLight4,
            useDarkAccents ? colorScheme.SystemAccentColorDark5 : colorScheme.SystemAccentColorLight5
        };
        
        colorScheme.Accent1 = sources[0].AvaloniaColor;
        colorScheme.Accent2 = sources[1].AvaloniaColor;
        colorScheme.Accent3 = sources[2].AvaloniaColor;
        colorScheme.Accent4 = sources[3].AvaloniaColor;
        colorScheme.Accent5 = sources[4].AvaloniaColor;
    }
}