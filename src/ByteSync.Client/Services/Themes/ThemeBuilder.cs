using Avalonia.Media;
using ByteSync.Business.Themes;
using ByteSync.Helpers;
using ByteSync.Interfaces.Controls.Themes;

namespace ByteSync.Services.Themes;

public class ThemeBuilder : IThemeBuilder
{
    private const double Epsilon = 0.0001;
    
    private record ColorSchemeConfig(
        double MainAccentSaturation,
        double MainAccentValue,
        double AccentTextSaturation,
        double AccentTextValue,
        double HomeCloudSyncSaturation,
        double HomeCloudSyncValue,
        double HomeCloudSyncPointerOverSaturation,
        double HomeCloudSyncPointerOverValue,
        double ChartsBarSaturation,
        double ChartsBarValue,
        double CurrentMemberSaturation,
        double CurrentMemberValue,
        double ConnectedMemberLetterSaturation,
        double ConnectedMemberLetterValue,
        double ConnectedMemberLetterBorderSaturation,
        double ConnectedMemberLetterBorderValue,
        double BsAccentButtonSaturation,
        double BsAccentButtonValue,
        double BsAccentButtonPointerOverSaturation,
        double BsAccentButtonPointerOverValue,
        double StatusMainBackGroundSaturation,
        double StatusMainBackGroundValue,
        Color DisabledMemberBackGround,
        Color DisabledMemberLetterBackGround,
        Color DisabledMemberLetterBorder,
        Color VeryLightGray,
        Color GenericButtonBorder,
        Color Gray1,
        Color Gray2,
        Color Gray5,
        Color Gray7,
        Color Gray8,
        Color SettingsHeaderColor,
        Color BlockBackColor,
        Color MainWindowTopColor,
        Color MainWindowBottomColor,
        bool UseDarkAccents);
    
    private static ColorSchemeConfig CreateConfig(
        double mainAccentSaturation, double mainAccentValue,
        double accentTextSaturation, double accentTextValue,
        double homeCloudSyncSaturation, double homeCloudSyncValue,
        double homeCloudSyncPointerOverSaturation, double homeCloudSyncPointerOverValue,
        double chartsBarSaturation, double chartsBarValue,
        double currentMemberSaturation, double currentMemberValue,
        double connectedMemberLetterSaturation, double connectedMemberLetterValue,
        double connectedMemberLetterBorderSaturation, double connectedMemberLetterBorderValue,
        double bsAccentButtonSaturation, double bsAccentButtonValue,
        double bsAccentButtonPointerOverSaturation, double bsAccentButtonPointerOverValue,
        double statusMainBackGroundSaturation, double statusMainBackGroundValue,
        Color disabledMemberBackGround, Color disabledMemberLetterBackGround, Color disabledMemberLetterBorder,
        Color veryLightGray, Color genericButtonBorder,
        Color gray1, Color gray2, Color gray5, Color gray7, Color gray8,
        Color settingsHeaderColor, Color blockBackColor,
        Color mainWindowTopColor, Color mainWindowBottomColor,
        bool useDarkAccents) =>
        new ColorSchemeConfig(
            mainAccentSaturation, mainAccentValue,
            accentTextSaturation, accentTextValue,
            homeCloudSyncSaturation, homeCloudSyncValue,
            homeCloudSyncPointerOverSaturation, homeCloudSyncPointerOverValue,
            chartsBarSaturation, chartsBarValue,
            currentMemberSaturation, currentMemberValue,
            connectedMemberLetterSaturation, connectedMemberLetterValue,
            connectedMemberLetterBorderSaturation, connectedMemberLetterBorderValue,
            bsAccentButtonSaturation, bsAccentButtonValue,
            bsAccentButtonPointerOverSaturation, bsAccentButtonPointerOverValue,
            statusMainBackGroundSaturation, statusMainBackGroundValue,
            disabledMemberBackGround, disabledMemberLetterBackGround, disabledMemberLetterBorder,
            veryLightGray, genericButtonBorder,
            gray1, gray2, gray5, gray7, gray8,
            settingsHeaderColor, blockBackColor,
            mainWindowTopColor, mainWindowBottomColor,
            useDarkAccents);
    
    private static readonly ColorSchemeConfig DarkConfig = CreateConfig(
        0.65, 0.50,
        0.33, 0.85,
        0.55, 0.70,
        0.25, 0.50,
        0.50, 0.75,
        0.35, 0.22,
        0.35, 0.28,
        0.35, 0.34,
        0.55, 0.25,
        0.55, 0.35,
        0.45, 0.25,
        new Color(0xFF, 0x30, 0x30, 0x30), new Color(0xFF, 0x37, 0x37, 0x37), new Color(0xFF, 0x3D, 0x3D, 0x3D),
        new Color(0xFF, 0x12, 0x12, 0x12), new Color(0xFF, 0x55, 0x55, 0x55),
        new Color(0xFF, 0xCC, 0xCC, 0xCC), new Color(0xFF, 0x80, 0x80, 0x80), new Color(0xFF, 0x46, 0x46, 0x46),
        new Color(0xFF, 0x3A, 0x3A, 0x3A), new Color(0xFF, 0x2C, 0x2C, 0x2C),
        new Color(0xFF, 0x30, 0x30, 0x30), new Color(0xFF, 0x1F, 0x1F, 0x1F),
        new Color(0xFF, 0x12, 0x12, 0x12), new Color(0xFF, 0x04, 0x04, 0x04),
        true);
    
    private static readonly ColorSchemeConfig LightConfig = CreateConfig(
        1.0, 1.0,
        1.0, 1.0,
        0.50, 0.65,
        0.25, 0.55,
        0.50, 0.80,
        0.20, 0.92,
        0.20, 0.84,
        0.20, 0.78,
        0.15, 0.95,
        0.12, 0.98,
        0.35, 0.90,
        new Color(0xFF, 0xEC, 0xEC, 0xEC), new Color(0xFF, 0xE6, 0xE6, 0xE6), new Color(0xFF, 0xE0, 0xE0, 0xE0),
        new Color(0xFF, 0xF7, 0xF7, 0xF7), new Color(0xFF, 0xAA, 0xAA, 0xAA),
        new Color(0xFF, 0x33, 0x33, 0x33), new Color(0xFF, 0x7F, 0x7F, 0x7F), new Color(0xFF, 0xB9, 0xB9, 0xB9),
        new Color(0xFF, 0xD6, 0xD6, 0xD6), new Color(0xFF, 0xE0, 0xE0, 0xE0),
        new Color(0xFF, 0xEC, 0xEC, 0xEC), new Color(0xFF, 0xFF, 0xFF, 0xFF),
        new Color(0xFF, 0xFA, 0xFA, 0xFA), new Color(0xFF, 0xEF, 0xEF, 0xEF),
        false);
    
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
        return Math.Abs(config.MainAccentSaturation - 1.0) < Epsilon && Math.Abs(config.MainAccentValue - 1.0) < Epsilon
            ? themeColor
            : themeColor.WithSaturationValue(config.MainAccentSaturation, config.MainAccentValue);
    }
    
    private static Color GetAccentTextForeground(ThemeColor themeColor, ColorSchemeConfig config)
    {
        return Math.Abs(config.AccentTextSaturation - 1.0) < Epsilon && Math.Abs(config.AccentTextValue - 1.0) < Epsilon
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
        for (int i = 1; i <= 3; i++)
        {
            double basePercent = -0.10;
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
        
        for (int i = 0; i < 5; i++)
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