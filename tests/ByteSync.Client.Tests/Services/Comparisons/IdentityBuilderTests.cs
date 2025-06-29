using ByteSync.Business;
using ByteSync.Business.DataSources;
using ByteSync.Business.Inventories;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Helpers;
using ByteSync.Services.Comparisons;
using ByteSync.Services.Inventories;
using ByteSync.Tests.TestUtilities.Helpers;
using ByteSync.TestsCommon;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Comparisons;

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

        var fileDescription = IdentityBuilder.BuildFileDescription(inventoryBuilder.Inventory!.InventoryParts.Single(), file1);
            
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

        var fileDescription = IdentityBuilder.BuildFileDescription(inventoryBuilder.Inventory!.InventoryParts.Single(), file1);
            
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

        var fileDescription = IdentityBuilder.BuildDirectoryDescription(inventoryBuilder.Inventory!.InventoryParts.Single(), subDirectory);
            
        fileDescription.FileSystemType.Should().Be(FileSystemTypes.Directory);
        fileDescription.Inventory.Should().Be(inventoryBuilder.Inventory);
        fileDescription.InventoryPart.Should().Be(inventoryBuilder.Inventory.InventoryParts.Single());
        fileDescription.Name.Should().Be("subDir");
        fileDescription.RelativePath.Should().Be(directoryRelativePath);
    }

    private static InventoryBuilder GetInventoryBuilder(FileSystemInfo dataSourceRoot)
    {
        DataSource dataSource = new DataSource();
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
        
        Mock<ILogger<InventoryBuilder>> loggerMock = new Mock<ILogger<InventoryBuilder>>();
            
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
        
        InventoryBuilder inventoryBuilder = new InventoryBuilder(sessionMemberInfo, 
            SessionSettingsHelper.BuildDefaultSessionSettings(DataTypes.FilesDirectories, LinkingKeys.RelativePath), 
            new InventoryProcessData(), OSPlatforms.Windows, FingerprintModes.Rsync, loggerMock.Object);

        inventoryBuilder.AddInventoryPart(dataSource);
            
        inventoryBuilder.Inventory.Should().NotBeNull();
        inventoryBuilder.Inventory.InventoryParts.Count.Should().Be(1);
            
        return inventoryBuilder;
    }
}
