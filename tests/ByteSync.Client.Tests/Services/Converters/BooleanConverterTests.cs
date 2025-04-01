using System.Globalization;
using ByteSync.Services.Converters;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Converters;

[TestFixture]
public class BooleanConverterTests
{
    private BooleanConverter<string> _converter;

    [SetUp]
    public void Setup()
    {
        _converter = new BooleanConverter<string>("Yes", "No");
    }

    [Test]
    public void Convert_InputTrue_ReturnsFalseValue()
    {
        // Arrange

        // Act
        var result = _converter.Convert(true, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("No");
    }

    [Test]
    public void Convert_InputFalse_ReturnsBooleanFalse()
    {
        // Arrange

        // Act
        var result = _converter.Convert(false, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(false);
    }

    [Test]
    public void Convert_InputNonBoolean_ReturnsFalseValue()
    {
        // Arrange

        // Act
        var result = _converter.Convert("anything", typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("No");
    }

    [Test]
    public void Convert_InputNull_ReturnsFalseValue()
    {
        // Arrange

        // Act
        var result = _converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("No");
    }

    [Test]
    public void ConvertBack_InputEqualsTrueValue_ReturnsTrue()
    {
        // Arrange

        // Act
        var result = _converter.ConvertBack("Yes", typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(true);
    }

    [Test]
    public void ConvertBack_InputNotEqualTrueValue_ReturnsFalse()
    {
        // Arrange

        // Act
        var result1 = _converter.ConvertBack("No", typeof(bool), null, CultureInfo.InvariantCulture);
        var result2 = _converter.ConvertBack("Other", typeof(bool), null, CultureInfo.InvariantCulture);
        var result3 = _converter.ConvertBack(123, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result1.Should().Be(false);
        result2.Should().Be(false);
        result3.Should().Be(false);
    }

    [Test]
    public void ConvertBack_InputNull_ReturnsFalse()
    {
        // Arrange

        // Act
        var result = _converter.ConvertBack(null, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(false);
    }
}