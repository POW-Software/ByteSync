using Avalonia.Media;
using ByteSync.Helpers;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Tests.Helpers;

[TestFixture]
public class ColorUtilsTests
{
    [Test]
    public void FromHex_WithValidThreeByteHexColor_ReturnsCorrectColor()
    {
        // Arrange
        string hexColor = "#FF0080";
        
        // Act
        var result = ColorUtils.FromHex(hexColor);
        
        // Assert
        result.Should().BeEquivalentTo(Color.FromArgb(255, 255, 0, 128));
    }
    
    [Test]
    public void FromHex_WithValidFourByteHexColor_ReturnsCorrectColor()
    {
        // Arrange
        string hexColor = "#80FF0080";
        
        // Act
        var result = ColorUtils.FromHex(hexColor);
        
        // Assert
        result.Should().BeEquivalentTo(Color.FromArgb(128, 255, 0, 128));
    }
    
    [Test]
    public void FromHex_WithoutHashPrefix_StillWorksCorrectly()
    {
        // Arrange
        string hexColor = "FF0080";
        
        // Act
        var result = ColorUtils.FromHex(hexColor);
        
        // Assert
        result.Should().BeEquivalentTo(Color.FromArgb(255, 255, 0, 128));
    }
    
    [Test]
    public void FromHex_WithOddNumberOfDigits_ThrowsException()
    {
        // Arrange
        string hexColor = "#12345";
        
        // Act & Assert
        Action act = () => ColorUtils.FromHex(hexColor);
        act.Should().Throw<Exception>()
            .WithMessage("The binary key cannot have an odd number of digits");
    }
    
    [Test]
    public void FromHex_WithInvalidLength_ThrowsException()
    {
        // Arrange
        string hexColor = "#12";
        
        // Act & Assert
        Action act = () => ColorUtils.FromHex(hexColor);
        act.Should().Throw<Exception>()
            .WithMessage("The binary key does not represent a valid color");
    }
    
    [Test]
    public void ToHex_ReturnsCorrectHexString()
    {
        // Arrange
        var color = Color.FromArgb(128, 255, 0, 128);
        
        // Act
        var result = ColorUtils.ToHex(color);
        
        // Assert
        result.Should().Be("#80FF0080");
    }
    
    [TestCase('0', 0)]
    [TestCase('9', 9)]
    [TestCase('A', 10)]
    [TestCase('F', 15)]
    [TestCase('a', 10)]
    [TestCase('f', 15)]
    public void GetHexVal_ReturnsCorrectValue(char hex, int expected)
    {
        // Act
        var result = ColorUtils.GetHexVal(hex);
        
        // Assert
        result.Should().Be(expected);
    }
    
    [Test]
    public void RgbToHls_ReturnsCorrectValues()
    {
        // Arrange
        var color = Color.FromRgb(255, 0, 0); // Red
        
        // Act
        ColorUtils.RgbToHls(color, out double h, out double l, out double s);
        
        // Assert
        h.Should().BeApproximately(0, 0.001);
        l.Should().BeApproximately(0.5, 0.001);
        s.Should().BeApproximately(1.0, 0.001);
    }
    
    [Test]
    public void HlsToRgb_ReturnsCorrectValues()
    {
        // Arrange
        double h = 0;
        double l = 0.5;
        double s = 1.0;
        
        // Act
        ColorUtils.HlsToRgb(h, l, s, out byte r, out byte g, out byte b);
        
        // Assert
        r.Should().Be(255);
        g.Should().Be(0);
        b.Should().Be(0);
    }
    
    [Test]
    public void HlsToRgb_WithSaturationZero_ReturnsGreyscale()
    {
        // Arrange
        double h = 0;
        double l = 0.5;
        double s = 0;
        
        // Act
        ColorUtils.HlsToRgb(h, l, s, out byte r, out byte g, out byte b);
        
        // Assert
        r.Should().BeInRange(127, 128);
        g.Should().BeInRange(127, 128);
        b.Should().BeInRange(127, 128);
    }
    
    [Test]
    public void BlendWithTransparency_ReturnsCorrectColor()
    {
        // Arrange
        var baseColor = Color.FromRgb(255, 255, 255); // White
        var overlayColor = Color.FromRgb(0, 0, 0);    // Black
        double opacity = 0.5;
        
        // Act
        var result = ColorUtils.BlendWithTransparency(baseColor, overlayColor, opacity);
        
        // Assert
        result.R.Should().BeInRange(127, 128);
        result.G.Should().BeInRange(127, 128);
        result.B.Should().BeInRange(127, 128);
        result.A.Should().Be(255); // Alpha should be the same as baseColor
    }
    
    [Test]
    public void BlendWithTransparency_WithOpacityOutOfRange_ClampsOpacity()
    {
        // Arrange
        var baseColor = Color.FromRgb(200, 200, 200);
        var overlayColor = Color.FromRgb(100, 100, 100);
        double opacity = 1.5; // Out of range, should be clamped to 1.0
        
        // Act
        var result = ColorUtils.BlendWithTransparency(baseColor, overlayColor, opacity);
        
        // Assert
        result.R.Should().Be(100);
        result.G.Should().Be(100);
        result.B.Should().Be(100);
    }
    
    [Test]
    public void ColorToHsv_ReturnsCorrectValues()
    {
        // Arrange
        var color = Color.FromRgb(255, 0, 0); // Red
        
        // Act
        ColorUtils.ColorToHsv(color, out double hue, out double saturation, out double value);
        
        // Assert
        hue.Should().BeApproximately(0, 0.001);
        saturation.Should().BeApproximately(1.0, 0.001);
        value.Should().BeApproximately(1.0, 0.001);
    }
    
    [Test]
    public void ToSystemColor_ReturnsCorrectColor()
    {
        // Arrange
        var avaloniaColor = Color.FromArgb(128, 255, 0, 128);
        
        // Act
        var systemColor = ColorUtils.ToSystemColor(avaloniaColor);
        
        // Assert
        systemColor.A.Should().Be(128);
        systemColor.R.Should().Be(255);
        systemColor.G.Should().Be(0);
        systemColor.B.Should().Be(128);
    }
    
    [Test]
    public void ColorFromHsv_ReturnsCorrectColor()
    {
        // Arrange
        double hue = 0;        // Red
        double saturation = 1.0;
        double value = 1.0;
        
        // Act
        var result = ColorUtils.ColorFromHsv(hue, saturation, value);
        
        // Assert
        result.R.Should().Be(255);
        result.G.Should().Be(0);
        result.B.Should().Be(0);
        result.A.Should().Be(255);
    }
    
    [TestCase(60, 1.0, 1.0, 255, 255, 0)]    // Yellow
    [TestCase(120, 1.0, 1.0, 0, 255, 0)]     // Green
    [TestCase(180, 1.0, 1.0, 0, 255, 255)]   // Cyan
    [TestCase(240, 1.0, 1.0, 0, 0, 255)]     // Blue
    [TestCase(300, 1.0, 1.0, 255, 0, 255)]   // Magenta
    public void ColorFromHsv_WithVariousHues_ReturnsCorrectColors(
        double hue, double saturation, double value, 
        byte expectedR, byte expectedG, byte expectedB)
    {
        // Act
        var result = ColorUtils.ColorFromHsv(hue, saturation, value);
        
        // Assert
        result.R.Should().Be(expectedR);
        result.G.Should().Be(expectedG);
        result.B.Should().Be(expectedB);
    }
}
