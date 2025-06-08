using FluentAssertions;

namespace ByteSync.Client.IntegrationTests.Business.Filtering;

public class TestFiltering_Size : BaseTestFiltering
{
    [SetUp]
    public void Setup()
    {
        SetupBase();
    }

    [TestCase(100, 100, "==", true)]
    [TestCase(100, 100, ">=", true)]
    [TestCase(100, 100, "<=", true)]
    [TestCase(100, 200, "==", false)]
    [TestCase(100, 200, "!=", true)]
    [TestCase(100, 200, "<=", true)]
    [TestCase(100, 200, "<", true)]
    [TestCase(100, 200, ">=", false)]
    [TestCase(100, 200, ">", false)]
    [TestCase(200, 100, ">=", true)]
    [TestCase(200, 100, ">", true)]
    [TestCase(200, 100, "<=", false)]
    [TestCase(200, 100, "<", false)]
    [TestCase(100, 101, "<", true)]
    [TestCase(100, 100, "<", false)]
    [TestCase(101, 100, ">", true)]
    [TestCase(100, 100, ">", false)]
    public void TestSizeComparison_WithTarget(long leftSize, long rightSize, string @operator, bool expectedResult)
    {
        // Arrange
        var now = DateTime.Now;
        var comparisonItem = PrepareComparisonWithTwoContents(
            "A1", "sameHash", now, leftSize,
            "B1", "sameHash", now, rightSize);
    
        var filterText = $"A1.size{@operator}B1.size";
    
        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);
    
        // Assert
        result.Should().Be(expectedResult);
    }
    
    [TestCase(105 * 1024, ">", true)]
    [TestCase(105 * 1024, "<", false)]
    [TestCase(80 * 1024, ">", false)]
    [TestCase(80 * 1024, "<", true)]
    [Test]
    public void TestSizeComparison_2(long leftSize, string @operator, bool expectedResult)
    {
        // Arrange
        var now = DateTime.Now;
        var comparisonItem = PrepareComparisonWithTwoContents(
            "A1", "sameHash", now, leftSize,
            "B1", "sameHash", now, 1);
    
        var filterText = $"A1.size{@operator}100kb";
    
        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);
    
        // Assert
        result.Should().Be(expectedResult);
    }
    
    [TestCase(100, 100, "==", true)]
    [TestCase(100, 100, ">=", true)]
    [TestCase(100, 100, "<=", true)]
    [TestCase(100, 200, "==", false)]
    [TestCase(100, 200, "!=", true)]
    [TestCase(100, 200, "<=", true)]
    [TestCase(100, 200, "<", true)]
    [TestCase(100, 200, ">=", false)]
    [TestCase(100, 200, ">", false)]
    [TestCase(200, 100, ">=", true)]
    [TestCase(200, 100, ">", true)]
    [TestCase(200, 100, "<=", false)]
    [TestCase(200, 100, "<", false)]
    [TestCase(100, 101, "<", true)]
    [TestCase(100, 100, "<", false)]
    [TestCase(101, 100, ">", true)]
    [TestCase(100, 100, ">", false)]
    public void TestSizeComparison_WithLiteral(long leftSize, long rightSize, string @operator, bool expectedResult)
    {
        // Arrange
        var now = DateTime.Now;
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", now, leftSize);
    
        var filterText = $"A1.size{@operator}{rightSize}b";
    
        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);
    
        // Assert
        result.Should().Be(expectedResult);
    }
}