using Avalonia.Media;
using ByteSync.Business.Themes;
using ByteSync.Helpers;
using ByteSync.Interfaces.Controls.Themes;

namespace ByteSync.Services.Themes;

public class ThemeBuilder : IThemeBuilder
{
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
        
        if (themeMode == ThemeModes.Dark)
        {
            BuildDarkColorScheme(colorScheme, themeColor, secondaryColorHue);
        }
        else
        {
            BuildLightColorScheme(colorScheme, themeColor, secondaryColorHue);
        }
        
        ComputeSecondaryColors(colorScheme, secondaryColorHue, themeMode);
        
        colorScheme.StatusMainBackGroundBrush = new SolidColorBrush(colorScheme.StatusMainBackGround.AvaloniaColor);
        colorScheme.StatusSecondaryBackGroundBrush = new SolidColorBrush(colorScheme.StatusSecondaryBackGround.AvaloniaColor);
        colorScheme.VeryLightGrayBrush = new SolidColorBrush(colorScheme.VeryLightGray);
        
        colorScheme.TextControlSelectionHighlightColor = colorScheme.BsAccentButtonBackGround;
        
        return colorScheme;
    }
    
    private void BuildDarkColorScheme(ColorScheme colorScheme, ThemeColor themeColor, double secondaryColorHue)
    {
        var config = new ColorSchemeConfig(
            MainAccentSaturation: 0.65,
            MainAccentValue: 0.50,
            AccentTextSaturation: 0.33,
            AccentTextValue: 0.85,
            HomeCloudSyncSaturation: 0.55,
            HomeCloudSyncValue: 0.70,
            HomeCloudSyncPointerOverSaturation: 0.25,
            HomeCloudSyncPointerOverValue: 0.50,
            ChartsBarSaturation: 0.50,
            ChartsBarValue: 0.75,
            CurrentMemberSaturation: 0.35,
            CurrentMemberValue: 0.22,
            ConnectedMemberLetterSaturation: 0.35,
            ConnectedMemberLetterValue: 0.28,
            ConnectedMemberLetterBorderSaturation: 0.35,
            ConnectedMemberLetterBorderValue: 0.34,
            BsAccentButtonSaturation: 0.55,
            BsAccentButtonValue: 0.25,
            BsAccentButtonPointerOverSaturation: 0.55,
            BsAccentButtonPointerOverValue: 0.35,
            StatusMainBackGroundSaturation: 0.45,
            StatusMainBackGroundValue: 0.25,
            DisabledMemberBackGround: Color.FromArgb(0xFF, 0x30, 0x30, 0x30),
            DisabledMemberLetterBackGround: Color.FromArgb(0xFF, 0x37, 0x37, 0x37),
            DisabledMemberLetterBorder: Color.FromArgb(0xFF, 0x3D, 0x3D, 0x3D),
            VeryLightGray: Color.FromArgb(0xFF, 0x12, 0x12, 0x12),
            GenericButtonBorder: Color.FromArgb(0xFF, 0x55, 0x55, 0x55),
            Gray1: Color.FromArgb(0xFF, 0xCC, 0xCC, 0xCC),
            Gray2: Color.FromArgb(0xFF, 0x80, 0x80, 0x80),
            Gray5: Color.FromArgb(0xFF, 0x46, 0x46, 0x46),
            Gray7: Color.FromArgb(0xFF, 0x3A, 0x3A, 0x3A),
            Gray8: Color.FromArgb(0xFF, 0x2C, 0x2C, 0x2C),
            SettingsHeaderColor: Color.FromArgb(0xFF, 0x30, 0x30, 0x30),
            BlockBackColor: Color.FromArgb(0xFF, 0x1F, 0x1F, 0x1F),
            MainWindowTopColor: Color.FromArgb(0xFF, 0x12, 0x12, 0x12),
            MainWindowBottomColor: Color.FromArgb(0xFF, 0x04, 0x04, 0x04),
            UseDarkAccents: true);
        
        BuildColorScheme(colorScheme, themeColor, secondaryColorHue, config);
    }
    
    private void BuildLightColorScheme(ColorScheme colorScheme, ThemeColor themeColor, double secondaryColorHue)
    {
        var config = new ColorSchemeConfig(
            MainAccentSaturation: 1.0,
            MainAccentValue: 1.0,
            AccentTextSaturation: 1.0,
            AccentTextValue: 1.0,
            HomeCloudSyncSaturation: 0.50,
            HomeCloudSyncValue: 0.65,
            HomeCloudSyncPointerOverSaturation: 0.25,
            HomeCloudSyncPointerOverValue: 0.55,
            ChartsBarSaturation: 0.50,
            ChartsBarValue: 0.80,
            CurrentMemberSaturation: 0.20,
            CurrentMemberValue: 0.92,
            ConnectedMemberLetterSaturation: 0.20,
            ConnectedMemberLetterValue: 0.84,
            ConnectedMemberLetterBorderSaturation: 0.20,
            ConnectedMemberLetterBorderValue: 0.78,
            BsAccentButtonSaturation: 0.15,
            BsAccentButtonValue: 0.95,
            BsAccentButtonPointerOverSaturation: 0.12,
            BsAccentButtonPointerOverValue: 0.98,
            StatusMainBackGroundSaturation: 0.35,
            StatusMainBackGroundValue: 0.90,
            DisabledMemberBackGround: Color.FromArgb(0xFF, 0xEC, 0xEC, 0xEC),
            DisabledMemberLetterBackGround: Color.FromArgb(0xFF, 0xE6, 0xE6, 0xE6),
            DisabledMemberLetterBorder: Color.FromArgb(0xFF, 0xE0, 0xE0, 0xE0),
            VeryLightGray: Color.FromArgb(0xFF, 0xF7, 0xF7, 0xF7),
            GenericButtonBorder: Color.FromArgb(0xFF, 0xAA, 0xAA, 0xAA),
            Gray1: Color.FromArgb(0xFF, 0x33, 0x33, 0x33),
            Gray2: Color.FromArgb(0xFF, 0x7F, 0x7F, 0x7F),
            Gray5: Color.FromArgb(0xFF, 0xB9, 0xB9, 0xB9),
            Gray7: Color.FromArgb(0xFF, 0xD6, 0xD6, 0xD6),
            Gray8: Color.FromArgb(0xFF, 0xE0, 0xE0, 0xE0),
            SettingsHeaderColor: Color.FromArgb(0xFF, 0xEC, 0xEC, 0xEC),
            BlockBackColor: Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF),
            MainWindowTopColor: Color.FromArgb(0xFF, 0xFA, 0xFA, 0xFA),
            MainWindowBottomColor: Color.FromArgb(0xFF, 0xEF, 0xEF, 0xEF),
            UseDarkAccents: false);
        
        BuildColorScheme(colorScheme, themeColor, secondaryColorHue, config);
    }
    
    private void BuildColorScheme(ColorScheme colorScheme, ThemeColor themeColor, double secondaryColorHue, ColorSchemeConfig config)
    {
        colorScheme.MainAccentColor = config.MainAccentSaturation == 1.0 && config.MainAccentValue == 1.0
            ? themeColor
            : themeColor.WithSaturationValue(config.MainAccentSaturation, config.MainAccentValue);
        colorScheme.MainSecondaryColor = colorScheme.MainAccentColor.WithHue(secondaryColorHue);
        
        colorScheme.AccentTextForeGround = config.AccentTextSaturation == 1.0 && config.AccentTextValue == 1.0
            ? themeColor.AvaloniaColor
            : themeColor.WithSaturationValue(config.AccentTextSaturation, config.AccentTextValue).AvaloniaColor;
        
        colorScheme.HomeCloudSynchronizationBackGround = themeColor
            .WithSaturationValue(config.HomeCloudSyncSaturation, config.HomeCloudSyncValue).AvaloniaColor;
        colorScheme.HomeCloudSynchronizationPointerOverBackGround = themeColor
            .WithSaturationValue(config.HomeCloudSyncPointerOverSaturation, config.HomeCloudSyncPointerOverValue).AvaloniaColor;
        colorScheme.HomeLocalSynchronizationBackGround = colorScheme.MainSecondaryColor
            .WithSaturationValue(config.HomeCloudSyncSaturation, config.HomeCloudSyncValue).AvaloniaColor;
        colorScheme.HomeLocalSynchronizationPointerOverBackGround = colorScheme.MainSecondaryColor
            .WithSaturationValue(config.HomeCloudSyncPointerOverSaturation, config.HomeCloudSyncPointerOverValue).AvaloniaColor;
        
        colorScheme.ChartsMainBarColor = themeColor
            .WithSaturationValue(config.ChartsBarSaturation, config.ChartsBarValue).AvaloniaColor;
        colorScheme.ChartsAlternateBarColor = colorScheme.MainSecondaryColor
            .WithSaturationValue(config.ChartsBarSaturation, config.ChartsBarValue).AvaloniaColor;
        colorScheme.ChartsMainLineColor = colorScheme.MainAccentColor.AvaloniaColor;
        
        colorScheme.CurrentMemberBackGround = themeColor
            .WithSaturationValue(config.CurrentMemberSaturation, config.CurrentMemberValue);
        colorScheme.DisabledMemberBackGround = new ThemeColor(config.DisabledMemberBackGround);
        colorScheme.OtherMemberBackGround = new ThemeColor(config.DisabledMemberBackGround);
        
        colorScheme.ConnectedMemberLetterBackGround = themeColor
            .WithSaturationValue(config.ConnectedMemberLetterSaturation, config.ConnectedMemberLetterValue);
        colorScheme.DisabledMemberLetterBackGround = new ThemeColor(config.DisabledMemberLetterBackGround);
        
        colorScheme.ConnectedMemberLetterBorder = themeColor
            .WithSaturationValue(config.ConnectedMemberLetterBorderSaturation, config.ConnectedMemberLetterBorderValue);
        colorScheme.DisabledMemberLetterBorder = new ThemeColor(config.DisabledMemberLetterBorder);
        
        colorScheme.BsAccentButtonBackGround = themeColor
            .WithSaturationValue(config.BsAccentButtonSaturation, config.BsAccentButtonValue).AvaloniaColor;
        colorScheme.BsAccentButtonPointerOverBackGround = themeColor
            .WithSaturationValue(config.BsAccentButtonPointerOverSaturation, config.BsAccentButtonPointerOverValue).AvaloniaColor;
        
        colorScheme.SecondaryButtonBackGround = colorScheme.MainSecondaryColor
            .WithSaturationValue(config.BsAccentButtonSaturation, config.BsAccentButtonValue).AvaloniaColor;
        colorScheme.SecondaryButtonPointerOverBackGround = colorScheme.MainSecondaryColor
            .WithSaturationValue(config.BsAccentButtonPointerOverSaturation, config.BsAccentButtonPointerOverValue).AvaloniaColor;
        
        colorScheme.StatusMainBackGround = themeColor
            .WithSaturationValue(config.StatusMainBackGroundSaturation, config.StatusMainBackGroundValue);
        
        ComputeAttenuations(colorScheme);
        
        if (config.UseDarkAccents)
        {
            colorScheme.Accent1 = colorScheme.SystemAccentColorDark1.AvaloniaColor;
            colorScheme.Accent2 = colorScheme.SystemAccentColorDark2.AvaloniaColor;
            colorScheme.Accent3 = colorScheme.SystemAccentColorDark3.AvaloniaColor;
            colorScheme.Accent4 = colorScheme.SystemAccentColorDark4.AvaloniaColor;
            colorScheme.Accent5 = colorScheme.SystemAccentColorDark5.AvaloniaColor;
        }
        else
        {
            colorScheme.Accent1 = colorScheme.SystemAccentColorLight1.AvaloniaColor;
            colorScheme.Accent2 = colorScheme.SystemAccentColorLight2.AvaloniaColor;
            colorScheme.Accent3 = colorScheme.SystemAccentColorLight3.AvaloniaColor;
            colorScheme.Accent4 = colorScheme.SystemAccentColorLight4.AvaloniaColor;
            colorScheme.Accent5 = colorScheme.SystemAccentColorLight5.AvaloniaColor;
        }
        
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
        colorScheme.SystemAccentColorDark1 = colorScheme.MainAccentColor.AdjustSaturationValue(0, -0.10);
        colorScheme.SystemAccentColorDark2 = colorScheme.MainAccentColor.AdjustSaturationValue(0, -0.20);
        colorScheme.SystemAccentColorDark3 = colorScheme.MainAccentColor.AdjustSaturationValue(0, -0.30);
        colorScheme.SystemAccentColorDark4 = colorScheme.MainAccentColor.AdjustSaturationValue(0, -0.40);
        colorScheme.SystemAccentColorDark5 = colorScheme.MainAccentColor.AdjustSaturationValue(0, -0.50);
        
        colorScheme.SystemAccentColorLight1 = colorScheme.MainAccentColor.AdjustSaturationValue(0, 0.10);
        colorScheme.SystemAccentColorLight2 = colorScheme.MainAccentColor.AdjustSaturationValue(0, 0.20);
        colorScheme.SystemAccentColorLight3 = colorScheme.MainAccentColor.AdjustSaturationValue(0, 0.30);
        colorScheme.SystemAccentColorLight4 = colorScheme.MainAccentColor.AdjustSaturationValue(0, 0.40);
        colorScheme.SystemAccentColorLight5 = colorScheme.MainAccentColor.AdjustSaturationValue(0, 0.50);
    }
}