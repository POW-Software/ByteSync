using Avalonia.Media;
using ByteSync.Business.Themes;
using ByteSync.Helpers;
using ByteSync.Interfaces.Controls.Themes;

namespace ByteSync.Services.Themes;

public class ThemeFactory : IThemeFactory
{
    private readonly IThemeService _themeService;

    public ThemeFactory(IThemeService themeService)
    {
        _themeService = themeService;
    }
    
    public void BuildThemes()
    {
        BuildThemes(ThemeConstants.BLUE, "#094177");      // PowBlue
        BuildThemes(ThemeConstants.GOLD, "#b88746");      // PowGold 
        
        BuildThemes("Green", "#0d990d");
        BuildThemes("Red", "#E51400");
        BuildThemes("Pink", "#F472D0"); 
        BuildThemes("Purple", "#763dc2");
        
        _themeService.OnThemesRegistered();
    }
    
    private void BuildThemes(string themeName, string primaryColorHex)
    {
        ThemeColor themeColor = new ThemeColor(primaryColorHex);

        // Create two variants with different secondary colors
        BuildAndRegisterThemes(themeName + "1", themeColor, themeColor.Hue - 60, primaryColorHex);
        BuildAndRegisterThemes(themeName + "2", themeColor, themeColor.Hue + 60, primaryColorHex);
    }

    private void BuildAndRegisterThemes(string themeName, ThemeColor themeColor, double secondaryColorHue, string primaryColorHex)
    {
        // Create secondary theme color with given hue but same saturation/value as primary
        var secondarySystemColor = ColorUtils.ColorFromHsv(secondaryColorHue, themeColor.Saturation, themeColor.Value);
        ThemeColor secondaryThemeColor = new ThemeColor(secondarySystemColor);

        // Create light theme
        Theme lightTheme = new Theme(themeName, ThemeModes.Light, themeColor, secondaryThemeColor);
        BuildColorScheme(lightTheme, themeColor, secondaryColorHue, ThemeModes.Light);
        _themeService.RegisterTheme(lightTheme);

        // Create dark theme
        Theme darkTheme = new Theme(themeName, ThemeModes.Dark, themeColor, secondaryThemeColor);
        BuildColorScheme(darkTheme, themeColor, secondaryColorHue, ThemeModes.Dark);
        _themeService.RegisterTheme(darkTheme);
    }
    
