using Avalonia.Media;
using ByteSync.Business.Themes;
using ByteSync.Helpers;
using ByteSync.Interfaces.Controls.Themes;

namespace ByteSync.Services.Themes;

public class ThemeBuilder : IThemeBuilder
{
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
        colorScheme.MainAccentColor = themeColor.WithSaturationValue(0.65, 0.50);
        colorScheme.MainSecondaryColor = colorScheme.MainAccentColor.WithHue(secondaryColorHue);
        
        colorScheme.AccentTextForeGround = themeColor.WithSaturationValue(0.33, 0.85).AvaloniaColor;
        
        colorScheme.HomeCloudSynchronizationBackGround = themeColor
            .WithSaturationValue(0.55, 0.70).AvaloniaColor;
        colorScheme.HomeCloudSynchronizationPointerOverBackGround = themeColor
            .WithSaturationValue(0.25, 0.50).AvaloniaColor;
        colorScheme.HomeLocalSynchronizationBackGround = colorScheme.MainSecondaryColor
            .WithSaturationValue(0.55, 0.70).AvaloniaColor;
        colorScheme.HomeLocalSynchronizationPointerOverBackGround = colorScheme.MainSecondaryColor
            .WithSaturationValue(0.25, 0.50).AvaloniaColor;
        
        colorScheme.ChartsMainBarColor = themeColor
            .WithSaturationValue(0.50, 0.75).AvaloniaColor;
        colorScheme.ChartsAlternateBarColor = colorScheme.MainSecondaryColor
            .WithSaturationValue(0.50, 0.75).AvaloniaColor;
        colorScheme.ChartsMainLineColor = colorScheme.MainAccentColor.AvaloniaColor;
        
        colorScheme.CurrentMemberBackGround = themeColor
            .WithSaturationValue(0.35, 0.22);
        colorScheme.DisabledMemberBackGround = new ThemeColor(Color.FromArgb(0xFF, 0x30, 0x30, 0x30));
        colorScheme.OtherMemberBackGround = new ThemeColor(Color.FromArgb(0xFF, 0x30, 0x30, 0x30));
        
        colorScheme.ConnectedMemberLetterBackGround = themeColor
            .WithSaturationValue(0.35, 0.28);
        colorScheme.DisabledMemberLetterBackGround = new ThemeColor(Color.FromArgb(0xFF, 0x37, 0x37, 0x37));
        
        colorScheme.ConnectedMemberLetterBorder = themeColor
            .WithSaturationValue(0.35, 0.34);
        colorScheme.DisabledMemberLetterBorder = new ThemeColor(Color.FromArgb(0xFF, 0x3D, 0x3D, 0x3D));
        
        colorScheme.BsAccentButtonBackGround = themeColor
            .WithSaturationValue(0.55, 0.25).AvaloniaColor;
        colorScheme.BsAccentButtonPointerOverBackGround = themeColor
            .WithSaturationValue(0.55, 0.35).AvaloniaColor;
        
        colorScheme.SecondaryButtonBackGround = colorScheme.MainSecondaryColor
            .WithSaturationValue(0.55, 0.25).AvaloniaColor;
        colorScheme.SecondaryButtonPointerOverBackGround = colorScheme.MainSecondaryColor
            .WithSaturationValue(0.55, 0.35).AvaloniaColor;
        
        colorScheme.StatusMainBackGround = themeColor
            .WithSaturationValue(0.45, 0.25);
        
        ComputeAttenuations(colorScheme);
        
        colorScheme.Accent1 = colorScheme.SystemAccentColorDark1.AvaloniaColor;
        colorScheme.Accent2 = colorScheme.SystemAccentColorDark2.AvaloniaColor;
        colorScheme.Accent3 = colorScheme.SystemAccentColorDark3.AvaloniaColor;
        colorScheme.Accent4 = colorScheme.SystemAccentColorDark4.AvaloniaColor;
        colorScheme.Accent5 = colorScheme.SystemAccentColorDark5.AvaloniaColor;
        
        colorScheme.VeryLightGray = Color.FromArgb(0xFF, 0x12, 0x12, 0x12);
        colorScheme.GenericButtonBorder = Color.FromArgb(0xFF, 0x55, 0x55, 0x55);
        colorScheme.Gray1 = Color.FromArgb(0xFF, 0xCC, 0xCC, 0xCC);
        colorScheme.Gray2 = Color.FromArgb(0xFF, 0x80, 0x80, 0x80);
        colorScheme.Gray5 = Color.FromArgb(0xFF, 0x46, 0x46, 0x46);
        colorScheme.Gray7 = Color.FromArgb(0xFF, 0x3A, 0x3A, 0x3A);
        colorScheme.Gray8 = Color.FromArgb(0xFF, 0x2C, 0x2C, 0x2C);
        colorScheme.SettingsHeaderColor = Color.FromArgb(0xFF, 0x30, 0x30, 0x30);
        colorScheme.BlockBackColor = Color.FromArgb(0xFF, 0x1F, 0x1F, 0x1F);
        
