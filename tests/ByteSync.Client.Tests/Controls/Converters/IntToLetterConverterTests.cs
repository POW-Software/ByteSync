using System.Globalization;
using ByteSync.Services.Converters;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Tests.Controls.Converters;

[TestFixture]
public class IntToLetterConverterTests
{
    private IntToLetterConverter _converter;

    [SetUp]
    public void SetUp()
    {
        _converter = new IntToLetterConverter();
    }

    [Test]
    public void Convert_IntegerValue_ShouldReturnCorrespondingLetter()
    {
        // Arrange & Act
        var result0 = _converter.Convert(0, typeof(string), null, CultureInfo.InvariantCulture);
        var result1 = _converter.Convert(1, typeof(string), null, CultureInfo.InvariantCulture);
        var result25 = _converter.Convert(25, typeof(string), null, CultureInfo.InvariantCulture);
        var resultNegative = _converter.Convert(-1, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result0.Should().Be("A", "0 corresponds to 'A'");
        result1.Should().Be("B", "1 corresponds to 'B'");
        result25.Should().Be("Z", "25 corresponds to 'Z'");
        resultNegative.Should().Be(((char)('A' - 1)).ToString(), "for a negative value, arithmetic conversion applies");
    }

    [Test]
    public void Convert_NonIntegerValue_ShouldConvertToIntegerAndReturnLetter()
    {
        // Arrange
        object value = "2"; // "2" will be converted to int 2 via System.Convert.ToInt32

        // Act
        var result = _converter.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("C");
    }

    [Test]
    public void ConvertBack_ValidLetter_ShouldReturnCorrespondingInteger()
    {
        // Arrange & Act
        var resultA = _converter.ConvertBack("A", typeof(int), null, CultureInfo.InvariantCulture);
        var resultB = _converter.ConvertBack("b", typeof(int), null, CultureInfo.InvariantCulture); // Test with lowercase

        // Assert
        resultA.Should().Be(0, "A corresponds to 0");
        resultB.Should().Be(1, "b, converted to uppercase, corresponds to 1");
    }

    [Test]
    public void ConvertBack_InvalidValue_ShouldThrowArgumentOutOfRangeException()
    {
        Action actMultipleChars = () => _converter.ConvertBack("AB", typeof(int), null, CultureInfo.InvariantCulture);
        actMultipleChars.Should().Throw<ArgumentOutOfRangeException>();

        Action actNonString = () => _converter.ConvertBack(123, typeof(int), null, CultureInfo.InvariantCulture);
        actNonString.Should().Throw<ArgumentOutOfRangeException>();

        Action actNonLetter = () => _converter.ConvertBack("1", typeof(int), null, CultureInfo.InvariantCulture);
        actNonLetter.Should().Throw<ArgumentOutOfRangeException>();
    }
}