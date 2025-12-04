using ByteSync.Business.Themes;
using ByteSync.Services.Themes;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Themes;

[TestFixture]
public class ThemeBuilderTests
{
    private ThemeBuilder _themeBuilder = null!;
    
    [SetUp]
    public void SetUp()
    {
        _themeBuilder = new ThemeBuilder();
    }
    
    [Test]
    public void BuildDefaultTheme_returns_light_blue_theme()
    {
        var theme = _themeBuilder.BuildDefaultTheme();
        
        theme.Should().NotBeNull();
        theme.Name.Should().Be("Blue1");
        theme.Mode.Should().Be(ThemeModes.Light);
        theme.IsDarkMode.Should().BeFalse();
        theme.ColorScheme.Should().NotBeNull();
    }
    
    [Test]
    public void BuildTheme_with_light_mode_creates_correct_theme()
    {
        var theme = _themeBuilder.BuildTheme("TestTheme", "#FF0000", ThemeModes.Light);
        
        theme.Should().NotBeNull();
        theme.Name.Should().Be("TestTheme");
        theme.Mode.Should().Be(ThemeModes.Light);
        theme.IsDarkMode.Should().BeFalse();
        theme.ColorScheme.Should().NotBeNull();
        theme.ColorScheme.ThemeMode.Should().Be(ThemeModes.Light);
    }
    
    [Test]
    public void BuildTheme_with_dark_mode_creates_correct_theme()
    {
        var theme = _themeBuilder.BuildTheme("TestTheme", "#FF0000", ThemeModes.Dark);
        
        theme.Should().NotBeNull();
        theme.Name.Should().Be("TestTheme");
        theme.Mode.Should().Be(ThemeModes.Dark);
        theme.IsDarkMode.Should().BeTrue();
        theme.ColorScheme.Should().NotBeNull();
        theme.ColorScheme.ThemeMode.Should().Be(ThemeModes.Dark);
    }
    
    [Test]
    public void BuildTheme_with_custom_secondary_offset_uses_offset()
    {
        var theme1 = _themeBuilder.BuildTheme("Theme1", "#094177", ThemeModes.Light, -60);
        var theme2 = _themeBuilder.BuildTheme("Theme2", "#094177", ThemeModes.Light, +60);
        
        theme1.SecondaryThemeColor.Hue.Should().NotBe(theme2.SecondaryThemeColor.Hue);
    }
    
    [Test]
    public void BuildTheme_color_scheme_has_all_required_colors()
    {
        var theme = _themeBuilder.BuildDefaultTheme();
        var cs = theme.ColorScheme;
        
        cs.MainAccentColor.Should().NotBeNull();
        cs.MainSecondaryColor.Should().NotBeNull();
        cs.VeryLightGray.Should().NotBe(default);
        cs.Gray1.Should().NotBe(default);
        cs.Gray2.Should().NotBe(default);
        cs.Gray5.Should().NotBe(default);
        cs.Gray7.Should().NotBe(default);
        cs.Gray8.Should().NotBe(default);
        cs.BlockBackColor.Should().NotBe(default);
        cs.SettingsHeaderColor.Should().NotBe(default);
        cs.StatusMainBackGroundBrush.Should().NotBeNull();
        cs.StatusSecondaryBackGroundBrush.Should().NotBeNull();
        cs.VeryLightGrayBrush.Should().NotBeNull();
    }
    
    [TestCase(ThemeConstants.BLUE, ThemeConstants.BLUE_HEX)]
    [TestCase(ThemeConstants.GOLD, ThemeConstants.GOLD_HEX)]
    [TestCase(ThemeConstants.GREEN, ThemeConstants.GREEN_HEX)]
    [TestCase(ThemeConstants.RED, ThemeConstants.RED_HEX)]
    [TestCase(ThemeConstants.PINK, ThemeConstants.PINK_HEX)]
    [TestCase(ThemeConstants.PURPLE, ThemeConstants.PURPLE_HEX)]
    public void BuildTheme_works_for_all_standard_colors(string name, string hex)
    {
        var lightTheme = _themeBuilder.BuildTheme(name, hex, ThemeModes.Light);
        var darkTheme = _themeBuilder.BuildTheme(name, hex, ThemeModes.Dark);
        
        lightTheme.Should().NotBeNull();
        darkTheme.Should().NotBeNull();
        lightTheme.ColorScheme.Should().NotBeNull();
        darkTheme.ColorScheme.Should().NotBeNull();
    }
}