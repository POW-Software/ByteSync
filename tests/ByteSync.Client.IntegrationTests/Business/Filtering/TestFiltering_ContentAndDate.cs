using Autofac;
using ByteSync.Business;
using ByteSync.Business.Comparisons;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.FileSystems;
using ByteSync.Models.Inventories;
using FluentAssertions;
using Moq;

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
        
        var pathIdentity = new PathIdentity(FileSystemTypes.File, "/file1.txt", "file1.txt", "/file1.txt");
        var comparisonItem = new ComparisonItem(pathIdentity);
        
        var inventoryA = new Inventory();
        inventoryA.InventoryId = "Id_A";
        var inventoryPartA1 = new InventoryPart(inventoryA, "/testRootA1", FileSystemTypes.Directory);
        var fileDescriptionA1 = new FileDescription {
            InventoryPart = inventoryPartA1,
            LastWriteTimeUtc = leftDateTime,
            Size = 100,
            FingerprintMode = FingerprintModes.Sha256,
            SignatureGuid = null
        };
        
        var inventoryB = new Inventory();
        inventoryB.InventoryId = "Id_B";
        var inventoryPartB1 = new InventoryPart(inventoryB, "/testRootB1", FileSystemTypes.Directory);
        var fileDescriptionB1 = new FileDescription {
            InventoryPart = inventoryPartB1,
            LastWriteTimeUtc = rightDateTime,
            Size = 100,
            FingerprintMode = FingerprintModes.Sha256,
            SignatureGuid = null
        };
        
        var contentIdentityCoreA1 = new ContentIdentityCore();
        contentIdentityCoreA1.SignatureHash = leftHash;
        contentIdentityCoreA1.Size = 21;
        var contentIdentityA1 = new ContentIdentity(contentIdentityCoreA1);
        comparisonItem.AddContentIdentity(contentIdentityA1);
        contentIdentityA1.Add(fileDescriptionA1);
        if (leftHash == rightHash)
        {
            contentIdentityA1.Add(fileDescriptionB1);
        }
        else
        {
            var contentIdentityCoreB1 = new ContentIdentityCore();
            contentIdentityCoreB1.SignatureHash = rightHash;
            contentIdentityCoreB1.Size = 23;
            var contentIdentityB1 = new ContentIdentity(contentIdentityCoreB1);
            comparisonItem.AddContentIdentity(contentIdentityB1);
            contentIdentityB1.Add(fileDescriptionB1);
        }


        var mockDataPartIndexer = Container.Resolve<Mock<IDataPartIndexer>>();
        var dataPartA1 = new DataPart("A1", inventoryPartA1);
        mockDataPartIndexer.Setup(m => m.GetDataPart("A1"))
            .Returns(dataPartA1);
        var dataPartA2 = new DataPart("B1", inventoryPartB1);
        mockDataPartIndexer.Setup(m => m.GetDataPart("B1"))
            .Returns(dataPartA2);

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