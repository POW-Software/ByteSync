using FluentAssertions;

namespace ByteSync.Client.IntegrationTests.Business.Filtering;

public class TestFiltering_ContentAndDate : BaseTestFiltering
{
    [SetUp]
    public void Setup()
    {
        SetupBase();
    }

    [TestCase("sameHash", "2023-10-01", "sameHash", "2023-10-01", "==", true)]
    [TestCase("sameHash", "2023-10-01", "sameHash", "2023-10-01", "<>", false)]
    [TestCase("sameHash", "2023-10-01", "sameHash", "2023-10-01", "!=", false)]
    [TestCase("hashLeft", "2023-10-01", "hashRight", "2023-10-01", "==", false)]
    [TestCase("hashLeft", "2023-10-01", "hashRight", "2023-10-01", "<>", true)]
    [TestCase("hashLeft", "2023-10-01", "hashRight", "2023-10-01", "!=", true)]
    [TestCase("sameHash", "2023-10-01", "sameHash", "2023-10-02", "==", false)]
    [TestCase("sameHash", "2023-10-01", "sameHash", "2023-10-02", "<>", true)]
    [TestCase("sameHash", "2023-10-01", "sameHash", "2023-10-02", "!=", true)]
    public void Test_ContentAndDate(string leftHash, string leftDateTimeStr, string rightHash, string rightDateTimeStr,
        string @operator, bool expectedResult)
    {
        // Arrange
        var filterText = $"A1.contentanddate{@operator}B1.contentanddate";
        
        DateTime leftDateTime = DateTime.Parse(leftDateTimeStr, System.Globalization.CultureInfo.InvariantCulture);
        DateTime rightDateTime = DateTime.Parse(rightDateTimeStr, System.Globalization.CultureInfo.InvariantCulture);
        
        var comparisonItem = PrepareComparisonWithTwoContents(
            "A1", leftHash, leftDateTime,
            "B1", rightHash, rightDateTime);

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().Be(expectedResult);
    }
}