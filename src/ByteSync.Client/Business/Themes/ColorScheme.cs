using Avalonia.Media;

namespace ByteSync.Business.Themes;

public class ColorScheme
{
    public ColorScheme(ThemeModes themeMode)
    {
        ThemeMode = themeMode;
        SecondaryColors = new List<ThemeColor>();
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
    
    public ThemeColor ConnectedMemberLetterBackGround { get; set; }
    public ThemeColor DisabledMemberLetterBackGround { get; set; }
    public ThemeColor ConnectedMemberLetterBorder { get; set; }
    public ThemeColor DisabledMemberLetterBorder { get; set; }
    
    public Color BsAccentButtonBackGround { get; set; }
    public Color BsAccentButtonPointerOverBackGround { get; set; }
    
    public Color SecondaryButtonBackGround { get; set; }
    public Color SecondaryButtonPointerOverBackGround { get; set; }
    
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
    
    public ThemeColor MainSecondaryColor { get; set; }
    public List<ThemeColor> SecondaryColors { get; set; }
    
    public ThemeColor CurrentMemberSecondaryBackGround { get; set; }
    public ThemeColor OtherMemberSecondaryBackGround { get; set; }
    
    public ThemeColor StatusMainBackGround { get; set; }
    public ThemeColor StatusSecondaryBackGround { get; set; }
    
    public Color ChartsMainBarColor { get; set; }
    public Color ChartsAlternateBarColor { get; set; }
    public Color ChartsMainLineColor { get; set; }
    
    public Color VeryLightGray { get; set; }
    public Color GenericButtonBorder { get; set; }
    public Color Gray1 { get; set; }
    public Color Gray2 { get; set; }
    public Color Gray5 { get; set; }
    public Color Gray8 { get; set; }
    public Color SettingsHeaderColor { get; set; }
    public Color BlockBackColor { get; set; }
    
    public SolidColorBrush StatusMainBackGroundBrush { get; set; }
    public SolidColorBrush StatusSecondaryBackGroundBrush { get; set; }
    public SolidColorBrush VeryLightGrayBrush { get; set; }
    
    public Color MainWindowTopColor { get; set; }
    public Color MainWindowBottomColor { get; set; }
    
    public Color TextControlSelectionHighlightColor { get; set; }
}