using FluentAssertions;

namespace ByteSync.Client.IntegrationTests.Business.Filtering;

public class TestFiltering_Existence : BaseTestFiltering
{
    [SetUp]
    public void Setup()
    {
        SetupBase();
    }
    
    [TestCase("A1", true)]
    [TestCase("B1", false)]
    [Test]
    public void TestOn(string location, bool expectedResult)
    {
        // Arrange
        var now = DateTime.Now;
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", now, 50);
    
        var filterText = $"on:{location}";
    
        // Act
        var expression = _filterParser.Parse(filterText);
        var evaluator = _evaluatorFactory.GetEvaluator(expression);
        bool result = evaluator.Evaluate(expression, comparisonItem);
    
        // Assert
        result.Should().Be(expectedResult);
    }
    
    [TestCase("A1", false)]
    [TestCase("B1", true)]
    [Test]
    public void TestNotOn_WhenA1(string location, bool expectedResult)
    {
        // Arrange
        var now = DateTime.Now;
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", now, 50);
    
        var filterText = $"NOT on:{location}";
    
        // Act
        var expression = _filterParser.Parse(filterText);
        var evaluator = _evaluatorFactory.GetEvaluator(expression);
        bool result = evaluator.Evaluate(expression, comparisonItem);
    
        // Assert
        result.Should().Be(expectedResult);
    }
    
    [TestCase("on:A1 AND on:B1", true)]
    [TestCase("on:A1 AND on:C1", false)]
    [TestCase("on:A1 AND NOT on:C1", true)]
    [TestCase("on:A1 AND on:B1 AND NOT on:C1", true)]
    [TestCase("on:A1 AND on:B1 AND on:C1", false)]
    [Test]
    public void TestNotOn_WhenA1B1(string filterText, bool expectedResult)
    {
        // Arrange
        var now = DateTime.Now;
        var comparisonItem = PrepareComparisonWithTwoContents(
            "A1", "sameHash", now, 50,
            "B1", "sameHash2", now, 51);
    
        // Act
        var expression = _filterParser.Parse(filterText);
        var evaluator = _evaluatorFactory.GetEvaluator(expression);
        bool result = evaluator.Evaluate(expression, comparisonItem);
    
        // Assert
        result.Should().Be(expectedResult);
    }
}