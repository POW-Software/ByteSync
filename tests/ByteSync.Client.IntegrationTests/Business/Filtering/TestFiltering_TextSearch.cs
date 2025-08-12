using Autofac;
using ByteSync.Business.Comparisons;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.Comparisons.Result;
using FluentAssertions;
using Moq;

namespace ByteSync.Client.IntegrationTests.Business.Filtering;

public class TestFiltering_TextSearch : BaseTestFiltering
{
    [SetUp]
    public void Setup()
    {
        SetupBase();
    }

    [Test]
    [TestCase("example", false)]
    [TestCase("ffile1.txt", false)]
    [TestCase("ile", true)]
    [TestCase("file1", true)]
    [TestCase("file1.txt", true)]
    [TestCase("FILE1.TXT", true)]
    public void Parse_SimpleTextSearch_ReturnsCorrectExpression(string filterText, bool expectedResult)
    {
        // Arrange
        var pathIdentity = new PathIdentity(FileSystemTypes.File, "/file1.txt", "file1.txt", "/file1.txt");
        var comparisonItem = new ComparisonItem(pathIdentity);
        
        var mockDataPartIndexer = Container.Resolve<Mock<IDataPartIndexer>>();
        mockDataPartIndexer.Setup(m => m.GetDataPart(It.IsAny<string>()))
            .Returns((DataPart)null);

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Test]
    [TestCase("test 2025", true)]
    [TestCase("2025 test", true)]
    [TestCase("test missing", false)]
    [TestCase("missing 2025", false)]
    public void Parse_MultiWordTextSearch_ReturnsCorrectExpression(string filterText, bool expectedResult)
    {
        // Arrange
        var pathIdentity = new PathIdentity(FileSystemTypes.File, "/test2025.txt", "test2025.txt", "/test2025.txt");
        var comparisonItem = new ComparisonItem(pathIdentity);
        
        var mockDataPartIndexer = Container.Resolve<Mock<IDataPartIndexer>>();
        mockDataPartIndexer.Setup(m => m.GetDataPart(It.IsAny<string>()))
            .Returns((DataPart)null);

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().Be(expectedResult);
    }
    
    // Documentation examples tests
    
    [Test]
    public void TestParse_Report2025_TextSearch()
    {
        // Arrange - "report 2025"
        var filterText = "report 2025";
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeTrue();
        parseResult.Expression.Should().NotBeNull();
    }
    
    [Test]
    public void TestEvaluate_Report2025_TextSearch()
    {
        // Arrange - "report 2025"
        var filterText = "report 2025";
        
        // Test file that contains both "report" and "2025"
        var pathIdentity1 = new PathIdentity(FileSystemTypes.File, "/report_2025.txt", "report_2025.txt", "/report_2025.txt");
        var comparisonItem1 = new ComparisonItem(pathIdentity1);
        
        // Test file that contains only "report"
        var pathIdentity2 = new PathIdentity(FileSystemTypes.File, "/report.txt", "report.txt", "/report.txt");
        var comparisonItem2 = new ComparisonItem(pathIdentity2);
        
        // Test file that contains only "2025"
        var pathIdentity3 = new PathIdentity(FileSystemTypes.File, "/data_2025.txt", "data_2025.txt", "/data_2025.txt");
        var comparisonItem3 = new ComparisonItem(pathIdentity3);
        
        // Test file that contains neither
        var pathIdentity4 = new PathIdentity(FileSystemTypes.File, "/data.txt", "data.txt", "/data.txt");
        var comparisonItem4 = new ComparisonItem(pathIdentity4);
        
        var mockDataPartIndexer = Container.Resolve<Mock<IDataPartIndexer>>();
        mockDataPartIndexer.Setup(m => m.GetDataPart(It.IsAny<string>()))
            .Returns((DataPart)null);

        // Act
        var result1 = EvaluateFilterExpression(filterText, comparisonItem1);
        var result2 = EvaluateFilterExpression(filterText, comparisonItem2);
        var result3 = EvaluateFilterExpression(filterText, comparisonItem3);
        var result4 = EvaluateFilterExpression(filterText, comparisonItem4);

        // Assert
        result1.Should().BeTrue(); // Contains both "report" and "2025"
        result2.Should().BeFalse(); // Contains only "report"
        result3.Should().BeFalse(); // Contains only "2025"
        result4.Should().BeFalse(); // Contains neither
    }
}