        colorScheme.MainWindowTopColor = colorScheme.VeryLightGray;
        colorScheme.MainWindowBottomColor = Color.FromArgb(0xFF, 0x04, 0x04, 0x04);
    }
    
    private void BuildLightColorScheme(ColorScheme colorScheme, ThemeColor themeColor, double secondaryColorHue)
    {
        colorScheme.MainAccentColor = themeColor;
        colorScheme.MainSecondaryColor = colorScheme.MainAccentColor.WithHue(secondaryColorHue);
        
        colorScheme.AccentTextForeGround = themeColor.AvaloniaColor;
        
        colorScheme.HomeCloudSynchronizationBackGround = themeColor
            .WithSaturationValue(0.50, 0.65).AvaloniaColor;
        colorScheme.HomeCloudSynchronizationPointerOverBackGround = themeColor
            .WithSaturationValue(0.25, 0.55).AvaloniaColor;
        colorScheme.HomeLocalSynchronizationBackGround = colorScheme.MainSecondaryColor
            .WithSaturationValue(0.50, 0.65).AvaloniaColor;
        colorScheme.HomeLocalSynchronizationPointerOverBackGround = colorScheme.MainSecondaryColor
            .WithSaturationValue(0.25, 0.55).AvaloniaColor;
        
        colorScheme.ChartsMainBarColor = themeColor
            .WithSaturationValue(0.50, 0.80).AvaloniaColor;
        colorScheme.ChartsAlternateBarColor = colorScheme.MainSecondaryColor
            .WithSaturationValue(0.50, 0.80).AvaloniaColor;
        colorScheme.ChartsMainLineColor = colorScheme.MainAccentColor.AvaloniaColor;
        
        colorScheme.CurrentMemberBackGround = themeColor
            .WithSaturationValue(0.20, 0.92);
        colorScheme.DisabledMemberBackGround = new ThemeColor(Color.FromArgb(0xFF, 0xEC, 0xEC, 0xEC));
        colorScheme.OtherMemberBackGround = new ThemeColor(Color.FromArgb(0xFF, 0xEC, 0xEC, 0xEC));
        
        colorScheme.ConnectedMemberLetterBackGround = themeColor
            .WithSaturationValue(0.20, 0.84);
        colorScheme.DisabledMemberLetterBackGround = new ThemeColor(Color.FromArgb(0xFF, 0xE6, 0xE6, 0xE6));
        
        colorScheme.ConnectedMemberLetterBorder = themeColor
            .WithSaturationValue(0.20, 0.78);
        colorScheme.DisabledMemberLetterBorder = new ThemeColor(Color.FromArgb(0xFF, 0xE0, 0xE0, 0xE0));
        
        colorScheme.BsAccentButtonBackGround = themeColor
            .WithSaturationValue(0.15, 0.95).AvaloniaColor;
        colorScheme.BsAccentButtonPointerOverBackGround = themeColor
            .WithSaturationValue(0.12, 0.98).AvaloniaColor;
        
        colorScheme.SecondaryButtonBackGround = colorScheme.MainSecondaryColor
            .WithSaturationValue(0.15, 0.95).AvaloniaColor;
        colorScheme.SecondaryButtonPointerOverBackGround = colorScheme.MainSecondaryColor
            .WithSaturationValue(0.12, 0.98).AvaloniaColor;
        
        colorScheme.StatusMainBackGround = themeColor
            .WithSaturationValue(0.35, 0.90);
        
        ComputeAttenuations(colorScheme);
        
        colorScheme.Accent1 = colorScheme.SystemAccentColorLight1.AvaloniaColor;
        colorScheme.Accent2 = colorScheme.SystemAccentColorLight2.AvaloniaColor;
        colorScheme.Accent3 = colorScheme.SystemAccentColorLight3.AvaloniaColor;
        colorScheme.Accent4 = colorScheme.SystemAccentColorLight4.AvaloniaColor;
        colorScheme.Accent5 = colorScheme.SystemAccentColorLight5.AvaloniaColor;
        
        colorScheme.VeryLightGray = Color.FromArgb(0xFF, 0xF7, 0xF7, 0xF7);
        colorScheme.GenericButtonBorder = Color.FromArgb(0xFF, 0xAA, 0xAA, 0xAA);
        colorScheme.Gray1 = Color.FromArgb(0xFF, 0x33, 0x33, 0x33);
        colorScheme.Gray2 = Color.FromArgb(0xFF, 0x7F, 0x7F, 0x7F);
        colorScheme.Gray5 = Color.FromArgb(0xFF, 0xB9, 0xB9, 0xB9);
        colorScheme.Gray7 = Color.FromArgb(0xFF, 0xD6, 0xD6, 0xD6);
        colorScheme.Gray8 = Color.FromArgb(0xFF, 0xE0, 0xE0, 0xE0);
        colorScheme.SettingsHeaderColor = Color.FromArgb(0xFF, 0xEC, 0xEC, 0xEC);
        colorScheme.BlockBackColor = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
        
        colorScheme.MainWindowTopColor = Color.FromArgb(0xFF, 0xFA, 0xFA, 0xFA);
        colorScheme.MainWindowBottomColor = Color.FromArgb(0xFF, 0xEF, 0xEF, 0xEF);
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