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
    public void TestSizeComparison_WithTarget_Simplified(long leftSize, long rightSize, string @operator, bool expectedResult)
    {
        // Arrange
        var now = DateTime.Now;
        var comparisonItem = PrepareComparisonWithTwoContents(
            "A1", "sameHash", now, leftSize,
            "B1", "sameHash", now, rightSize);
    
        var filterText = $"A1.size{@operator}B1._";
    
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
    
    [TestCase(105 * 1024, ">", false)]
    [TestCase(105 * 1024, "<", true)]
    [TestCase(80 * 1024, ">", false)]
    [TestCase(80 * 1024, "<", true)]
    [Test]
    public void TestSizeComparison_3(long leftSize, string @operator, bool expectedResult)
    {
        // Arrange
        var now = DateTime.Now;
        var comparisonItem = PrepareComparisonWithTwoContents(
            "A1", "sameHash", now, leftSize,
            "B1", "sameHash", now, 1);
    
        var filterText = $"A1.size{@operator}1MB";
    
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
    
    // Documentation examples tests
    
    [Test]
    public void TestEvaluate_ExactSize_1024()
    {
        // Arrange
        var filterText = "A1.size == 1024";
        
        // Test file with exact size
        var comparisonItem1 = PrepareComparisonWithOneContent("A1", "sameHash", DateTime.Now, 1024);
        
        // Test file with different size
        var comparisonItem2 = PrepareComparisonWithOneContent("A1", "sameHash", DateTime.Now, 1025);
        
        // Act
        var result1 = EvaluateFilterExpression(filterText, comparisonItem1);
        var result2 = EvaluateFilterExpression(filterText, comparisonItem2);
        
        // Assert
        result1.Should().BeTrue(); // Exact match should be true
        result2.Should().BeFalse(); // Different size should be false
    }
    
    [Test]
    public void TestEvaluate_NotZeroSize()
    {
        // Arrange
        var filterText = "A1.size != 0";
        
        // Test zero-size file
        var comparisonItem1 = PrepareComparisonWithOneContent("A1", "sameHash", DateTime.Now, 0);
        
        // Test non-zero size file
        var comparisonItem2 = PrepareComparisonWithOneContent("A1", "sameHash", DateTime.Now, 100);
        
        // Act
        var result1 = EvaluateFilterExpression(filterText, comparisonItem1);
        var result2 = EvaluateFilterExpression(filterText, comparisonItem2);
        
        // Assert
        result1.Should().BeFalse(); // Zero size should be excluded
        result2.Should().BeTrue(); // Non-zero size should match
    }
}