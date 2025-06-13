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

public class TestFiltering_Contents : BaseTestFiltering
{
    [SetUp]
    public void Setup()
    {
        SetupBase();
    }

    [Test]
    public void Test_Contents()
    {
        // Arrange
        var filterText = "A1.contents==B1.contents";
        
        var pathIdentity = new PathIdentity(FileSystemTypes.File, "/file1.txt", "file1.txt", "/file1.txt");
        var comparisonItem = new ComparisonItem(pathIdentity);
        
        var lastWriteTime1 = new DateTime(2023, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var inventoryA = new Inventory();
        inventoryA.InventoryId = "Id_A";
        var inventoryPartA1 = new InventoryPart(inventoryA, "/testRootA1", FileSystemTypes.Directory);
        var fileDescriptionA1 = new FileDescription {
            InventoryPart = inventoryPartA1,
            LastWriteTimeUtc = lastWriteTime1,
            Size = 100,
            FingerprintMode = FingerprintModes.Sha256,
            SignatureGuid = null,
            Sha256 = "sameHash"
        };
        
        var inventoryB = new Inventory();
        inventoryB.InventoryId = "Id_B";
        var inventoryPartB1 = new InventoryPart(inventoryB, "/testRootB1", FileSystemTypes.Directory);
        var fileDescriptionB1 = new FileDescription {
            InventoryPart = inventoryPartB1,
            LastWriteTimeUtc = lastWriteTime1,
            Size = 100,
            FingerprintMode = FingerprintModes.Sha256,
            SignatureGuid = null,
            Sha256 = "sameHash"
        };
        
        var contentIdentityCore = new ContentIdentityCore();
        contentIdentityCore.SignatureHash = "TestHash";
        contentIdentityCore.Size = 21;
        var contentIdentity = new ContentIdentity(contentIdentityCore);
        comparisonItem.AddContentIdentity(contentIdentity);
        contentIdentity.Add(fileDescriptionA1);
        contentIdentity.Add(fileDescriptionB1);

        var mockDataPartIndexer = Container.Resolve<Mock<IDataPartIndexer>>();
        var dataPartA1 = new DataPart("A1", inventoryPartA1);
        mockDataPartIndexer.Setup(m => m.GetDataPart("A1"))
            .Returns(dataPartA1);
        var dataPartA2 = new DataPart("B1", inventoryPartB1);
        mockDataPartIndexer.Setup(m => m.GetDataPart("B1"))
            .Returns(dataPartA2);

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }
    
    [TestCase("sameHash", "2023-10-01", "sameHash", "2023-10-01", "==", true)]
    [TestCase("sameHash", "2023-10-01", "sameHash", "2023-10-01", "<>", false)]
    [TestCase("sameHash", "2023-10-01", "sameHash", "2023-10-01", "!=", false)]
    [TestCase("hashLeft", "2023-10-01", "hashRight", "2023-10-01", "==", false)]
    [TestCase("hashLeft", "2023-10-01", "hashRight", "2023-10-01", "<>", true)]
    [TestCase("hashLeft", "2023-10-01", "hashRight", "2023-10-01", "!=", true)]
    [TestCase("sameHash", "2023-10-01", "sameHash", "2023-10-02", "==", true)]
    [TestCase("sameHash", "2023-10-01", "sameHash", "2023-10-02", "<>", false)]
    [TestCase("sameHash", "2023-10-01", "sameHash", "2023-10-02", "!=", false)]
    public void Test_Contents_Simplified(string leftHash, string leftDateTimeStr, string rightHash, string rightDateTimeStr,
        string @operator, bool expectedResult)
    {
        // Arrange
        var filterText = $"A1.contents{@operator}B1._";
        
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