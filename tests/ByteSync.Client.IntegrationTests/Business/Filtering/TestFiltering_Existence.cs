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
    public void TestIsOn(string location, bool expectedResult)
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
}