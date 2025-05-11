using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using ByteSync.Business.Themes;
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
        // BuildThemes(ThemeConstants.BLUE, "#094177");      // PowBlue
        // BuildThemes(ThemeConstants.GOLD, "#b88746");      // PowGold 
        //
        // BuildThemes("Green", "#0d990d");
        // BuildThemes("Red", "#E51400");
        // BuildThemes("Pink", "#F472D0"); 
        // BuildThemes("Purple", "#763dc2");
        //
        // _themeService.OnThemesRegistred();
    }
    
    private void BuildThemes(string themeName, string primaryColorHex)
    {
        ThemeColor themeColor = new ThemeColor(primaryColorHex);

        BuildAndRegisterThemes(themeName + "1", themeColor, themeColor.Hue - 40);
        BuildAndRegisterThemes(themeName + "2", themeColor, themeColor.Hue + 40);
    }

    private void BuildAndRegisterThemes(string themeName, ThemeColor themeColor, double secondaryColorHue)
    {
        Style genericStyle = new Style();

        ColorScheme colorSchemeDark = BuildColorScheme(themeColor, secondaryColorHue, ThemeModes.Dark);
        ColorScheme colorSchemeLight = BuildColorScheme(themeColor, secondaryColorHue, ThemeModes.Light);


        // Allows the DataGrid to be hidden when it is disabled
        genericStyle.Resources.Add("DataGridDisabledVisualElementBackground", Color.FromArgb(0, 0, 0, 0));
        genericStyle.Resources.Add("ContentControlThemeFontFamily", new FontFamily("SansSerif"));
        genericStyle.Resources.Add("ControlContentThemeFontSize", 14d);
        genericStyle.Resources.Add("ToolTipContentMaxWidth", 450d); // https://github.com/AvaloniaUI/Avalonia/blob/master/src/Avalonia.Themes.Fluent/Controls/ToolTip.xaml


        Style specificLightStyle = new Style();
        specificLightStyle.Resources.Add("TextControlSelectionHighlightColor", Color.FromArgb(51, 0, 0, 0)); // SystemBaseLowColor
        Styles lightStyles = BuildStyles(colorSchemeLight, specificLightStyle);
        lightStyles.Add(genericStyle);


        Style specificDarkStyle = new Style();
        specificDarkStyle.Resources.Add("TextControlSelectionHighlightColor", Color.FromArgb(51, 255, 255, 255)); // SystemBaseLowColor
        Styles darkStyles = BuildStyles(colorSchemeDark, specificDarkStyle);
        darkStyles.Add(genericStyle);


        Theme lightTheme = new Theme(themeName, ThemeModes.Light, lightStyles);
        _themeService.RegisterTheme(lightTheme);

        Theme darkTheme = new Theme(themeName, ThemeModes.Dark, darkStyles);
        _themeService.RegisterTheme(darkTheme);
    }

    private Styles BuildStyles(ColorScheme colorScheme, Style themeModeFixedStyle)
    {
        Style style = new Style();
        style.Resources.Add("SystemAccentColor", colorScheme.MainAccentColor.AvaloniaColor);
        style.Resources.Add("SystemAccentColorDark1", colorScheme.SystemAccentColorDark1.AvaloniaColor);
        style.Resources.Add("SystemAccentColorDark2", colorScheme.SystemAccentColorDark2.AvaloniaColor);
        style.Resources.Add("SystemAccentColorDark3", colorScheme.SystemAccentColorDark3.AvaloniaColor);
        style.Resources.Add("SystemAccentColorLight1", colorScheme.SystemAccentColorLight1.AvaloniaColor); 
        style.Resources.Add("SystemAccentColorLight2", colorScheme.SystemAccentColorLight2.AvaloniaColor); 
        style.Resources.Add("SystemAccentColorLight3", colorScheme.SystemAccentColorLight3.AvaloniaColor); 
        style.Resources.Add("SystemAccentColorLight4", colorScheme.SystemAccentColorLight4.AvaloniaColor); 
        style.Resources.Add("SystemAccentColorLight5", colorScheme.SystemAccentColorLight5.AvaloniaColor); 

        style.Resources.Add("Accent0", colorScheme.MainAccentColor.AvaloniaColor);
        style.Resources.Add("Accent1", colorScheme.Accent1);
        style.Resources.Add("Accent2", colorScheme.Accent2);
        style.Resources.Add("Accent3", colorScheme.Accent3);
        style.Resources.Add("Accent4", colorScheme.Accent4);
        style.Resources.Add("Accent5", colorScheme.Accent5);
        
        style.Resources.Add("AccentTextForeGround", colorScheme.AccentTextForeGround);
        
        style.Resources.Add("HomeCloudSynchronizationBackGround", colorScheme.HomeCloudSynchronizationBackGround);
        style.Resources.Add("HomeCloudSynchronizationPointerOverBackGround", colorScheme.HomeCloudSynchronizationPointerOverBackGround);
        style.Resources.Add("HomeLocalSynchronizationBackGround", colorScheme.HomeLocalSynchronizationBackGround);
        style.Resources.Add("HomeLocalSynchronizationPointerOverBackGround", colorScheme.HomeLocalSynchronizationPointerOverBackGround);
        
        style.Resources.Add("ChartsMainBarColor", colorScheme.ChartsMainBarColor);
        style.Resources.Add("ChartsAlternateBarColor", colorScheme.ChartsAlternateBarColor);
        style.Resources.Add("ChartsMainLineColor", colorScheme.ChartsMainLineColor);

        style.Resources.Add("CurrentMemberBackGround", colorScheme.CurrentMemberBackGround.AvaloniaColor);
        style.Resources.Add("DisabledMemberBackGround", colorScheme.DisabledMemberBackGround.AvaloniaColor);
        style.Resources.Add("OtherMemberBackGround", colorScheme.OtherMemberBackGround.AvaloniaColor);
        style.Resources.Add("ConnectedMemberLetterBackGround", colorScheme.ConnectedMemberLetterBackGround);
        style.Resources.Add("DisabledMemberLetterBackGround", colorScheme.DisabledMemberLetterBackGround);
        style.Resources.Add("ConnectedMemberLetterBorder", colorScheme.ConnectedMemberLetterBorder);
        style.Resources.Add("DisabledMemberLetterBorder", colorScheme.DisabledMemberLetterBorder);
        
        style.Resources.Add("BsAccentButtonBackGround", colorScheme.BsAccentButtonBackGround);
        style.Resources.Add("BsAccentButtonPointerOverBackGround", colorScheme.BsAccentButtonPointerOverBackGround);
        
        style.Resources.Add("OppositeButtonBackGround", colorScheme.OppositeButtonBackGround);
        style.Resources.Add("OppositeButtonPointerOverBackGround", colorScheme.OppositeButtonPointerOverBackGround);
        
        style.Resources.Add("StatusMainBackGroundBrush", new SolidColorBrush(colorScheme.StatusMainBackGround.AvaloniaColor));
        style.Resources.Add("StatusOppositeBackGroundBrush", new SolidColorBrush(colorScheme.StatusOppositeBackGround.AvaloniaColor));
        
        style.Resources.Add("Accent0Brush", new SolidColorBrush(colorScheme.MainAccentColor.AvaloniaColor)); 
        style.Resources.Add("Accent1Brush", new SolidColorBrush(colorScheme.Accent1)); 
        style.Resources.Add("Accent2Brush", new SolidColorBrush(colorScheme.Accent2)); 
        style.Resources.Add("Accent3Brush", new SolidColorBrush(colorScheme.Accent3)); 
        style.Resources.Add("Accent4Brush", new SolidColorBrush(colorScheme.Accent4)); 
        style.Resources.Add("Accent5Brush", new SolidColorBrush(colorScheme.Accent5));

        for (int i = 1; i <= 3; i++)
        {
            style.Resources.Add($"Opposite{i}", colorScheme.OppositeColors[i - 1].AvaloniaColor);
            style.Resources.Add($"Opposite{i}Brush", new SolidColorBrush(colorScheme.OppositeColors[i - 1].AvaloniaColor));
        }
        
        // RegisterOppositeColor(style, colorScheme.Accent1, "1");
        // RegisterOppositeColor(style, colorScheme.Accent2, "2");
        // RegisterOppositeColor(style, colorScheme.Accent3, "3");
        
        // string specificUri1;
        string specificUri2;

        if (colorScheme.ThemeMode == ThemeModes.Light)
        {
            // specificUri1 = "avares://Avalonia.Themes.Fluent/Accents/BaseLight.xaml";
            specificUri2 = "avares://ByteSync/Assets/Themes/PowLight.axaml";
        }
        else
        {
            // specificUri1 = "avares://Avalonia.Themes.Fluent/Accents/BaseDark.xaml";
            specificUri2 = "avares://ByteSync/Assets/Themes/PowDark.axaml";
        }

        Styles styles = new Styles
        {
            // new StyleInclude(new Uri("avares://Avalonia.ThemeManager/Styles"))
            // {
            //     Source = new Uri(specificUri1)
            // },
            // new StyleInclude(new Uri("avares://Avalonia.ThemeManager/Styles"))
            // {
            //     Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/Base.xaml")
            // },
            // new StyleInclude(new Uri("avares://Avalonia.ThemeManager/Styles"))
            // {
            //     Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/FluentControlResourcesLight.xaml")
            // },
            // new StyleInclude(new Uri("avares://Avalonia.ThemeManager/Styles"))
            // {
            //     Source = new Uri("avares://Avalonia.Themes.Fluent/Controls/FluentControls.xaml")
            // },
            // new StyleInclude(new Uri("avares://Avalonia.ThemeManager/Styles"))
            // {
            //     Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml")
            // },
            new StyleInclude(new Uri("avares://Avalonia.ThemeManager/Styles"))
            {
                Source = new Uri(specificUri2)
            },
            // new StyleInclude(new Uri("avares://Avalonia.ThemeManager/Styles"))
            // {
            //     Source = new Uri("avares://ByteSync/Assets/Themes/GeneralStyles.axaml")
            // },

            style,
            themeModeFixedStyle,
        };

        return styles;
    }

    private ColorScheme BuildColorScheme(ThemeColor themeColor, double secondaryColorHue, ThemeModes themeMode)
    {
        var colorScheme = new ColorScheme(themeMode);
        
        if (themeMode == ThemeModes.Dark)
        {
            //*** Dark *** \\

            // colorScheme.MainAccentColor = themeColor
            //      .GetWithSaturationValue(0.80, 0.60);


            
            colorScheme.MainAccentColor = themeColor
                .SetSaturationValue(0.65, 0.50);
            
            colorScheme.MainOppositeColor = colorScheme.MainAccentColor
                .SetHue(secondaryColorHue);
            
            // colorScheme.MainAccentColor = themeColor;

            colorScheme.AccentTextForeGround = themeColor
                // .GetWithSaturationValue(0.35, 0.80).AvaloniaColor;
                .SetSaturationValue(0.33, 0.85).AvaloniaColor;

            // Home
            colorScheme.HomeCloudSynchronizationBackGround = themeColor
                .SetSaturationValue(0.55, 0.70).AvaloniaColor;
            colorScheme.HomeCloudSynchronizationPointerOverBackGround = themeColor
                .SetSaturationValue(0.25, 0.50).AvaloniaColor;
            colorScheme.HomeLocalSynchronizationBackGround = colorScheme.MainOppositeColor
                .SetSaturationValue(0.55, 0.70).AvaloniaColor;
            colorScheme.HomeLocalSynchronizationPointerOverBackGround = colorScheme.MainOppositeColor
                .SetSaturationValue(0.25, 0.50).AvaloniaColor;
            
            
            // Charts
            colorScheme.ChartsMainBarColor = themeColor
                .SetSaturationValue(0.50, 0.75).AvaloniaColor;
            colorScheme.ChartsAlternateBarColor = colorScheme.MainOppositeColor
                .SetSaturationValue(0.50, 0.75).AvaloniaColor;
            colorScheme.ChartsMainLineColor = colorScheme.MainAccentColor.AvaloniaColor;
            
            
            // SessionMember / LobbyMember
            colorScheme.CurrentMemberBackGround = themeColor
                .SetSaturationValue(0.55, 0.32);
            colorScheme.DisabledMemberBackGround = themeColor
                .SetSaturationValue(0.0, 0.22);
            colorScheme.OtherMemberBackGround = themeColor
                .SetSaturationValue(0.35, 0.22);
            
            colorScheme.ConnectedMemberLetterBackGround = themeColor
                .SetSaturationValue(0.50, 0.18).AvaloniaColor;
            colorScheme.DisabledMemberLetterBackGround = themeColor
                .SetSaturationValue(0.0, 0.20).AvaloniaColor;
            
            colorScheme.ConnectedMemberLetterBorder = themeColor
                .SetSaturationValue(0.50, 0.21).AvaloniaColor;
            colorScheme.DisabledMemberLetterBorder = themeColor
                .SetSaturationValue(0.0, 0.23).AvaloniaColor;
            
            // AccentButton
            colorScheme.BsAccentButtonBackGround = themeColor
                .SetSaturationValue(0.55, 0.25).AvaloniaColor;
            colorScheme.BsAccentButtonPointerOverBackGround = themeColor
                .SetSaturationValue(0.55, 0.35).AvaloniaColor;
            
            // OppositeButton
            colorScheme.OppositeButtonBackGround = colorScheme.MainOppositeColor
                .SetSaturationValue(0.55, 0.25).AvaloniaColor;
            colorScheme.OppositeButtonPointerOverBackGround = colorScheme.MainOppositeColor
                .SetSaturationValue(0.55, 0.35).AvaloniaColor;
            
            
            colorScheme.StatusMainBackGround = themeColor
                .SetSaturationValue(0.45, 0.25);

            ComputeAttenuations(colorScheme);

            colorScheme.Accent1 = colorScheme.SystemAccentColorDark1.AvaloniaColor;
            colorScheme.Accent2 = colorScheme.SystemAccentColorDark2.AvaloniaColor;
            colorScheme.Accent3 = colorScheme.SystemAccentColorDark3.AvaloniaColor;
            colorScheme.Accent4 = colorScheme.SystemAccentColorDark4.AvaloniaColor;
            colorScheme.Accent5 = colorScheme.SystemAccentColorDark5.AvaloniaColor;
            
            ComputeOpposites(colorScheme, secondaryColorHue);
        }
        else
        {
            //*** Light *** \\
            
            colorScheme.MainAccentColor = themeColor;
            
            colorScheme.MainOppositeColor = colorScheme.MainAccentColor
                .SetHue(secondaryColorHue);
            
            
            colorScheme.AccentTextForeGround = themeColor.AvaloniaColor;
            
            // Home
            colorScheme.HomeCloudSynchronizationBackGround = themeColor
                .SetSaturationValue(0.50, 0.65).AvaloniaColor;
            colorScheme.HomeCloudSynchronizationPointerOverBackGround = themeColor
                .SetSaturationValue(0.25, 0.55).AvaloniaColor;
            colorScheme.HomeLocalSynchronizationBackGround = colorScheme.MainOppositeColor
                .SetSaturationValue(0.50, 0.65).AvaloniaColor;
            colorScheme.HomeLocalSynchronizationPointerOverBackGround = colorScheme.MainOppositeColor
                .SetSaturationValue(0.25, 0.55).AvaloniaColor;
            
            
            // Charts
            colorScheme.ChartsMainBarColor = themeColor
                .SetSaturationValue(0.50, 0.80).AvaloniaColor;
            colorScheme.ChartsAlternateBarColor = colorScheme.MainOppositeColor
                .SetSaturationValue(0.50, 0.80).AvaloniaColor;
            colorScheme.ChartsMainLineColor = colorScheme.MainAccentColor.AvaloniaColor;
            
            
            // SessionMember / LobbyMember
            colorScheme.CurrentMemberBackGround = themeColor
                .SetSaturationValue(0.45, 0.85);
            colorScheme.DisabledMemberBackGround = themeColor
                .SetSaturationValue(0.0, 0.88);
            colorScheme.OtherMemberBackGround = themeColor
                .SetSaturationValue(0.20, 0.92);
            
            colorScheme.ConnectedMemberLetterBackGround = themeColor
                .SetSaturationValue(0.20, 0.95).AvaloniaColor;
            colorScheme.DisabledMemberLetterBackGround = themeColor
                .SetSaturationValue(0.0, 0.95).AvaloniaColor;
                
            colorScheme.ConnectedMemberLetterBorder = themeColor
                .SetSaturationValue(0.20, 0.92).AvaloniaColor;
            colorScheme.DisabledMemberLetterBorder = themeColor
                .SetSaturationValue(0.0, 0.92).AvaloniaColor;
            
            // AccentButton
            colorScheme.BsAccentButtonBackGround = themeColor
                .SetSaturationValue(0.15, 0.95).AvaloniaColor;
            colorScheme.BsAccentButtonPointerOverBackGround = themeColor
                .SetSaturationValue(0.12, 0.98).AvaloniaColor;
            
            // OppositeButton
            colorScheme.OppositeButtonBackGround = colorScheme.MainOppositeColor
                .SetSaturationValue(0.15, 0.95).AvaloniaColor;
            colorScheme.OppositeButtonPointerOverBackGround = colorScheme.MainOppositeColor
                .SetSaturationValue(0.12, 0.98).AvaloniaColor;
            
            colorScheme.StatusMainBackGround = themeColor
                .SetSaturationValue(0.35, 0.90);
            
            ComputeAttenuations(colorScheme);
            
            colorScheme.Accent1 = colorScheme.SystemAccentColorLight1.AvaloniaColor;
            colorScheme.Accent2 = colorScheme.SystemAccentColorLight2.AvaloniaColor;
            colorScheme.Accent3 = colorScheme.SystemAccentColorLight3.AvaloniaColor;
            colorScheme.Accent4 = colorScheme.SystemAccentColorLight4.AvaloniaColor;
            colorScheme.Accent5 = colorScheme.SystemAccentColorLight5.AvaloniaColor;
            
            ComputeOpposites(colorScheme, secondaryColorHue);
        }
        
        return colorScheme;
    }

    private void ComputeOpposites(ColorScheme colorScheme, double secondaryColorHue)
    {
        for (int i = 1; i <= 3; i++)
        {
            double basePercent;
            if (colorScheme.ThemeMode == ThemeModes.Dark)
            {
                basePercent = -0.10;
            }
            else
            {
                basePercent = -0.10;
            }

            var opposite = colorScheme.MainOppositeColor.PercentIncreaseSaturationValue(0, basePercent * i);
            colorScheme.OppositeColors.Add(opposite);
        }
        
        colorScheme.CurrentMemberOppositeBackGround = colorScheme.CurrentMemberBackGround
            .SetHue(secondaryColorHue);
        
        colorScheme.OtherMemberOppositeBackGround = colorScheme.OtherMemberBackGround
            .SetHue(secondaryColorHue);
        
        colorScheme.StatusOppositeBackGround = colorScheme.StatusMainBackGround
            .SetHue(secondaryColorHue);
    }

    private static void ComputeAttenuations(ColorScheme colorScheme)
    {
        colorScheme.SystemAccentColorDark1 = colorScheme.MainAccentColor.PercentIncreaseSaturationValue(0, -0.10);
        colorScheme.SystemAccentColorDark2 = colorScheme.MainAccentColor.PercentIncreaseSaturationValue(0, -0.20);
        colorScheme.SystemAccentColorDark3 = colorScheme.MainAccentColor.PercentIncreaseSaturationValue(0, -0.30);
        colorScheme.SystemAccentColorDark4 = colorScheme.MainAccentColor.PercentIncreaseSaturationValue(0, -0.40);
        colorScheme.SystemAccentColorDark5 = colorScheme.MainAccentColor.PercentIncreaseSaturationValue(0, -0.50);

        colorScheme.SystemAccentColorLight1 = colorScheme.MainAccentColor.PercentIncreaseSaturationValue(0, 0.10);
        colorScheme.SystemAccentColorLight2 = colorScheme.MainAccentColor.PercentIncreaseSaturationValue(0, 0.20);
        colorScheme.SystemAccentColorLight3 = colorScheme.MainAccentColor.PercentIncreaseSaturationValue(0, 0.30);
        colorScheme.SystemAccentColorLight4 = colorScheme.MainAccentColor.PercentIncreaseSaturationValue(0, 0.40);
        colorScheme.SystemAccentColorLight5 = colorScheme.MainAccentColor.PercentIncreaseSaturationValue(0, 0.50);
    }
}