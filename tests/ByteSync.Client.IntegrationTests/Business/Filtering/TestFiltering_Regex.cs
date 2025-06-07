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
    public void TestFiltering_NameRegex(string pattern, bool expectedResult)
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "hash", DateTime.Now, 50, "/sameFile.txt");

        var filterText = $"A1.name=~\"{pattern}\"";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Test]
    public void TestFiltering_NameRegex_InvalidPattern()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "hash", DateTime.Now, 50, "/sameFile.txt");

        var filterText = "A1.name=~\"[invalid\"";

        // Act
        bool result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeFalse();
    }
}