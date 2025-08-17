using System.Globalization;
using ByteSync.Interfaces;
using ByteSync.Services.Converters;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Converters;

[TestFixture]
public class ItemCountToLocalizedTextConverterTests
{
    private Mock<ILocalizationService> _mockLocalizationService;
    private ItemCountToLocalizedTextConverter _converter;

    [SetUp]
    public void SetUp()
    {
        _mockLocalizationService = new Mock<ILocalizationService>();
        
        // Setup mock responses for localization keys
        _mockLocalizationService.Setup(x => x["ValidationFailure_ItemSingular"])
            .Returns("item:");
        _mockLocalizationService.Setup(x => x["ValidationFailure_ItemPlural"])
            .Returns("items:");

        // Use constructor injection for testing
        _converter = new ItemCountToLocalizedTextConverter(_mockLocalizationService.Object);
    }

    [Test]
    public void Convert_CountZero_ShouldReturnSingularForm()
    {
        // Arrange
        
        // Act
        var result = _converter.Convert(0, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("item:");
    }

    [Test]
    public void Convert_CountOne_ShouldReturnSingularForm()
    {
        // Arrange
        
        // Act
        var result = _converter.Convert(1, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("item:");
    }

    [Test]
    public void Convert_CountTwo_ShouldReturnPluralForm()
    {
        // Arrange
        
        // Act
        var result = _converter.Convert(2, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("items:");
    }

    [Test]
    public void Convert_CountMultiple_ShouldReturnPluralForm()
    {
        // Arrange
        
        // Act
        var result5 = _converter.Convert(5, typeof(string), null, CultureInfo.InvariantCulture);
        var result100 = _converter.Convert(100, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result5.Should().Be("items:");
        result100.Should().Be("items:");
    }

    [Test]
    public void Convert_CountNegative_ShouldReturnSingularForm()
    {
        // Arrange
        
        // Act
        var result = _converter.Convert(-1, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("item:");
    }

    [Test]
    public void Convert_NonIntegerValue_ShouldReturnSingularForm()
    {
        // Arrange
        
        // Act
        var resultString = _converter.Convert("test", typeof(string), null, CultureInfo.InvariantCulture);
        var resultDouble = _converter.Convert(3.14, typeof(string), null, CultureInfo.InvariantCulture);
        var resultBool = _converter.Convert(true, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        resultString.Should().Be("item:");
        resultDouble.Should().Be("item:");
        resultBool.Should().Be("item:");
    }

    [Test]
    public void Convert_NullValue_ShouldReturnSingularForm()
    {
        // Arrange
        
        // Act
        var result = _converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("item:");
    }

    [Test]
    public void Convert_WithFrenchLocalization_ShouldReturnLocalizedText()
    {
        // Arrange - Setup French localization
        _mockLocalizationService.Setup(x => x["ValidationFailure_ItemSingular"])
            .Returns("élément :");
        _mockLocalizationService.Setup(x => x["ValidationFailure_ItemPlural"])
            .Returns("éléments :");
        
        var converterFr = new ItemCountToLocalizedTextConverter(_mockLocalizationService.Object);

        // Act
        var resultSingular = converterFr.Convert(1, typeof(string), null, CultureInfo.InvariantCulture);
        var resultPlural = converterFr.Convert(3, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        resultSingular.Should().Be("élément :");
        resultPlural.Should().Be("éléments :");
    }

    [Test]
    public void Convert_CallsCorrectLocalizationKey()
    {
        // Arrange & Act
        _converter.Convert(1, typeof(string), null, CultureInfo.InvariantCulture);
        _converter.Convert(3, typeof(string), null, CultureInfo.InvariantCulture);
        _converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert - Verify the correct keys were called
        _mockLocalizationService.Verify(x => x["ValidationFailure_ItemSingular"], Times.Exactly(2), "Should call singular key for count 1 and null");
        _mockLocalizationService.Verify(x => x["ValidationFailure_ItemPlural"], Times.Once, "Should call plural key for count 3");
    }

    [Test]
    public void ConvertBack_ShouldThrowNotImplementedException()
    {
        // Arrange & Act & Assert
        Action act = () => _converter.ConvertBack("item:", typeof(int), null, CultureInfo.InvariantCulture);
        act.Should().Throw<NotImplementedException>();
    }
}
