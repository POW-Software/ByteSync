using FluentAssertions;

namespace ByteSync.Client.IntegrationTests.Business.Filtering;

public class TestFiltering_Regex : BaseTestFiltering
{
    [SetUp]
    public void Setup()
    {
        SetupBase();
    }

    [TestCase("^same.*$", true)]
    [TestCase("^diff.*$", false)]
    public void TestFiltering_ContentRegex(string pattern, bool expectedResult)
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", DateTime.Now, 50);

        var filterText = $"A1.content=~\"{pattern}\"";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Test]
    public void TestFiltering_ContentRegex_InvalidPattern()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "hash", DateTime.Now, 50);

        var filterText = "A1.content=~\"[invalid"";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeFalse();
    }
}

