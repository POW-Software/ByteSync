using System.Reactive.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using Autofac;
using ByteSync.Business;
using ByteSync.Business.DataNodes;
using ByteSync.Business.Inventories;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Common.Business.Misc;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Services.Inventories;
using ByteSync.TestsCommon;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ByteSync.Client.IntegrationTests.Services.Inventories;

[TestFixture]
public class InventoryBuilderAccessHandling_IntegrationTests : IntegrationTest
{
    private InventoryProcessData _inventoryProcessData = null!;
    
    private static bool CanAccessFile(string filePath)
    {
        try
        {
            using var _ = File.OpenRead(filePath);
            
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }
    
    private static bool CanAccessDirectory(string dirPath)
    {
        try
        {
            Directory.EnumerateFileSystemEntries(dirPath).Any();
            
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }
    
    [SetUp]
    public void Setup()
    {
        BuildMoqContainer();
        
        var contextHelper = new TestContextGenerator(Container);
        contextHelper.GenerateSession();
        contextHelper.GenerateCurrentEndpoint();
        var testDirectory = _testDirectoryService.CreateTestDirectory();
        
        var env = Container.Resolve<Mock<IEnvironmentService>>();
        env.Setup(m => m.AssemblyFullName).Returns(Path.Combine(testDirectory.FullName, "Assembly", "Assembly.exe"));
        
        var appData = Container.Resolve<Mock<ILocalApplicationDataManager>>();
        appData.Setup(m => m.ApplicationDataPath).Returns(Path.Combine(testDirectory.FullName, "ApplicationDataPath"));
        
        _inventoryProcessData = new InventoryProcessData();
    }
    
    [Test]
    [Platform(Include = "Win")]
    public async Task InaccessibleDirectory_MarkedAsInaccessibleAndSkipped_Windows()
    {
        var dataRoot = _testDirectoryService.CreateSubTestDirectory("data");
        var accessibleDir = Directory.CreateDirectory(Path.Combine(dataRoot.FullName, "accessible"));
        var inaccessibleDir = Directory.CreateDirectory(Path.Combine(dataRoot.FullName, "inaccessible"));
        
        File.WriteAllText(Path.Combine(accessibleDir.FullName, "file1.txt"), "content1");
        
        var fileInInaccessible = Path.Combine(inaccessibleDir.FullName, "file2.txt");
        File.WriteAllText(fileInInaccessible, "content2");
        
        var original = inaccessibleDir.GetAccessControl();
        var sid = WindowsIdentity.GetCurrent().User!;
        var denyRule = new FileSystemAccessRule(sid,
            FileSystemRights.ListDirectory | FileSystemRights.ReadData,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None,
            AccessControlType.Deny);
        
        try
        {
            var sec = inaccessibleDir.GetAccessControl();
            sec.AddAccessRule(denyRule);
            inaccessibleDir.SetAccessControl(sec);
            
            var sessionMember = new SessionMember
            {
                Endpoint = new(),
                PrivateData = new() { MachineName = "Test" }
            };
            var dataNode = new DataNode { Id = "DN1", Code = "A" };
            var sessionSettings = new SessionSettings
            {
                DataType = DataTypes.Files,
                MatchingMode = MatchingModes.Tree,
                LinkingCase = LinkingCases.Sensitive
            };
            
            var loggerMock = new Mock<ILogger<InventoryBuilder>>();
            var inventoryFileAnalyzerLoggerMock = new Mock<ILogger<InventoryFileAnalyzer>>();
            
            var inventorySaver = new InventorySaver();
            var inventoryFileAnalyzer = new InventoryFileAnalyzer(FingerprintModes.Rsync, _inventoryProcessData, inventorySaver,
                inventoryFileAnalyzerLoggerMock.Object);
            
            var inventoryIndexer = new InventoryIndexer();
            
            var builder = new InventoryBuilder(
                sessionMember,
                dataNode,
                sessionSettings,
                _inventoryProcessData,
                OSPlatforms.Windows,
                FingerprintModes.Rsync,
                loggerMock.Object,
                inventoryFileAnalyzer,
                inventorySaver,
                inventoryIndexer
            );
            
            builder.AddInventoryPart(dataRoot.FullName);
            
            var inventoryFile = Path.Combine(_testDirectoryService.TestDirectory.FullName, "inventory.zip");
            await builder.BuildBaseInventoryAsync(inventoryFile);
            
            var inventory = builder.Inventory;
            var part = inventory.InventoryParts.First();
            
            part.DirectoryDescriptions.Should().HaveCountGreaterThanOrEqualTo(2);
            
            var accessibleDirDesc = part.DirectoryDescriptions
                .FirstOrDefault(d => d.RelativePath.Contains("accessible"));
            accessibleDirDesc.Should().NotBeNull();
            accessibleDirDesc!.IsAccessible.Should().BeTrue();
            
            var inaccessibleDirDesc = part.DirectoryDescriptions
                .FirstOrDefault(d => d.RelativePath.Contains("inaccessible"));
            inaccessibleDirDesc.Should().NotBeNull();
            inaccessibleDirDesc!.IsAccessible.Should().BeFalse();
            
            var file1 = part.FileDescriptions.FirstOrDefault(f => f.RelativePath.Contains("file1.txt"));
            file1.Should().NotBeNull();
            file1!.IsAccessible.Should().BeTrue();
            
            var file2 = part.FileDescriptions.FirstOrDefault(f => f.RelativePath.Contains("file2.txt"));
            file2.Should().BeNull("le fichier dans un répertoire inaccessible ne doit pas être inventorié");
        }
        finally
        {
            var sec = inaccessibleDir.GetAccessControl();
            sec.RemoveAccessRule(denyRule);
            inaccessibleDir.SetAccessControl(sec);
        }
    }
    
    [Test]
    [Platform(Include = "Linux,MacOsX")]
    public async Task InaccessibleDirectory_MarkedAsInaccessibleAndSkipped_Posix()
    {
        var dataRoot = _testDirectoryService.CreateSubTestDirectory("data");
        var accessibleDir = Directory.CreateDirectory(Path.Combine(dataRoot.FullName, "accessible"));
        var inaccessibleDir = Directory.CreateDirectory(Path.Combine(dataRoot.FullName, "inaccessible"));
        
        File.WriteAllText(Path.Combine(accessibleDir.FullName, "file1.txt"), "content1");
        var fileInInaccessible = Path.Combine(inaccessibleDir.FullName, "file2.txt");
        File.WriteAllText(fileInInaccessible, "content2");
        
        try
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                File.SetUnixFileMode(inaccessibleDir.FullName, UnixFileMode.None);
                
                if (CanAccessDirectory(inaccessibleDir.FullName))
                {
                    Assert.Ignore("Cannot test inaccessible directory - running with elevated permissions that bypass access restrictions");
                }
            }
            
            var sessionMember = new SessionMember
            {
                Endpoint = new(),
                PrivateData = new() { MachineName = "Test" }
            };
            var dataNode = new DataNode { Id = "DN1", Code = "A" };
            var sessionSettings = new SessionSettings
            {
                DataType = DataTypes.Files,
                MatchingMode = MatchingModes.Tree,
                LinkingCase = LinkingCases.Sensitive
            };
            
            var loggerMock = new Mock<ILogger<InventoryBuilder>>();
            var inventoryFileAnalyzerLoggerMock = new Mock<ILogger<InventoryFileAnalyzer>>();
            
            var inventorySaver = new InventorySaver();
            var inventoryFileAnalyzer = new InventoryFileAnalyzer(FingerprintModes.Rsync, _inventoryProcessData, inventorySaver,
                inventoryFileAnalyzerLoggerMock.Object);
            
            var inventoryIndexer = new InventoryIndexer();
            
            var builder = new InventoryBuilder(
                sessionMember,
                dataNode,
                sessionSettings,
                _inventoryProcessData,
                OSPlatforms.Linux,
                FingerprintModes.Rsync,
                loggerMock.Object,
                inventoryFileAnalyzer,
                inventorySaver,
                inventoryIndexer
            );
            
            builder.AddInventoryPart(dataRoot.FullName);
            
            var inventoryFile = Path.Combine(_testDirectoryService.TestDirectory.FullName, "inventory.zip");
            await builder.BuildBaseInventoryAsync(inventoryFile);
            
            var inventory = builder.Inventory;
            var part = inventory.InventoryParts.First();
            
            part.DirectoryDescriptions.Should().HaveCountGreaterThanOrEqualTo(2);
            
            var accessibleDirDesc = part.DirectoryDescriptions
                .FirstOrDefault(d => d.RelativePath.Contains("accessible"));
            accessibleDirDesc.Should().NotBeNull();
            accessibleDirDesc!.IsAccessible.Should().BeTrue();
            
            var inaccessibleDirDesc = part.DirectoryDescriptions
                .FirstOrDefault(d => d.RelativePath.Contains("inaccessible"));
            inaccessibleDirDesc.Should().NotBeNull();
            inaccessibleDirDesc!.IsAccessible.Should().BeFalse();
            
            var file1 = part.FileDescriptions.FirstOrDefault(f => f.RelativePath.Contains("file1.txt"));
            file1.Should().NotBeNull();
            file1!.IsAccessible.Should().BeTrue();
            
            var file2 = part.FileDescriptions.FirstOrDefault(f => f.RelativePath.Contains("file2.txt"));
            file2.Should().BeNull("le fichier dans un répertoire inaccessible ne doit pas être inventorié");
        }
        finally
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                File.SetUnixFileMode(inaccessibleDir.FullName,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }
        }
    }
    
    [Test]
    [Platform(Include = "Win")]
    public async Task InaccessibleFile_MarkedAsInaccessibleButDirectoryAccessible_Windows()
    {
        var dataRoot = _testDirectoryService.CreateSubTestDirectory("data");
        var subDir = Directory.CreateDirectory(Path.Combine(dataRoot.FullName, "subdir"));
        
        var accessibleFile = Path.Combine(subDir.FullName, "accessible.txt");
        File.WriteAllText(accessibleFile, "content1");
        
        var inaccessibleFile = Path.Combine(subDir.FullName, "inaccessible.txt");
        File.WriteAllText(inaccessibleFile, "content2");
        
        var originalFile = new FileInfo(inaccessibleFile).GetAccessControl();
        var sid = WindowsIdentity.GetCurrent().User!;
        var denyRule = new FileSystemAccessRule(sid,
            FileSystemRights.ReadData | FileSystemRights.ReadAttributes | FileSystemRights.ReadExtendedAttributes,
            AccessControlType.Deny);
        
        try
        {
            var sec = new FileInfo(inaccessibleFile).GetAccessControl();
            sec.AddAccessRule(denyRule);
            new FileInfo(inaccessibleFile).SetAccessControl(sec);
            
            var sessionMember = new SessionMember
            {
                Endpoint = new(),
                PrivateData = new() { MachineName = "Test" }
            };
            var dataNode = new DataNode { Id = "DN1", Code = "A" };
            var sessionSettings = new SessionSettings
            {
                DataType = DataTypes.Files,
                MatchingMode = MatchingModes.Tree,
                LinkingCase = LinkingCases.Sensitive
            };
            
            var loggerMock = new Mock<ILogger<InventoryBuilder>>();
            var inventoryFileAnalyzerLoggerMock = new Mock<ILogger<InventoryFileAnalyzer>>();
            
            var inventorySaver = new InventorySaver();
            var inventoryFileAnalyzer = new InventoryFileAnalyzer(FingerprintModes.Rsync, _inventoryProcessData, inventorySaver,
                inventoryFileAnalyzerLoggerMock.Object);
            
            var inventoryIndexer = new InventoryIndexer();
            
            var builder = new InventoryBuilder(
                sessionMember,
                dataNode,
                sessionSettings,
                _inventoryProcessData,
                OSPlatforms.Windows,
                FingerprintModes.Rsync,
                loggerMock.Object,
                inventoryFileAnalyzer,
                inventorySaver,
                inventoryIndexer
            );
            
            builder.AddInventoryPart(dataRoot.FullName);
            
            var inventoryFile = Path.Combine(_testDirectoryService.TestDirectory.FullName, "inventory.zip");
            await builder.BuildBaseInventoryAsync(inventoryFile);
            
            var inventory = builder.Inventory;
            var part = inventory.InventoryParts.First();
            
            var subDirDesc = part.DirectoryDescriptions.FirstOrDefault(d => d.RelativePath.Contains("subdir"));
            subDirDesc.Should().NotBeNull();
            subDirDesc!.IsAccessible.Should().BeTrue();
            
            var accessibleFileDesc = part.FileDescriptions.FirstOrDefault(f => f.RelativePath.Contains("accessible.txt"));
            accessibleFileDesc.Should().NotBeNull();
            accessibleFileDesc!.IsAccessible.Should().BeTrue();
            
            var inaccessibleFileDesc = part.FileDescriptions.FirstOrDefault(f => f.RelativePath.Contains("inaccessible.txt"));
            inaccessibleFileDesc.Should().NotBeNull();
            inaccessibleFileDesc!.IsAccessible.Should().BeFalse();
        }
        finally
        {
            var sec = new FileInfo(inaccessibleFile).GetAccessControl();
            sec.RemoveAccessRule(denyRule);
            new FileInfo(inaccessibleFile).SetAccessControl(sec);
        }
    }
    
    [Test]
    [Platform(Include = "Linux,MacOsX")]
    public async Task InaccessibleFile_MarkedAsInaccessibleButDirectoryAccessible_Posix()
    {
        var dataRoot = _testDirectoryService.CreateSubTestDirectory("data");
        var subDir = Directory.CreateDirectory(Path.Combine(dataRoot.FullName, "subdir"));
        
        var accessibleFile = Path.Combine(subDir.FullName, "accessible.txt");
        File.WriteAllText(accessibleFile, "content1");
        
        var inaccessibleFile = Path.Combine(subDir.FullName, "inaccessible.txt");
        File.WriteAllText(inaccessibleFile, "content2");
        
        try
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                File.SetUnixFileMode(inaccessibleFile, UnixFileMode.None);
                
                if (CanAccessFile(inaccessibleFile))
                {
                    Assert.Ignore("Cannot test inaccessible file - running with elevated permissions that bypass access restrictions");
                }
            }
            
            var sessionMember = new SessionMember
            {
                Endpoint = new(),
                PrivateData = new() { MachineName = "Test" }
            };
            var dataNode = new DataNode { Id = "DN1", Code = "A" };
            var sessionSettings = new SessionSettings
            {
                DataType = DataTypes.Files,
                MatchingMode = MatchingModes.Tree,
                LinkingCase = LinkingCases.Sensitive
            };
            
            var loggerMock = new Mock<ILogger<InventoryBuilder>>();
            var inventoryFileAnalyzerLoggerMock = new Mock<ILogger<InventoryFileAnalyzer>>();
            
            var inventorySaver = new InventorySaver();
            var inventoryFileAnalyzer = new InventoryFileAnalyzer(FingerprintModes.Rsync, _inventoryProcessData, inventorySaver,
                inventoryFileAnalyzerLoggerMock.Object);
            
            var inventoryIndexer = new InventoryIndexer();
            
            var builder = new InventoryBuilder(
                sessionMember,
                dataNode,
                sessionSettings,
                _inventoryProcessData,
                OSPlatforms.Linux,
                FingerprintModes.Rsync,
                loggerMock.Object,
                inventoryFileAnalyzer,
                inventorySaver,
                inventoryIndexer
            );
            
            builder.AddInventoryPart(dataRoot.FullName);
            
            var inventoryFile = Path.Combine(_testDirectoryService.TestDirectory.FullName, "inventory.zip");
            await builder.BuildBaseInventoryAsync(inventoryFile);
            
            var inventory = builder.Inventory;
            var part = inventory.InventoryParts.First();
            
            var subDirDesc = part.DirectoryDescriptions.FirstOrDefault(d => d.RelativePath.Contains("subdir"));
            subDirDesc.Should().NotBeNull();
            subDirDesc!.IsAccessible.Should().BeTrue();
            
            var accessibleFileDesc = part.FileDescriptions.FirstOrDefault(f => f.RelativePath.Contains("accessible.txt"));
            accessibleFileDesc.Should().NotBeNull();
            accessibleFileDesc!.IsAccessible.Should().BeTrue();
            
            var inaccessibleFileDesc = part.FileDescriptions.FirstOrDefault(f => f.RelativePath.Contains("inaccessible.txt"));
            inaccessibleFileDesc.Should().NotBeNull();
            inaccessibleFileDesc!.IsAccessible.Should().BeFalse();
        }
        finally
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                File.SetUnixFileMode(inaccessibleFile,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead);
            }
        }
    }
    
    [Test]
    public async Task InaccessibleFiles_NotCountedInIdentifiedVolume()
    {
        var dataRoot = _testDirectoryService.CreateSubTestDirectory("data");
        
        var accessibleFile1 = Path.Combine(dataRoot.FullName, "file1.txt");
        File.WriteAllText(accessibleFile1, new string('A', 1000));
        
        var accessibleFile2 = Path.Combine(dataRoot.FullName, "file2.txt");
        File.WriteAllText(accessibleFile2, new string('B', 2000));
        
        var inaccessibleFile = Path.Combine(dataRoot.FullName, "inaccessible.txt");
        File.WriteAllText(inaccessibleFile, new string('C', 5000));
        
        FileSecurity? originalSecurity = null;
        UnixFileMode? originalMode = null;
        
        try
        {
            if (OperatingSystem.IsWindows())
            {
                originalSecurity = new FileInfo(inaccessibleFile).GetAccessControl();
                var sid = WindowsIdentity.GetCurrent().User!;
                var denyRule = new FileSystemAccessRule(sid,
                    FileSystemRights.ReadData | FileSystemRights.ReadAttributes,
                    AccessControlType.Deny);
                var sec = new FileInfo(inaccessibleFile).GetAccessControl();
                sec.AddAccessRule(denyRule);
                new FileInfo(inaccessibleFile).SetAccessControl(sec);
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                originalMode = File.GetUnixFileMode(inaccessibleFile);
                File.SetUnixFileMode(inaccessibleFile, UnixFileMode.None);
                
                if (CanAccessFile(inaccessibleFile))
                {
                    Assert.Ignore("Cannot test inaccessible file - running with elevated permissions that bypass access restrictions");
                }
            }
            
            var sessionMember = new SessionMember
            {
                Endpoint = new(),
                PrivateData = new() { MachineName = "Test" }
            };
            var dataNode = new DataNode { Id = "DN1", Code = "A" };
            var sessionSettings = new SessionSettings
            {
                DataType = DataTypes.Files,
                MatchingMode = MatchingModes.Tree,
                LinkingCase = LinkingCases.Sensitive
            };
            
            var loggerMock = new Mock<ILogger<InventoryBuilder>>();
            var inventoryFileAnalyzerLoggerMock = new Mock<ILogger<InventoryFileAnalyzer>>();
            
            var inventorySaver = new InventorySaver();
            var inventoryFileAnalyzer = new InventoryFileAnalyzer(FingerprintModes.Rsync, _inventoryProcessData, inventorySaver,
                inventoryFileAnalyzerLoggerMock.Object);
            
            var inventoryIndexer = new InventoryIndexer();
            
            var osPlatform = OperatingSystem.IsWindows() ? OSPlatforms.Windows :
                OperatingSystem.IsLinux() ? OSPlatforms.Linux : OSPlatforms.MacOs;
            
            var builder = new InventoryBuilder(
                sessionMember,
                dataNode,
                sessionSettings,
                _inventoryProcessData,
                osPlatform,
                FingerprintModes.Rsync,
                loggerMock.Object,
                inventoryFileAnalyzer,
                inventorySaver,
                inventoryIndexer
            );
            
            builder.AddInventoryPart(dataRoot.FullName);
            
            var inventoryFile = Path.Combine(_testDirectoryService.TestDirectory.FullName, "inventory.zip");
            await builder.BuildBaseInventoryAsync(inventoryFile);
            
            var monitorData = await _inventoryProcessData.InventoryMonitorObservable.FirstAsync();
            
            monitorData.IdentifiedFiles.Should().Be(3);
            
            monitorData.IdentifiedVolume.Should().Be(3000, "les fichiers inaccessibles ne doivent pas être comptés dans le volume");
            
            var inventory = builder.Inventory;
            var part = inventory.InventoryParts.First();
            
            part.FileDescriptions.Should().HaveCount(3);
            
            part.FileDescriptions.Count(f => f.IsAccessible).Should().Be(2);
            part.FileDescriptions.Count(f => !f.IsAccessible).Should().Be(1);
        }
        finally
        {
            if (OperatingSystem.IsWindows() && originalSecurity != null)
            {
                new FileInfo(inaccessibleFile).SetAccessControl(originalSecurity);
            }
            else if ((OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) && originalMode.HasValue)
            {
                File.SetUnixFileMode(inaccessibleFile, originalMode.Value);
            }
        }
    }
    
    [Test]
    public async Task InventoryContinues_AfterEncountering_InaccessibleItems()
    {
        var dataRoot = _testDirectoryService.CreateSubTestDirectory("data");
        
        var dir1 = Directory.CreateDirectory(Path.Combine(dataRoot.FullName, "dir1"));
        File.WriteAllText(Path.Combine(dir1.FullName, "file1.txt"), "content1");
        
        var dir2 = Directory.CreateDirectory(Path.Combine(dataRoot.FullName, "dir2_inaccessible"));
        File.WriteAllText(Path.Combine(dir2.FullName, "file2.txt"), "content2");
        
        var dir3 = Directory.CreateDirectory(Path.Combine(dataRoot.FullName, "dir3"));
        File.WriteAllText(Path.Combine(dir3.FullName, "file3.txt"), "content3");
        
        DirectorySecurity? originalSecurity = null;
        UnixFileMode? originalMode = null;
        
        try
        {
            if (OperatingSystem.IsWindows())
            {
                originalSecurity = dir2.GetAccessControl();
                var sid = WindowsIdentity.GetCurrent().User!;
                var denyRule = new FileSystemAccessRule(sid,
                    FileSystemRights.ListDirectory,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Deny);
                var sec = dir2.GetAccessControl();
                sec.AddAccessRule(denyRule);
                dir2.SetAccessControl(sec);
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                originalMode = File.GetUnixFileMode(dir2.FullName);
                File.SetUnixFileMode(dir2.FullName, UnixFileMode.None);
                
                if (CanAccessDirectory(dir2.FullName))
                {
                    Assert.Ignore("Cannot test inaccessible directory - running with elevated permissions that bypass access restrictions");
                }
            }
            
            var sessionMember = new SessionMember
            {
                Endpoint = new(),
                PrivateData = new() { MachineName = "Test" }
            };
            var dataNode = new DataNode { Id = "DN1", Code = "A" };
            var sessionSettings = new SessionSettings
            {
                DataType = DataTypes.Files,
                MatchingMode = MatchingModes.Tree,
                LinkingCase = LinkingCases.Sensitive
            };
            
            var loggerMock = new Mock<ILogger<InventoryBuilder>>();
            var inventoryFileAnalyzerLoggerMock = new Mock<ILogger<InventoryFileAnalyzer>>();
            
            var inventorySaver = new InventorySaver();
            var inventoryFileAnalyzer = new InventoryFileAnalyzer(FingerprintModes.Rsync, _inventoryProcessData, inventorySaver,
                inventoryFileAnalyzerLoggerMock.Object);
            
            var inventoryIndexer = new InventoryIndexer();
            
            var osPlatform = OperatingSystem.IsWindows() ? OSPlatforms.Windows :
                OperatingSystem.IsLinux() ? OSPlatforms.Linux : OSPlatforms.MacOs;
            
            var builder = new InventoryBuilder(
                sessionMember,
                dataNode,
                sessionSettings,
                _inventoryProcessData,
                osPlatform,
                FingerprintModes.Rsync,
                loggerMock.Object,
                inventoryFileAnalyzer,
                inventorySaver,
                inventoryIndexer
            );
            
            builder.AddInventoryPart(dataRoot.FullName);
            
            var inventoryFile = Path.Combine(_testDirectoryService.TestDirectory.FullName, "inventory.zip");
            await builder.BuildBaseInventoryAsync(inventoryFile);
            
            var inventory = builder.Inventory;
            var part = inventory.InventoryParts.First();
            
            var dir1Desc = part.DirectoryDescriptions.FirstOrDefault(d => d.RelativePath.Contains("dir1"));
            dir1Desc.Should().NotBeNull();
            dir1Desc!.IsAccessible.Should().BeTrue();
            
            var dir3Desc = part.DirectoryDescriptions.FirstOrDefault(d => d.RelativePath.Contains("dir3"));
            dir3Desc.Should().NotBeNull();
            dir3Desc!.IsAccessible.Should().BeTrue();
            
            var dir2Desc = part.DirectoryDescriptions.FirstOrDefault(d => d.RelativePath.Contains("dir2"));
            dir2Desc.Should().NotBeNull();
            dir2Desc!.IsAccessible.Should().BeFalse();
            
            part.FileDescriptions.Should().Contain(f => f.RelativePath.Contains("file1.txt"));
            part.FileDescriptions.Should().Contain(f => f.RelativePath.Contains("file3.txt"));
            
            part.FileDescriptions.Should().NotContain(f => f.RelativePath.Contains("file2.txt"));
        }
        finally
        {
            if (OperatingSystem.IsWindows() && originalSecurity != null)
            {
                dir2.SetAccessControl(originalSecurity);
            }
            else if ((OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) && originalMode.HasValue)
            {
                File.SetUnixFileMode(dir2.FullName, originalMode.Value);
            }
        }
    }
}