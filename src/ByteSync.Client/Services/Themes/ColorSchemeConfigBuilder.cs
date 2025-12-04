using Avalonia.Media;
using ByteSync.Business.Themes;

namespace ByteSync.Services.Themes;

public class ColorSchemeConfigBuilder
{
    public ColorSchemeConfig CreateDarkConfig()
    {
        return CreateConfig(
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
    }
    
    public ColorSchemeConfig CreateLightConfig()
    {
        return CreateConfig(
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
    }
    
    private ColorSchemeConfig CreateConfig(
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
}