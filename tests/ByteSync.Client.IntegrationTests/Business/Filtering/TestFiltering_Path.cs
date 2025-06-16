using FluentAssertions;

namespace ByteSync.Client.IntegrationTests.Business.Filtering;

public class TestFiltering_Path : BaseTestFiltering
{
    [SetUp]
    public void Setup()
    {
        SetupBase();
    }
    
    [Test]
    public void TestEquals_WhenNameNotMatchesWithColon_ShouldBeTrue()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", DateTime.Now, 50, "file1.txt");

        var filterText = "path:file1.txt";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void TestEquals_WhenNameMatchesWithColonAndWildCard_ShouldBeTrue()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", DateTime.Now, 50, "file1.txt");

        var filterText = "path:*file1.txt";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }
    
    [Theory]
    [TestCase("/file1.txt")]
    [TestCase("\\file1.txt")]
    public void TestEquals_WhenNameMatchesWithColonAndSlash_ShouldBeTrue(string searchText)
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", DateTime.Now, 50, "file1.txt");

        var filterText = "path:" + searchText;

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }
    
       [Test]
    public void TestEquals_WhenPathMatches_ShouldBeTrue()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", DateTime.Now, 50, "/folder/file1.txt");

        var filterText = "path:/folder/file1.txt";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void TestEquals_WhenPathDoesNotMatch_ShouldBeFalse()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", DateTime.Now, 50, "/folder/file2.txt");

        var filterText = "path:/folder/file1.txt";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void TestWildcardMatch_WhenPathMatches_ShouldBeTrue()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", DateTime.Now, 50, "/folder/file123.txt");

        var filterText = "path:/folder/file1*";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void TestWildcardMatch_WhenPathDoesNotMatch_ShouldBeFalse()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", DateTime.Now, 50, "/folder/file2.txt");

        var filterText = "path:/folder/file1*";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void TestRegexMatch_WhenPathMatchesPattern_ShouldBeTrue()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", DateTime.Now, 50, "/folder/file123.txt");

        var filterText = "path=~^/folder/file\\d+\\.txt$";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void TestRegexMatch_WhenPathDoesNotMatchPattern_ShouldBeFalse()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", DateTime.Now, 50, "/folder/file.txt");

        var filterText = "path=~^/folder/file\\d+\\.txt$";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeFalse();
    }
}