using FluentAssertions;

namespace ByteSync.Client.IntegrationTests.Business.Filtering;

public class TestFiltering_LastWriteTime : BaseTestFiltering
{
    [SetUp]
    public void Setup()
    {
        SetupBase();
    }

    [TestCase("2024-05-01", "2024-05-01", "==", true)]
    [TestCase("2024-05-01", "2024-05-01", ">=", true)]
    [TestCase("2024-05-01", "2024-05-01", "<=", true)]
    [TestCase("2024-05-01", "2024-06-01", "==", false)]
    [TestCase("2024-05-01", "2024-06-01", "!=", true)]
    [TestCase("2024-05-01", "2024-06-01", "<=", true)]
    [TestCase("2024-05-01", "2024-06-01", "<", true)]
    [TestCase("2024-05-01", "2024-06-01", ">=", false)]
    [TestCase("2024-05-01", "2024-06-01", ">", false)]
    [TestCase("2024-06-01", "2024-05-01", ">=", true)]
    [TestCase("2024-06-01", "2024-05-01", ">", true)]
    [TestCase("2024-06-01", "2024-05-01", "<=", false)]
    [TestCase("2024-06-01", "2024-05-01", "<", false)]
    [TestCase("2024-05-01", "2024-06-01", "<", true)]
    [TestCase("2024-05-01", "2024-05-01", "<", false)]
    [TestCase("2024-06-01", "2024-05-01", ">", true)]
    [TestCase("2024-05-01", "2024-05-01", ">", false)]
    public void TestLastWriteTimeComparison_WithTarget(string leftDateTime, string rightDateTime, string @operator, bool expectedResult)
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithTwoContents(
            "A1", "sameHash", DateTime.Parse(leftDateTime, System.Globalization.CultureInfo.InvariantCulture),
            "B1", "sameHash", DateTime.Parse(rightDateTime, System.Globalization.CultureInfo.InvariantCulture));
    
        var filterText = $"A1.last-write-time{@operator}B1.last-write-time";
    
        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);
    
        // Assert
        result.Should().Be(expectedResult);
    }
    
    [TestCase("2024-05-01", "2024-05-01", "==", true)]
    [TestCase("2024-05-01", "2024-05-01", ">=", true)]
    [TestCase("2024-05-01", "2024-05-01", "<=", true)]
    [TestCase("2024-05-01", "2024-06-01", "==", false)]
    [TestCase("2024-05-01", "2024-06-01", "!=", true)]
    [TestCase("2024-05-01", "2024-06-01", "<=", true)]
    [TestCase("2024-05-01", "2024-06-01", "<", true)]
    [TestCase("2024-05-01", "2024-06-01", ">=", false)]
    [TestCase("2024-05-01", "2024-06-01", ">", false)]
    [TestCase("2024-06-01", "2024-05-01", ">=", true)]
    [TestCase("2024-06-01", "2024-05-01", ">", true)]
    [TestCase("2024-06-01", "2024-05-01", "<=", false)]
    [TestCase("2024-06-01", "2024-05-01", "<", false)]
    [TestCase("2024-05-01", "2024-06-01", "<", true)]
    [TestCase("2024-05-01", "2024-05-01", "<", false)]
    [TestCase("2024-06-01", "2024-05-01", ">", true)]
    [TestCase("2024-05-01", "2024-05-01", ">", false)]
    public void TestLastWriteTimeComparison_WithTarget_Simplified(string leftDateTime, string rightDateTime, string @operator, bool expectedResult)
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithTwoContents(
            "A1", "sameHash", DateTime.Parse(leftDateTime, System.Globalization.CultureInfo.InvariantCulture),
            "B1", "sameHash", DateTime.Parse(rightDateTime, System.Globalization.CultureInfo.InvariantCulture));
    
        var filterText = $"A1.last-write-time{@operator}B1._";
    
        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);
    
        // Assert
        result.Should().Be(expectedResult);
    }
    
    [TestCase("2024-05-01", "2024-05-01", "==", true)]
    [TestCase("2024-05-01", "2024-05-01", ">=", true)]
    [TestCase("2024-05-01", "2024-05-01", "<=", true)]
    [TestCase("2024-05-01", "2024-06-01", "==", false)]
    [TestCase("2024-05-01", "2024-06-01", "!=", true)]
    [TestCase("2024-05-01", "2024-06-01", "<=", true)]
    [TestCase("2024-05-01", "2024-06-01", "<", true)]
    [TestCase("2024-05-01", "2024-06-01", ">=", false)]
    [TestCase("2024-05-01", "2024-06-01", ">", false)]
    [TestCase("2024-06-01", "2024-05-01", ">=", true)]
    [TestCase("2024-06-01", "2024-05-01", ">", true)]
    [TestCase("2024-06-01", "2024-05-01", "<=", false)]
    [TestCase("2024-06-01", "2024-05-01", "<", false)]
    [TestCase("2024-05-01", "2024-06-01", "<", true)]
    [TestCase("2024-05-01", "2024-05-01", "<", false)]
    [TestCase("2024-06-01", "2024-05-01", ">", true)]
    [TestCase("2024-05-01", "2024-05-01", ">", false)]
    public void TestLastWriteTimeComparison_WithLiteral(string leftDateTime, string rightDateTime, string @operator, bool expectedResult)
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "hash", DateTime.Parse(leftDateTime, System.Globalization.CultureInfo.InvariantCulture), 24);
    
        var filterText = $"A1.last-write-time{@operator}{rightDateTime}";
    
        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);
    
        // Assert
        result.Should().Be(expectedResult);
    }
    
    [TestCase(0, "<", "now-7d", false)]
    [TestCase(-6, "<", "now-7d", false)]
    [TestCase(-8, "<", "now-7d", true)]
    [TestCase(0, ">", "now-1M", true)]
    [TestCase(0, ">=", "now-1y", true)]
    [TestCase(0, "<", "now-1m", false)]
    [TestCase(-1, "<", "now-1m", true)]
    [TestCase(0, ">", "now-1h", true)]
    [TestCase(-1, ">", "now-1h", false)]
    [TestCase(0, ">=", "now-1d", true)]
    [TestCase(-1, ">=", "now-1d", false)]
    [TestCase(-8, "<", "now-1w", true)]
    [TestCase(-6, "<", "now-1w", false)]
    [TestCase(-32, "<=", "now-1M", true)]
    [TestCase(-15, "<=", "now-1M", false)]
    [TestCase(-367, "<", "now-1y", true)]
    [TestCase(-180, "<", "now-1y", false)]
    public void TestLastWriteTimeComparison_WithDynamicExpression(int daysDiff, string @operator, string expression,  bool expectedResult)
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "hash", DateTime.UtcNow.AddDays(daysDiff), 24);

        var filterText = $"A1.last-write-time{@operator}{expression}";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().Be(expectedResult);
    }
}