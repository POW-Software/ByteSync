using ByteSync.Business;
using ByteSync.Business.DataNodes;
using ByteSync.Business.DataSources;
using ByteSync.Business.Inventories;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Client.UnitTests.TestUtilities.Helpers;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Helpers;
using ByteSync.Services.Comparisons;
using ByteSync.Services.Inventories;
using ByteSync.TestsCommon;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Comparisons;

[TestFixture]
public class IdentityBuilderTests : AbstractTester
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
        
        var fileDescription = IdentityBuilder.BuildFileDescription(inventoryBuilder.Inventory.InventoryParts.Single(), file1);
        
        fileDescription.Size.Should().Be("file1Content".Length);
        fileDescription.FingerprintMode.Should().BeNull();
        fileDescription.SignatureGuid.Should().BeNull();
        fileDescription.Sha256.Should().BeNull();
        fileDescription.AnalysisErrorDescription.Should().BeNull();
        fileDescription.AnalysisErrorType.Should().BeNull();
        fileDescription.FileSystemType.Should().Be(FileSystemTypes.File);
        fileDescription.LastWriteTimeUtc.Should().Be(file1.LastWriteTimeUtc); // todo CreationTimeUtc
        fileDescription.Inventory.Should().Be(inventoryBuilder.Inventory);
        fileDescription.InventoryPart.Should().Be(inventoryBuilder.Inventory.InventoryParts.Single());
        fileDescription.Name.Should().Be("file1.txt");
        fileDescription.RelativePath.Should().Be(fileRelativePath);
    }
    
    [Test]
    public void Test_FileDescriptionInFile()
    {
        CreateTestDirectory();
        var dataA = CreateSubTestDirectory("dataA");
        var file1 = CreateFileInDirectory(dataA, "file1.txt", "file1Content");
        
        var inventoryBuilder = GetInventoryBuilder(file1);
        
        var fileDescription = IdentityBuilder.BuildFileDescription(inventoryBuilder.Inventory.InventoryParts.Single(), file1);
        
        fileDescription.Size.Should().Be("file1Content".Length);
        fileDescription.FingerprintMode.Should().BeNull();
        fileDescription.SignatureGuid.Should().BeNull();
        fileDescription.Sha256.Should().BeNull();
        fileDescription.AnalysisErrorDescription.Should().BeNull();
        fileDescription.AnalysisErrorType.Should().BeNull();
        fileDescription.FileSystemType.Should().Be(FileSystemTypes.File);
        fileDescription.LastWriteTimeUtc.Should().Be(file1.LastWriteTimeUtc);
        fileDescription.Inventory.Should().Be(inventoryBuilder.Inventory);
        fileDescription.InventoryPart.Should().Be(inventoryBuilder.Inventory.InventoryParts.Single());
        fileDescription.Name.Should().Be("file1.txt");
        fileDescription.RelativePath.Should().Be("/file1.txt");
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
        
        var fileDescription = IdentityBuilder.BuildDirectoryDescription(inventoryBuilder.Inventory.InventoryParts.Single(), subDirectory);
        
        fileDescription.FileSystemType.Should().Be(FileSystemTypes.Directory);
        fileDescription.Inventory.Should().Be(inventoryBuilder.Inventory);
        fileDescription.InventoryPart.Should().Be(inventoryBuilder.Inventory.InventoryParts.Single());
        fileDescription.Name.Should().Be("subDir");
        fileDescription.RelativePath.Should().Be(directoryRelativePath);
    }
    
    [Test]
    public void Test_RelativePathAlwaysStartsWithSlash()
    {
        CreateTestDirectory();
        var dataA = CreateSubTestDirectory("dataA");
        
        // Test with directory inventory part - RootPath with trailing slash
        var inventoryBuilder1 = GetInventoryBuilder(dataA);
        var inventoryPart1 = inventoryBuilder1.Inventory.InventoryParts.Single();
        inventoryPart1.RootPath = dataA.FullName + Path.DirectorySeparatorChar; // Ensure trailing slash
        
        var file1 = CreateFileInDirectory(dataA, "test1.txt", "content1");
        var fileDescription1 = IdentityBuilder.BuildFileDescription(inventoryPart1, file1);
        fileDescription1.RelativePath.Should().StartWith("/");
        
        var subDir1 = new DirectoryInfo(dataA.Combine("subdir1"));
        subDir1.Create();
        var dirDescription1 = IdentityBuilder.BuildDirectoryDescription(inventoryPart1, subDir1);
        dirDescription1.RelativePath.Should().StartWith("/");
        
        // Test with directory inventory part - RootPath without trailing slash
        var inventoryBuilder2 = GetInventoryBuilder(dataA);
        var inventoryPart2 = inventoryBuilder2.Inventory.InventoryParts.Single();
        inventoryPart2.RootPath = dataA.FullName.TrimEnd(Path.DirectorySeparatorChar); // Remove trailing slash
        
        var file2 = CreateFileInDirectory(dataA, "test2.txt", "content2");
        var fileDescription2 = IdentityBuilder.BuildFileDescription(inventoryPart2, file2);
        fileDescription2.RelativePath.Should().StartWith("/");
        
        var subDir2 = new DirectoryInfo(dataA.Combine("subdir2"));
        subDir2.Create();
        var dirDescription2 = IdentityBuilder.BuildDirectoryDescription(inventoryPart2, subDir2);
        dirDescription2.RelativePath.Should().StartWith("/");
        
        // Test with file inventory part
        var file3 = CreateFileInDirectory(dataA, "test3.txt", "content3");
        var inventoryBuilder3 = GetInventoryBuilder(file3);
        var inventoryPart3 = inventoryBuilder3.Inventory.InventoryParts.Single();
        
        var fileDescription3 = IdentityBuilder.BuildFileDescription(inventoryPart3, file3);
        fileDescription3.RelativePath.Should().StartWith("/");
        fileDescription3.RelativePath.Should().Be("/test3.txt");
    }
    
    private static InventoryBuilder GetInventoryBuilder(FileSystemInfo dataSourceRoot)
    {
        var dataSource = new DataSource();
        dataSource.Path = dataSourceRoot.FullName;
        if (dataSourceRoot is DirectoryInfo)
        {
            dataSource.Type = FileSystemTypes.Directory;
        }
        else if (dataSourceRoot is FileInfo)
        {
            dataSource.Type = FileSystemTypes.File;
        }
        else
        {
            Assert.Fail("unknown type " + dataSourceRoot.GetType().Name);
        }
        
        var loggerMock = new Mock<ILogger<InventoryBuilder>>();
        
        var endpoint = new ByteSyncEndpoint
        {
            ClientInstanceId = $"CII_A"
        };
        
        var sessionMemberInfo = new SessionMember
        {
            Endpoint = endpoint,
            PositionInList = 0,
            PrivateData = new()
            {
                MachineName = "MachineA"
            }
        };
        
        var dataNode = new DataNode
        {
            Id = "NodeA",
            Code = "NodeA",
            ClientInstanceId = endpoint.ClientInstanceId
        };
        
        var inventoryFileAnalyzerLoggerMock = new Mock<ILogger<InventoryFileAnalyzer>>();
        
        var processData = new InventoryProcessData();
        var saver = new InventorySaver();
        var analyzer = new InventoryFileAnalyzer(FingerprintModes.Rsync, processData, saver, inventoryFileAnalyzerLoggerMock.Object);
        var inventoryBuilder = new InventoryBuilder(sessionMemberInfo, dataNode,
            SessionSettingsHelper.BuildDefaultSessionSettings(DataTypes.FilesDirectories, MatchingModes.Tree),
            processData, OSPlatforms.Windows, FingerprintModes.Rsync, loggerMock.Object,
            analyzer,
            saver,
            new InventoryIndexer());
        
        inventoryBuilder.AddInventoryPart(dataSource);
        
        inventoryBuilder.Inventory.Should().NotBeNull();
        inventoryBuilder.Inventory.InventoryParts.Count.Should().Be(1);
        
        return inventoryBuilder;
    }
}