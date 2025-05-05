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
        var expression = _filterParser.Parse(filterText);
        var evaluator = _evaluatorFactory.GetEvaluator(expression);
        bool result = evaluator.Evaluate(expression, comparisonItem);

        result.Should().Be(expectedResult);

        // // Assert
        // Assert.IsInstanceOf<AndExpression>(expression);
        // Assert.IsTrue(((AndExpression)expression).Expressions.Any(e => e is TextSearchExpression));
    }
}
