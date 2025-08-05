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
    public void TestEquals_WhenNameMatchesWithColon_ShouldBeTrue()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", DateTime.Now, 50, "file1.txt");

        var filterText = "name:file1.txt";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
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
    public void TestEquals_WhenNameMatches_ShouldBeFalse()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", DateTime.Now, 50, "file1.txt");

        var filterText = "name!=file1.txt";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeFalse();
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
    public void TestWildcardMatch_WhenNameMatchesFile1StarWithColon_ShouldBeTrue()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", DateTime.Now, 50, "file123.txt");

        var filterText = "name:file1*";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void TestWildcardMatch_WhenNameMatchesFile1Star_ShouldBeTrue()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", DateTime.Now, 50, "file123.txt");

        var filterText = "name==file1*";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void TestWildcardMatch_WhenNameDoesNotMatchFile1Star_ShouldBeFalse()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", DateTime.Now, 50, "file2.txt");

        var filterText = "name==file1*";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void TestWildcardMatch_WhenNameMatchesTxtExtension_ShouldBeTrue()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", DateTime.Now, 50, "file123.txt");

        var filterText = "name==*.txt";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void TestWildcardMatch_WhenNameDoesNotMatchTxtExtension_ShouldBeFalse()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", DateTime.Now, 50, "file123.doc");

        var filterText = "name==*.txt";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeFalse();
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
    
    [Test]
    public void TestEquals_WhenNameMatchesWithQuotes_ShouldBeTrue()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", DateTime.Now, 50, "file1.txt");

        var filterText = "name==\"file1.txt\"";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void TestEquals_WhenNameMatchesWithQuotesWithSpaces_ShouldBeTrue()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", DateTime.Now, 50, "file 1.txt");

        var filterText = "name==\"file 1.txt\"";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [TestCase("file2.txt", "\"file1.txt\"")]
    [TestCase("file1.txt", "\"file 1.txt\"")]
    public void TestEquals_WhenNameDoesNotMatchWithQuotes_ShouldBeFalse(string fileName, string filterValue)
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent(
            "A1", "sameHash", DateTime.Now, 50, fileName);

        var filterText = $"name=={filterValue}";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeFalse();
    }
    
    // Documentation examples tests
    
    [Test]
    public void TestParse_ExactNameMatch_README()
    {
        // Arrange - "name:README.md"
        var filterText = "name:README.md";
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeTrue();
        parseResult.Expression.Should().NotBeNull();
    }
    
    [Test]
    public void TestEvaluate_ExactNameMatch_README()
    {
        // Arrange - "name:README.md"
        var filterText = "name:README.md";
        var comparisonItem = PrepareComparisonWithOneContent("A1", "sameHash", DateTime.Now, 50, "README.md");
        
        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void TestParse_ExactNameEquals_File1()
    {
        // Arrange - "name==\"file1.txt\""
        var filterText = "name==\"file1.txt\"";
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeTrue();
        parseResult.Expression.Should().NotBeNull();
    }
    
    [Test]
    public void TestEvaluate_ExactNameEquals_File1()
    {
        // Arrange - "name==\"file1.txt\""
        var filterText = "name==\"file1.txt\"";
        var comparisonItem = PrepareComparisonWithOneContent("A1", "sameHash", DateTime.Now, 50, "file1.txt");
        
        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void TestParse_ExcludeName_TempLog()
    {
        // Arrange - "name!=\"temp.log\""
        var filterText = "name!=\"temp.log\"";
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeTrue();
        parseResult.Expression.Should().NotBeNull();
    }
    
    [Test]
    public void TestEvaluate_ExcludeName_TempLog()
    {
        // Arrange - "name!=\"temp.log\""
        var filterText = "name!=\"temp.log\"";
        
        // Test with temp.log (should not match)
        var comparisonItem1 = PrepareComparisonWithOneContent("A1", "sameHash", DateTime.Now, 50, "temp.log");
        
        // Test with other file (should match)
        var comparisonItem2 = PrepareComparisonWithOneContent("A1", "sameHash", DateTime.Now, 50, "data.log");
        
        // Act
        var result1 = EvaluateFilterExpression(filterText, comparisonItem1);
        var result2 = EvaluateFilterExpression(filterText, comparisonItem2);
        
        // Assert
        result1.Should().BeFalse(); // temp.log should be excluded
        result2.Should().BeTrue(); // data.log should match
    }
    
    [Test]
    public void TestParse_NameStartsWith_File1()
    {
        // Arrange - "name:file1*"
        var filterText = "name:file1*";
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeTrue();
        parseResult.Expression.Should().NotBeNull();
    }
    
    [Test]
    public void TestEvaluate_NameStartsWith_File1()
    {
        // Arrange - "name:file1*"
        var filterText = "name:file1*";
        
        // Test files that start with file1
        var comparisonItem1 = PrepareComparisonWithOneContent("A1", "sameHash", DateTime.Now, 50, "file1.txt");
        var comparisonItem2 = PrepareComparisonWithOneContent("A1", "sameHash", DateTime.Now, 50, "file1_backup.txt");
        var comparisonItem3 = PrepareComparisonWithOneContent("A1", "sameHash", DateTime.Now, 50, "file2.txt");
        
        // Act
        var result1 = EvaluateFilterExpression(filterText, comparisonItem1);
        var result2 = EvaluateFilterExpression(filterText, comparisonItem2);
        var result3 = EvaluateFilterExpression(filterText, comparisonItem3);
        
        // Assert
        result1.Should().BeTrue(); // file1.txt matches
        result2.Should().BeTrue(); // file1_backup.txt matches
        result3.Should().BeFalse(); // file2.txt doesn't match
    }
    
    [Test]
    public void TestParse_NameEndsWith_Log()
    {
        // Arrange - "name==*.log"
        var filterText = "name==*.log";
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeTrue();
        parseResult.Expression.Should().NotBeNull();
    }
    
    [Test]
    public void TestEvaluate_NameEndsWith_Log()
    {
        // Arrange - "name==*.log"
        var filterText = "name==*.log";
        
        // Test files that end with .log
        var comparisonItem1 = PrepareComparisonWithOneContent("A1", "sameHash", DateTime.Now, 50, "app.log");
        var comparisonItem2 = PrepareComparisonWithOneContent("A1", "sameHash", DateTime.Now, 50, "error.log");
        var comparisonItem3 = PrepareComparisonWithOneContent("A1", "sameHash", DateTime.Now, 50, "data.txt");
        
        // Act
        var result1 = EvaluateFilterExpression(filterText, comparisonItem1);
        var result2 = EvaluateFilterExpression(filterText, comparisonItem2);
        var result3 = EvaluateFilterExpression(filterText, comparisonItem3);
        
        // Assert
        result1.Should().BeTrue(); // app.log matches
        result2.Should().BeTrue(); // error.log matches
        result3.Should().BeFalse(); // data.txt doesn't match
    }
    
    [Test]
    public void TestParse_RegexPattern_LogTxtFiles()
    {
        // Arrange - "name=~\"^log_.*\\\\.txt$\""
        var filterText = "name=~\"^log_.*\\\\.txt$\"";
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeTrue();
        parseResult.Expression.Should().NotBeNull();
    }
    
    [Test]
    public void TestEvaluate_RegexPattern_LogTxtFiles()
    {
        // Arrange - "name=~\"^log_.*\\\\.txt$\""
        var filterText = "name=~\"^log_.*\\\\.txt$\"";
        
        // Test files matching the regex pattern
        var comparisonItem1 = PrepareComparisonWithOneContent("A1", "sameHash", DateTime.Now, 50, "log_error.txt");
        var comparisonItem2 = PrepareComparisonWithOneContent("A1", "sameHash", DateTime.Now, 50, "log_2025.txt");
        var comparisonItem3 = PrepareComparisonWithOneContent("A1", "sameHash", DateTime.Now, 50, "error_log.txt");
        var comparisonItem4 = PrepareComparisonWithOneContent("A1", "sameHash", DateTime.Now, 50, "log_data.log");
        
        // Act
        var result1 = EvaluateFilterExpression(filterText, comparisonItem1);
        var result2 = EvaluateFilterExpression(filterText, comparisonItem2);
        var result3 = EvaluateFilterExpression(filterText, comparisonItem3);
        var result4 = EvaluateFilterExpression(filterText, comparisonItem4);
        
        // Assert
        result1.Should().BeTrue(); // log_error.txt matches
        result2.Should().BeTrue(); // log_2025.txt matches
        result3.Should().BeFalse(); // error_log.txt doesn't start with log_
        result4.Should().BeFalse(); // log_data.log doesn't end with .txt
    }
    
    [Test]
    public void TestParse_RegexPattern_ReportPdfFiles()
    {
        // Arrange - "name =~ \"^report.*\\\\.pdf$\""
        var filterText = "name =~ \"^report.*\\\\.pdf$\"";
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeTrue();
        parseResult.Expression.Should().NotBeNull();
    }
    
    [Test]
    public void TestEvaluate_RegexPattern_ReportPdfFiles()
    {
        // Arrange - "name =~ \"^report.*\\\\.pdf$\""
        var filterText = "name =~ \"^report.*\\\\.pdf$\"";
        
        // Test files matching the regex pattern
        var comparisonItem1 = PrepareComparisonWithOneContent("A1", "sameHash", DateTime.Now, 50, "report.pdf");
        var comparisonItem2 = PrepareComparisonWithOneContent("A1", "sameHash", DateTime.Now, 50, "report_2025.pdf");
        var comparisonItem3 = PrepareComparisonWithOneContent("A1", "sameHash", DateTime.Now, 50, "final_report.pdf");
        var comparisonItem4 = PrepareComparisonWithOneContent("A1", "sameHash", DateTime.Now, 50, "report.txt");
        
        // Act
        var result1 = EvaluateFilterExpression(filterText, comparisonItem1);
        var result2 = EvaluateFilterExpression(filterText, comparisonItem2);
        var result3 = EvaluateFilterExpression(filterText, comparisonItem3);
        var result4 = EvaluateFilterExpression(filterText, comparisonItem4);
        
        // Assert
        result1.Should().BeTrue(); // report.pdf matches
        result2.Should().BeTrue(); // report_2025.pdf matches
        result3.Should().BeFalse(); // final_report.pdf doesn't start with report
        result4.Should().BeFalse(); // report.txt doesn't end with .pdf
    }
    
    [Test]
    public void TestParse_RegexPattern_LogExtensions()
    {
        // Arrange - "name=~\"\\\\.log$\""
        var filterText = "name=~\"\\\\.log$\"";
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeTrue();
        parseResult.Expression.Should().NotBeNull();
    }
    
    [Test]
    public void TestEvaluate_RegexPattern_LogExtensions()
    {
        // Arrange - "name=~\"\\\\.log$\""
        var filterText = "name=~\"\\\\.log$\"";
        
        // Test files ending with .log
        var comparisonItem1 = PrepareComparisonWithOneContent("A1", "sameHash", DateTime.Now, 50, "application.log");
        var comparisonItem2 = PrepareComparisonWithOneContent("A1", "sameHash", DateTime.Now, 50, "error.log");
        var comparisonItem3 = PrepareComparisonWithOneContent("A1", "sameHash", DateTime.Now, 50, "log.txt");
        var comparisonItem4 = PrepareComparisonWithOneContent("A1", "sameHash", DateTime.Now, 50, "data.log.backup");
        
        // Act
        var result1 = EvaluateFilterExpression(filterText, comparisonItem1);
        var result2 = EvaluateFilterExpression(filterText, comparisonItem2);
        var result3 = EvaluateFilterExpression(filterText, comparisonItem3);
        var result4 = EvaluateFilterExpression(filterText, comparisonItem4);
        
        // Assert
        result1.Should().BeTrue(); // application.log matches
        result2.Should().BeTrue(); // error.log matches
        result3.Should().BeFalse(); // log.txt doesn't end with .log
        result4.Should().BeFalse(); // data.log.backup doesn't end with .log
    }
}