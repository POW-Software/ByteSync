using FluentAssertions;

namespace ByteSync.Client.IntegrationTests.Business.Filtering;

public class TestFiltering_Only : BaseTestFiltering
{
    [SetUp]
    public void Setup()
    {
        SetupBase();
    }
    
    [Test]
    public void TestOnly_WhenOnlyOnA1_ShouldBeTrue()
    {
        // Arrange
        var now = DateTime.Now;
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", now, 50);
    
        var filterText = "only:A1";
    
        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);
    
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void TestOnly_WhenExistsInTwoInventories_ShouldBeFalse()
    {
        // Arrange
        var now = DateTime.Now;
        var comparisonItem = PrepareComparisonWithTwoContents(
            "A1", "sameHash", now,
            "B1", "sameHash", now);
    
        var filterText = "only:A";
    
        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);
    
        // Assert
        result.Should().BeFalse();
    }
    
    [Test]
    public void TestOnly_WhenExistsInDifferentInventory_ShouldBeFalse()
    {
        // Arrange
        var now = DateTime.Now;
        var comparisonItem = PrepareComparisonWithOneContent(
            "B1", "sameHash", now, 50);
    
        var filterText = "only:A";
    
        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);
    
        // Assert
        result.Should().BeFalse();
    }
    
    [Test]
    public void TestOnly_WithCombinedLogicalOperators()
    {
        // Arrange
        var now = DateTime.Now;
        var comparisonItem = PrepareComparisonWithTwoContents(
            "A1", "sameHash", now,
            "B1", "sameHash", now);
    
        // Test AND with two false conditions
        var filterText1 = "only:A AND only:B";
        var result1 = EvaluateFilterExpression(filterText1, comparisonItem);
        result1.Should().BeFalse();
        
        // Test OR with at least one true condition
        var singleAComparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", now, 50);
        var filterText2 = "only:A OR only:B";
        var result2 = EvaluateFilterExpression(filterText2, singleAComparisonItem);
        result2.Should().BeTrue();
        
        // Test NOT with inverted condition
        var filterText3 = "NOT only:A";
        var result3 = EvaluateFilterExpression(filterText3, singleAComparisonItem);
        result3.Should().BeFalse();
        
        var result4 = EvaluateFilterExpression(filterText3, comparisonItem);
        result4.Should().BeTrue();
    }
    
    [Test]
    public void TestOnly_CaseInsensitivity()
    {
        // Arrange
        var now = DateTime.Now;
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", now, 50);
    
        // Test with lowercase
        var filterText1 = "only:a";
        var result1 = EvaluateFilterExpression(filterText1, comparisonItem);
        result1.Should().BeTrue();
        
        // Test with uppercase
        var filterText2 = "ONLY:A";
        var result2 = EvaluateFilterExpression(filterText2, comparisonItem);
        result2.Should().BeTrue();
    }
    
    [Test]
    public void TestOnly_WithDifferentSyntax()
    {
        // Arrange
        var now = DateTime.Now;
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", now, 50);
    
        // Test with the :only<LETTER> syntax
        var filterText = ":onlyA";
        
        Action act = () => EvaluateFilterExpression(filterText, comparisonItem);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Parse error:*");
    }
    
    [Test]
    public void TestOnly_ComplexConditions()
    {
        // Arrange
        var now = DateTime.Now;
        var comparisonItemA = PrepareComparisonWithOneContent(
            "A1", "sameHash", now, 50);
        
        var comparisonItemAB = PrepareComparisonWithTwoContents(
            "A1", "sameHash", now,
            "B1", "sameHash", now);
        
        // Test complex condition: (only:A AND file1) OR (only:B)
        var filterText = "(only:A AND file1) OR only:B";
        
        var resultA = EvaluateFilterExpression(filterText, comparisonItemA);
        resultA.Should().BeTrue(); // True because 'only:A AND file1' is true
        
        var resultAB = EvaluateFilterExpression(filterText, comparisonItemAB);
        resultAB.Should().BeFalse(); // False because neither condition is true
    }
}