    private ColorScheme BuildColorScheme(Theme theme, ThemeColor themeColor, double secondaryColorHue, ThemeModes themeMode)
    {
        var colorScheme = new ColorScheme(themeMode);
        
        if (themeMode == ThemeModes.Dark)
        {
            //*** Dark *** \\
            colorScheme.MainAccentColor = themeColor.WithSaturationValue(0.65, 0.50);
            colorScheme.MainSecondaryColor = colorScheme.MainAccentColor.WithHue(secondaryColorHue);
            
            colorScheme.AccentTextForeGround = themeColor.WithSaturationValue(0.33, 0.85).AvaloniaColor;

            // Home
            colorScheme.HomeCloudSynchronizationBackGround = themeColor
                .WithSaturationValue(0.55, 0.70).AvaloniaColor;
            colorScheme.HomeCloudSynchronizationPointerOverBackGround = themeColor
                .WithSaturationValue(0.25, 0.50).AvaloniaColor;
            colorScheme.HomeLocalSynchronizationBackGround = colorScheme.MainSecondaryColor
                .WithSaturationValue(0.55, 0.70).AvaloniaColor;
            colorScheme.HomeLocalSynchronizationPointerOverBackGround = colorScheme.MainSecondaryColor
                .WithSaturationValue(0.25, 0.50).AvaloniaColor;
            
            // Charts
            colorScheme.ChartsMainBarColor = themeColor
                .WithSaturationValue(0.50, 0.75).AvaloniaColor;
            colorScheme.ChartsAlternateBarColor = colorScheme.MainSecondaryColor
                .WithSaturationValue(0.50, 0.75).AvaloniaColor;
            colorScheme.ChartsMainLineColor = colorScheme.MainAccentColor.AvaloniaColor;
            
            // SessionMember / LobbyMember
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
            
            // AccentButton
            colorScheme.BsAccentButtonBackGround = themeColor
                .WithSaturationValue(0.55, 0.25).AvaloniaColor;
            colorScheme.BsAccentButtonPointerOverBackGround = themeColor
                .WithSaturationValue(0.55, 0.35).AvaloniaColor;
            
            // SecondaryButton
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
            colorScheme.Gray8 = Color.FromArgb(0xFF, 0x2C, 0x2C, 0x2C);
            colorScheme.SettingsHeaderColor = Color.FromArgb(0xFF, 0x30, 0x30, 0x30);
            colorScheme.BlockBackColor = Color.FromArgb(0xFF, 0x1F, 0x1F, 0x1F);

            colorScheme.MainWindowTopColor = colorScheme.VeryLightGray;
            colorScheme.MainWindowBottomColor = Color.FromArgb(0xFF, 0x04, 0x04, 0x04);
            
            ComputeSecondaryColors(colorScheme, secondaryColorHue);
            
            colorScheme.StatusMainBackGroundBrush = new SolidColorBrush(colorScheme.StatusMainBackGround.AvaloniaColor);
            colorScheme.StatusSecondaryBackGroundBrush = new SolidColorBrush(colorScheme.StatusSecondaryBackGround.AvaloniaColor);
            colorScheme.VeryLightGrayBrush = new SolidColorBrush(colorScheme.VeryLightGray);
            
            colorScheme.TextControlSelectionHighlightColor = colorScheme.BsAccentButtonBackGround;
        }
        else
        {
            //*** Light *** \\
            colorScheme.MainAccentColor = themeColor;
            colorScheme.MainSecondaryColor = colorScheme.MainAccentColor.WithHue(secondaryColorHue);
            
            colorScheme.AccentTextForeGround = themeColor.AvaloniaColor;
            
            // Home
            colorScheme.HomeCloudSynchronizationBackGround = themeColor
                .WithSaturationValue(0.50, 0.65).AvaloniaColor;
            colorScheme.HomeCloudSynchronizationPointerOverBackGround = themeColor
                .WithSaturationValue(0.25, 0.55).AvaloniaColor;
            colorScheme.HomeLocalSynchronizationBackGround = colorScheme.MainSecondaryColor
                .WithSaturationValue(0.50, 0.65).AvaloniaColor;
            colorScheme.HomeLocalSynchronizationPointerOverBackGround = colorScheme.MainSecondaryColor
                .WithSaturationValue(0.25, 0.55).AvaloniaColor;
            
            // Charts
            colorScheme.ChartsMainBarColor = themeColor
                .WithSaturationValue(0.50, 0.80).AvaloniaColor;
            colorScheme.ChartsAlternateBarColor = colorScheme.MainSecondaryColor
                .WithSaturationValue(0.50, 0.80).AvaloniaColor;
            colorScheme.ChartsMainLineColor = colorScheme.MainAccentColor.AvaloniaColor;
            
            // SessionMember / LobbyMember
            colorScheme.CurrentMemberBackGround = themeColor
                .WithSaturationValue(0.20, 0.92);
            colorScheme.DisabledMemberBackGround = new ThemeColor(Color.FromArgb(0xFF, 0xEC, 0xEC, 0xEC));
            colorScheme.OtherMemberBackGround = new ThemeColor(Color.FromArgb(0xFF, 0xEC, 0xEC, 0xEC));
            
            colorScheme.ConnectedMemberLetterBackGround = themeColor
                .WithSaturationValue(0.20, 0.84);
            colorScheme.DisabledMemberLetterBackGround = new ThemeColor(Color.FromArgb(0xFF, 0xE6, 0xE6, 0xE6));
                
            colorScheme.ConnectedMemberLetterBorder = themeColor
                .WithSaturationValue(0.20, 0.78);
            colorScheme.DisabledMemberLetterBorder = new ThemeColor(Color.FromArgb(0xFF, 0xE0, 0xE0, 0xE0)); // new ThemeColor(Color.FromArgb(0xFF, 0xDD, 0xDD, 0xDD));
            
            // AccentButton
            colorScheme.BsAccentButtonBackGround = themeColor
                .WithSaturationValue(0.15, 0.95).AvaloniaColor;
            colorScheme.BsAccentButtonPointerOverBackGround = themeColor
                .WithSaturationValue(0.12, 0.98).AvaloniaColor;
            
            // SecondaryButton
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
            colorScheme.Gray8 = Color.FromArgb(0xFF, 0xE0, 0xE0, 0xE0);
            colorScheme.SettingsHeaderColor = Color.FromArgb(0xFF, 0xEC, 0xEC, 0xEC);
            colorScheme.BlockBackColor = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);

            colorScheme.MainWindowTopColor = Color.FromArgb(0xFF, 0xFA, 0xFA, 0xFA);
            colorScheme.MainWindowBottomColor = Color.FromArgb(0xFF, 0xEF, 0xEF, 0xEF);
            
            ComputeSecondaryColors(colorScheme, secondaryColorHue);
            
            colorScheme.StatusMainBackGroundBrush = new SolidColorBrush(colorScheme.StatusMainBackGround.AvaloniaColor);
            colorScheme.StatusSecondaryBackGroundBrush = new SolidColorBrush(colorScheme.StatusSecondaryBackGround.AvaloniaColor);
            colorScheme.VeryLightGrayBrush = new SolidColorBrush(colorScheme.VeryLightGray);
            
            colorScheme.TextControlSelectionHighlightColor = colorScheme.BsAccentButtonBackGround;
        }
        
        theme.ColorScheme = colorScheme;
        
        return colorScheme;
    }
    
    private void ComputeSecondaryColors(ColorScheme colorScheme, double secondaryColorHue)
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
        
        colorScheme.StatusSecondaryBackGround = colorScheme.StatusMainBackGround
            .WithHue(secondaryColorHue);
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