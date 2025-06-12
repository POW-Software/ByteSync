using FluentAssertions;

namespace ByteSync.Client.IntegrationTests.Business.Filtering;

public class TestFiltering_Name : BaseTestFiltering
{
    [SetUp]
    public void Setup()
    {
        SetupBase();
    }
    
    // [Test]
    // public void TestOnly_WhenOnlyOnA1_ShouldBeTrue()
    // {
    //     // Arrange
    //     var now = DateTime.Now;
    //     var comparisonItem = PrepareComparisonWithOneContent(
    //         "A1", "sameHash", now, 50, "file1.txt");
    //
    //     var filterText = "name==file1.txt";
    //
    //     // Act
    //     var result = EvaluateFilterExpression(filterText, comparisonItem);
    //
    //     // Assert
    //     result.Should().BeTrue();
    // }
    
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
}