using System.IO;
using System.Linq;
using ByteSync.Business.Inventories;
using ByteSync.Business.PathItems;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Helpers;
using ByteSync.Services.Comparisons;
using ByteSync.Services.Inventories;
using ByteSync.Tests.TestUtilities.Helpers;
using ByteSync.TestsCommon;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace ByteSync.Tests.Controls.Comparisons;

[TestFixture]
public class TestIdentityBuilder : AbstractTester
{
    [Test]
    [TestCase("/file1.txt")]
    [TestCase("/subDir1/subDir2/subDir3/file1.txt")]
    public void Test_FileDescriptionInDirectory(string fileRelativePath)
    {
        CreateTestDirectory();
        var dataA = CreateSubTestDirectory("dataA");
        var file1 = CreateFileInDirectory(dataA, fileRelativePath, "file1Content");
            
        var inventoryBuilder = GetInventoryBuilder(dataA);

        var fileDescription = IdentityBuilder.BuildFileDescription(inventoryBuilder.Inventory!.InventoryParts.Single(), file1);
            
        ClassicAssert.AreEqual("file1Content".Length, fileDescription.Size);
        ClassicAssert.IsNull(fileDescription.FingerprintMode);
        ClassicAssert.IsNull(fileDescription.SignatureGuid);
        ClassicAssert.IsNull(null, fileDescription.Sha256);
        ClassicAssert.AreEqual(null, fileDescription.AnalysisErrorDescription);
        ClassicAssert.AreEqual(null, fileDescription.AnalysisErrorType);
        ClassicAssert.AreEqual(FileSystemTypes.File, fileDescription.FileSystemType);
        ClassicAssert.AreEqual(file1.LastWriteTimeUtc, fileDescription.LastWriteTimeUtc); // todo CreationTimeUtc
        ClassicAssert.AreEqual(inventoryBuilder.Inventory, fileDescription.Inventory);
        ClassicAssert.AreEqual(inventoryBuilder.Inventory.InventoryParts.Single(), fileDescription.InventoryPart);
        ClassicAssert.AreEqual("file1.txt", fileDescription.Name);
        // ClassicAssert.AreEqual("/file1.txt", fileDescription.RelativePath);
        ClassicAssert.AreEqual(fileRelativePath, fileDescription.RelativePath);
    }
        
    [Test]
    public void Test_FileDescriptionInFile()
    {
        CreateTestDirectory();
        var dataA = CreateSubTestDirectory("dataA");
        var file1 = CreateFileInDirectory(dataA, "file1.txt", "file1Content");
            
        var inventoryBuilder = GetInventoryBuilder(file1);

        var fileDescription = IdentityBuilder.BuildFileDescription(inventoryBuilder.Inventory!.InventoryParts.Single(), file1);
            
        ClassicAssert.AreEqual("file1Content".Length, fileDescription.Size);
        ClassicAssert.IsNull(fileDescription.FingerprintMode);
        ClassicAssert.IsNull(fileDescription.SignatureGuid);
        ClassicAssert.IsNull(null, fileDescription.Sha256);
        ClassicAssert.AreEqual(null, fileDescription.AnalysisErrorDescription);
        ClassicAssert.AreEqual(null, fileDescription.AnalysisErrorType);
        ClassicAssert.AreEqual(FileSystemTypes.File, fileDescription.FileSystemType);
        ClassicAssert.AreEqual(file1.LastWriteTimeUtc, fileDescription.LastWriteTimeUtc);
        ClassicAssert.AreEqual(inventoryBuilder.Inventory, fileDescription.Inventory);
        ClassicAssert.AreEqual(inventoryBuilder.Inventory.InventoryParts.Single(), fileDescription.InventoryPart);
        ClassicAssert.AreEqual("file1.txt", fileDescription.Name);
        // ClassicAssert.AreEqual("/file1.txt", fileDescription.RelativePath);
        ClassicAssert.AreEqual("/file1.txt", fileDescription.RelativePath);
    }
        
    [Test]
    [TestCase("/subDir")]
    [TestCase("/subDir1/subDir2/subDir")]
    public void Test_DirectoryDescriptionInDirectory(string directoryRelativePath)
    {
        CreateTestDirectory();
        var dataA = CreateSubTestDirectory("dataA");
        var subDirectory = new DirectoryInfo(dataA.Combine(directoryRelativePath));
        subDirectory.Create();

        var inventoryBuilder = GetInventoryBuilder(dataA);

        var fileDescription = IdentityBuilder.BuildDirectoryDescription(inventoryBuilder.Inventory!.InventoryParts.Single(), subDirectory);
            
        ClassicAssert.AreEqual(FileSystemTypes.Directory, fileDescription.FileSystemType);
        ClassicAssert.AreEqual(inventoryBuilder.Inventory, fileDescription.Inventory);
        ClassicAssert.AreEqual(inventoryBuilder.Inventory.InventoryParts.Single(), fileDescription.InventoryPart);
        ClassicAssert.AreEqual("subDir", fileDescription.Name);
        ClassicAssert.AreEqual(directoryRelativePath, fileDescription.RelativePath);
    }

    private static InventoryBuilder GetInventoryBuilder(FileSystemInfo pathItemRoot)
    {
        PathItem pathItem = new PathItem();
        pathItem.Path = pathItemRoot.FullName;
        if (pathItemRoot is DirectoryInfo)
        {
            pathItem.Type = FileSystemTypes.Directory;
        }
        else if (pathItemRoot is FileInfo)
        {
            pathItem.Type = FileSystemTypes.File;
        }
        else
        {
            ClassicAssert.Fail("unknown type " + pathItemRoot.GetType().Name);
        }
            
        var endpoint = new ByteSyncEndpoint
        {
            ClientInstanceId = $"CII_A"
        };
        InventoryBuilder inventoryBuilder = new InventoryBuilder("A",
            SessionSettingsHelper.BuildDefaultSessionSettings(DataTypes.FilesDirectories, LinkingKeys.RelativePath), new InventoryProcessData(), endpoint,
            $"MachineA");

        inventoryBuilder.AddInventoryPart(pathItem);
            
        ClassicAssert.IsNotNull(inventoryBuilder.Inventory);
        ClassicAssert.AreEqual(1, inventoryBuilder.Inventory.InventoryParts.Count);
            
        return inventoryBuilder;
    }
}