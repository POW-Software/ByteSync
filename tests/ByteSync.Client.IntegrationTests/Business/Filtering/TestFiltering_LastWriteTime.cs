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
}