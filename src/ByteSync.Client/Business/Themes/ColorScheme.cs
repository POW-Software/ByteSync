using Avalonia.Media;

namespace ByteSync.Business.Themes;

public class ColorScheme
{
    public ColorScheme(ThemeModes themeMode)
    {
        ThemeMode = themeMode;

        OppositeColors = new List<ThemeColor>();
    }

    public ThemeModes ThemeMode { get; }
    
    public ThemeColor MainAccentColor { get; set; }

    public Color Accent1 { get; set; }
    
    public Color Accent2 { get; set; }
    
    public Color Accent3 { get; set; }
    
    public Color Accent4 { get; set; }
    
    public Color Accent5 { get; set; }
    
    public Color AccentTextForeGround { get; set; }

    public Color HomeCloudSynchronizationBackGround { get; set; }
    
    public Color HomeCloudSynchronizationPointerOverBackGround { get; set; }

    public Color HomeLocalSynchronizationBackGround { get; set; }
    
    public Color HomeLocalSynchronizationPointerOverBackGround { get; set; }
    
    public ThemeColor CurrentMemberBackGround { get; set; }
    
    public ThemeColor DisabledMemberBackGround { get; set; }
    
    public ThemeColor OtherMemberBackGround { get; set; }
    
    public Color ConnectedMemberLetterBackGround { get; set; }
    
    public Color DisabledMemberLetterBackGround { get; set; }
    
    public Color ConnectedMemberLetterBorder { get; set; }
    
    public Color DisabledMemberLetterBorder { get; set; }
    
    public Color PowAccentButtonBackGround { get; set; }
    
    public Color PowAccentButtonPointerOverBackGround { get; set; }
    
    public ThemeColor SystemAccentColorDark1 { get; set; }
    
    public ThemeColor SystemAccentColorDark2 { get; set; }

    public ThemeColor SystemAccentColorDark3 { get; set; }

    public ThemeColor SystemAccentColorDark4 { get; set; }
    
    public ThemeColor SystemAccentColorDark5 { get; set; }

    public ThemeColor SystemAccentColorLight1 { get; set; }
    
    public ThemeColor SystemAccentColorLight2 { get; set; }
    
    public ThemeColor SystemAccentColorLight3 { get; set; }
    
    public ThemeColor SystemAccentColorLight4 { get; set; }
    
    public ThemeColor SystemAccentColorLight5 { get; set; }
    
    public ThemeColor MainOppositeColor { get; set; }
    
    public List<ThemeColor> OppositeColors { get; set; }
    
    public ThemeColor CurrentMemberOppositeBackGround { get; set; }
    
    public ThemeColor OtherMemberOppositeBackGround { get; set; }
    
    public ThemeColor StatusMainBackGround { get; set; }
    
    public ThemeColor StatusOppositeBackGround { get; set; }
    
    public Color ChartsMainBarColor { get; set; }
    
    public Color ChartsAlternateBarColor { get; set; }
    
    public Color ChartsMainLineColor { get; set; }
}