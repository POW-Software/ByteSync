using FluentAssertions;

namespace ByteSync.Client.IntegrationTests.Business.Filtering;

public class TestFiltering_Name : BaseTestFiltering
{
    [SetUp]
    public void Setup()
    {
        SetupBase();
    }
    
    [Test]
    public void TestEquals_WhenNameMatches_ShouldBeTrue()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", DateTime.Now, 50, "file1.txt");

        var filterText = "name==file1.txt";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void TestEquals_WhenNameDoesNotMatch_ShouldBeFalse()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", DateTime.Now, 50, "file2.txt");

        var filterText = "name==file1.txt";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void TestNotEquals_WhenNameDoesNotMatch_ShouldBeTrue()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", DateTime.Now, 50, "file2.txt");

        var filterText = "name!=file1.txt";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void TestRegexMatch_WhenNameMatchesPattern_ShouldBeTrue()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", DateTime.Now, 50, "file123.txt");

        var filterText = "name=~file\\d+.txt";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void TestRegexMatch_WhenNameDoesNotMatchPattern_ShouldBeFalse()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", DateTime.Now, 50, "file.txt");

        var filterText = "name=~file\\d+.txt";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeFalse();
    }
    
    [Test]
    public void TestRegexMatch_WithComplexPattern_ShouldBeTrue()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", DateTime.Now, 50, "file_2023_v1.txt");

        var filterText = "name=~^file_\\d{4}_v\\d+\\.txt$";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void TestRegexMatch_WithComplexPattern_ShouldBeFalse()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", DateTime.Now, 50, "file_v1.txt");

        var filterText = "name=~^file_\\d{4}_v\\d+\\.txt$";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeFalse();
    }
    
    
    [Test]
    public void TestRegexMatch_WithSquareBrackets_ShouldBeTrue()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", DateTime.Now, 50, "file[2023]_v1.txt");

        var filterText = "name=~^file\\[\\d{4}\\]_v\\d+\\.txt$";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void TestRegexMatch_WithSquareBrackets_ShouldBeFalse()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", DateTime.Now, 50, "file2023_v1.txt");

        var filterText = "name=~^file\\[\\d{4}\\]_v\\d+\\.txt$";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeFalse();
    }
}