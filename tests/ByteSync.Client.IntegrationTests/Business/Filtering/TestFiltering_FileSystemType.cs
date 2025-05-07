using FluentAssertions;

namespace ByteSync.Client.IntegrationTests.Business.Filtering;

public class TestFiltering_FileSystemType : BaseTestFiltering
{
    [SetUp]
    public void Setup()
    {
        SetupBase();
    }

    [Test]
    public void TestFiltering_IsFile_WhenIsFile_ShouldBeTrue()
    {
        // Arrange
        var now = DateTime.Now;
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "fileHash", now, 50);

        var filterText = "is:file";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void TestFiltering_IsFile_WhenIsDirectory_ShouldBeFalse()
    {
        // Arrange
        var now = DateTime.Now;
        var comparisonItem = PrepareComparisonWithOneDirectory("A1");

        var filterText = "is:file";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void TestFiltering_IsDir_WhenIsDirectory_ShouldBeTrue()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneDirectory("A1");

        var filterText = "is:dir";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void TestFiltering_IsDirectory_WhenIsDirectory_ShouldBeTrue()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneDirectory("A1");

        var filterText = "is:directory";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void TestFiltering_IsDir_WhenIsFile_ShouldBeFalse()
    {
        // Arrange
        var now = DateTime.Now;
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "fileHash", now, 50);

        var filterText = "is:dir";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void TestFiltering_CaseInsensitivity()
    {
        // Arrange
        var now = DateTime.Now;
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "fileHash", now, 50);

        // Test with mixed case
        var filterText1 = "Is:FiLe";
        var result1 = EvaluateFilterExpression(filterText1, comparisonItem);
        result1.Should().BeTrue();

        // Test with uppercase
        var filterText2 = "IS:FILE";
        var result2 = EvaluateFilterExpression(filterText2, comparisonItem);
        result2.Should().BeTrue();
    }
    
    [Test]
    public void TestFiltering_File_WithNotOperator()
    {
        // Arrange
        var now = DateTime.Now;
        var item = PrepareComparisonWithOneContent(
            "A1", "fileHash", now, 50);

        // Test NOT operator with file
        var filterText1 = "NOT is:file";
        
        var resultFile1 = EvaluateFilterExpression(filterText1, item);
        resultFile1.Should().BeFalse();

        // Test NOT operator with directory
        var filterText2 = "NOT is:dir";
        
        var resultFile2 = EvaluateFilterExpression(filterText2, item);
        resultFile2.Should().BeTrue();
    }
    
    [Test]
    public void TestFiltering_Directory_WithNotOperator()
    {
        // Arrange
        var item = PrepareComparisonWithOneDirectory("A1");

        // Test NOT operator with file
        var filterText1 = "NOT is:file";
        
        var result1 = EvaluateFilterExpression(filterText1, item);
        result1.Should().BeTrue();

        // Test NOT operator with directory
        var filterText2 = "NOT is:dir";
        
        var result2 = EvaluateFilterExpression(filterText2, item);
        result2.Should().BeFalse();
    }
}