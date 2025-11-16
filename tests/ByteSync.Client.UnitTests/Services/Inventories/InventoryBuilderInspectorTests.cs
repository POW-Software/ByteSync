using ByteSync.Business;
using ByteSync.Business.DataNodes;
using ByteSync.Business.Inventories;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Misc;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Services.Inventories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Inventories;

public class InventoryBuilderInspectorTests
{
    private string _rootTemp = null!;
    private readonly List<ManualResetEvent> _manualResetEvents = new();
    
    [SetUp]
    public void SetUp()
    {
        _rootTemp = Path.Combine(Path.GetTempPath(), "ByteSync.Unit", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_rootTemp);
    }
    
    [TearDown]
    public void TearDown()
    {
        try
        {
            if (Directory.Exists(_rootTemp)) Directory.Delete(_rootTemp, true);
        }
        catch
        {
            /* ignore */
        }
        
        foreach (var mre in _manualResetEvents)
        {
            mre.Dispose();
        }
        
        _manualResetEvents.Clear();
    }
    
    private InventoryBuilder CreateBuilder(IFileSystemInspector inspector)
    {
        var endpoint = new ByteSyncEndpoint
        {
            ClientId = "client",
            ClientInstanceId = Guid.NewGuid().ToString("N"),
            Version = "1.0",
            OSPlatform = OSPlatforms.Windows,
            IpAddress = "127.0.0.1"
        };
        
        var sessionMember = new SessionMember
        {
            Endpoint = endpoint,
            PrivateData = new SessionMemberPrivateData { MachineName = Environment.MachineName },
            SessionId = Guid.NewGuid().ToString("N"),
            JoinedSessionOn = DateTimeOffset.UtcNow,
            PositionInList = 0
        };
        
        var dataNode = new DataNode
            { Id = Guid.NewGuid().ToString("N"), ClientInstanceId = endpoint.ClientInstanceId, Code = "A", OrderIndex = 0 };
        var settings = SessionSettings.BuildDefault();
        var processData = new InventoryProcessData();
        
        var logger = new Mock<ILogger<InventoryBuilder>>().Object;
        var analyzer = new Mock<IInventoryFileAnalyzer>();
        analyzer.SetupAllProperties();
        var mre = new ManualResetEvent(false);
        _manualResetEvents.Add(mre);
        analyzer.Setup(a => a.HasFinished).Returns(mre);
        var saver = new InventorySaver();
        var indexer = new Mock<IInventoryIndexer>().Object;
        
        return new InventoryBuilder(sessionMember, dataNode, settings, processData, endpoint.OSPlatform,
            FingerprintModes.Rsync, logger, analyzer.Object, saver, indexer, inspector);
    }
    
    [Test]
    public async Task Hidden_File_Is_Ignored()
    {
        var insp = new Mock<IFileSystemInspector>(MockBehavior.Strict);
        insp.Setup(i => i.IsHidden(It.IsAny<FileSystemInfo>(), It.IsAny<OSPlatforms>())).Returns(true);
        var builder = CreateBuilder(insp.Object);
        
        var filePath = Path.Combine(_rootTemp, "a.txt");
        await File.WriteAllTextAsync(filePath, "x");
        
        builder.AddInventoryPart(filePath);
        var invPath = Path.Combine(_rootTemp, "inv.zip");
        await builder.BuildBaseInventoryAsync(invPath);
        
        var part = builder.Inventory.InventoryParts.Single();
        part.FileDescriptions.Should().BeEmpty();
    }
    
    [Test]
    public async Task System_File_Is_Ignored()
    {
        var insp = new Mock<IFileSystemInspector>(MockBehavior.Strict);
        insp.Setup(i => i.IsHidden(It.IsAny<FileSystemInfo>(), It.IsAny<OSPlatforms>())).Returns(false);
        insp.Setup(i => i.IsSystem(It.IsAny<FileInfo>())).Returns(true);
        var builder = CreateBuilder(insp.Object);
        
        var filePath = Path.Combine(_rootTemp, "b.txt");
        await File.WriteAllTextAsync(filePath, "x");
        
        builder.AddInventoryPart(filePath);
        var invPath = Path.Combine(_rootTemp, "inv2.zip");
        await builder.BuildBaseInventoryAsync(invPath);
        
        var part = builder.Inventory.InventoryParts.Single();
        part.FileDescriptions.Should().BeEmpty();
    }
    
    [Test]
    public async Task Reparse_File_Is_Ignored()
    {
        var insp = new Mock<IFileSystemInspector>(MockBehavior.Strict);
        insp.Setup(i => i.IsHidden(It.IsAny<FileSystemInfo>(), It.IsAny<OSPlatforms>())).Returns(false);
        insp.Setup(i => i.IsSystem(It.IsAny<FileInfo>())).Returns(false);
        insp.Setup(i => i.IsReparsePoint(It.IsAny<FileSystemInfo>())).Returns(true);
        var builder = CreateBuilder(insp.Object);
        
        var filePath = Path.Combine(_rootTemp, "c.txt");
        await File.WriteAllTextAsync(filePath, "x");
        
        builder.AddInventoryPart(filePath);
        var invPath = Path.Combine(_rootTemp, "inv3.zip");
        await builder.BuildBaseInventoryAsync(invPath);
        
        var part = builder.Inventory.InventoryParts.Single();
        part.FileDescriptions.Should().BeEmpty();
    }
    
    [Test]
    public async Task ExistsFalse_File_Is_Ignored()
    {
        var insp = new Mock<IFileSystemInspector>(MockBehavior.Strict);
        insp.Setup(i => i.IsHidden(It.IsAny<FileSystemInfo>(), It.IsAny<OSPlatforms>())).Returns(false);
        insp.Setup(i => i.IsSystem(It.IsAny<FileInfo>())).Returns(false);
        insp.Setup(i => i.IsReparsePoint(It.IsAny<FileSystemInfo>())).Returns(false);
        insp.Setup(i => i.Exists(It.IsAny<FileInfo>())).Returns(false);
        insp.Setup(i => i.IsOffline(It.IsAny<FileInfo>())).Returns(false);
        insp.Setup(i => i.IsRecallOnDataAccess(It.IsAny<FileInfo>())).Returns(false);
        var builder = CreateBuilder(insp.Object);
        
        var filePath = Path.Combine(_rootTemp, "d.txt");
        await File.WriteAllTextAsync(filePath, "x");
        
        builder.AddInventoryPart(filePath);
        var invPath = Path.Combine(_rootTemp, "inv4.zip");
        await builder.BuildBaseInventoryAsync(invPath);
        
        var part = builder.Inventory.InventoryParts.Single();
        part.FileDescriptions.Should().BeEmpty();
    }
    
    [Test]
    public async Task UnauthorizedAccess_Adds_Inaccessible_FileDescription()
    {
        var insp = new Mock<IFileSystemInspector>(MockBehavior.Strict);
        
        // Directory is readable
        insp.Setup(i => i.IsHidden(It.IsAny<DirectoryInfo>(), It.IsAny<OSPlatforms>())).Returns(false);
        
        // File access triggers UnauthorizedAccess inside DoAnalyze(FileInfo) try/catch
        insp.Setup(i => i.IsHidden(It.IsAny<FileInfo>(), It.IsAny<OSPlatforms>()))
            .Throws(new UnauthorizedAccessException("denied"));
        insp.Setup(i => i.IsSystem(It.IsAny<FileInfo>())).Returns(false);
        insp.Setup(i => i.IsReparsePoint(It.IsAny<FileSystemInfo>())).Returns(false);
        insp.Setup(i => i.Exists(It.IsAny<FileInfo>())).Returns(true);
        insp.Setup(i => i.IsOffline(It.IsAny<FileInfo>())).Returns(false);
        insp.Setup(i => i.IsRecallOnDataAccess(It.IsAny<FileInfo>())).Returns(false);
        var builder = CreateBuilder(insp.Object);
        
        var root = Directory.CreateDirectory(Path.Combine(_rootTemp, "root"));
        var filePath = Path.Combine(root.FullName, "e.txt");
        await File.WriteAllTextAsync(filePath, "x");
        
        builder.AddInventoryPart(root.FullName);
        var invPath = Path.Combine(_rootTemp, "inv5.zip");
        await builder.BuildBaseInventoryAsync(invPath);
        
        var part = builder.Inventory.InventoryParts.Single();
        part.FileDescriptions.Should().ContainSingle();
        var fd = part.FileDescriptions.Single();
        fd.IsAccessible.Should().BeFalse();
        fd.RelativePath.Should().Be("/e.txt");
    }
    
    [Test]
    public async Task DirectoryNotFound_Adds_Inaccessible_FileDescription()
    {
        var insp = new Mock<IFileSystemInspector>(MockBehavior.Strict);
        insp.Setup(i => i.IsHidden(It.IsAny<DirectoryInfo>(), It.IsAny<OSPlatforms>())).Returns(false);
        insp.Setup(i => i.IsHidden(It.IsAny<FileInfo>(), It.IsAny<OSPlatforms>()))
            .Throws(new DirectoryNotFoundException("parent missing"));
        insp.Setup(i => i.IsSystem(It.IsAny<FileInfo>())).Returns(false);
        insp.Setup(i => i.IsReparsePoint(It.IsAny<FileSystemInfo>())).Returns(false);
        insp.Setup(i => i.Exists(It.IsAny<FileInfo>())).Returns(true);
        insp.Setup(i => i.IsOffline(It.IsAny<FileInfo>())).Returns(false);
        insp.Setup(i => i.IsRecallOnDataAccess(It.IsAny<FileInfo>())).Returns(false);
        
        var builder = CreateBuilder(insp.Object);
        
        var root = Directory.CreateDirectory(Path.Combine(_rootTemp, "root_df"));
        var filePath = Path.Combine(root.FullName, "df.txt");
        await File.WriteAllTextAsync(filePath, "x");
        
        builder.AddInventoryPart(root.FullName);
        var invPath = Path.Combine(_rootTemp, "inv_df.zip");
        await builder.BuildBaseInventoryAsync(invPath);
        
        var part = builder.Inventory.InventoryParts.Single();
        part.FileDescriptions.Should().ContainSingle();
        var fd = part.FileDescriptions.Single();
        fd.IsAccessible.Should().BeFalse();
        fd.RelativePath.Should().Be("/df.txt");
    }
    
    [Test]
    public async Task IOException_Adds_Inaccessible_FileDescription()
    {
        var insp = new Mock<IFileSystemInspector>(MockBehavior.Strict);
        insp.Setup(i => i.IsHidden(It.IsAny<DirectoryInfo>(), It.IsAny<OSPlatforms>())).Returns(false);
        insp.Setup(i => i.IsHidden(It.IsAny<FileInfo>(), It.IsAny<OSPlatforms>()))
            .Throws(new IOException("io error"));
        insp.Setup(i => i.IsSystem(It.IsAny<FileInfo>())).Returns(false);
        insp.Setup(i => i.IsReparsePoint(It.IsAny<FileSystemInfo>())).Returns(false);
        insp.Setup(i => i.Exists(It.IsAny<FileInfo>())).Returns(true);
        insp.Setup(i => i.IsOffline(It.IsAny<FileInfo>())).Returns(false);
        insp.Setup(i => i.IsRecallOnDataAccess(It.IsAny<FileInfo>())).Returns(false);
        
        var builder = CreateBuilder(insp.Object);
        
        var root = Directory.CreateDirectory(Path.Combine(_rootTemp, "root_io"));
        var filePath = Path.Combine(root.FullName, "io.txt");
        await File.WriteAllTextAsync(filePath, "x");
        
        builder.AddInventoryPart(root.FullName);
        var invPath = Path.Combine(_rootTemp, "inv_io.zip");
        await builder.BuildBaseInventoryAsync(invPath);
        
        var part = builder.Inventory.InventoryParts.Single();
        part.FileDescriptions.Should().ContainSingle();
        var fd = part.FileDescriptions.Single();
        fd.IsAccessible.Should().BeFalse();
        fd.RelativePath.Should().Be("/io.txt");
    }
    
    [Test]
    public async Task Directory_IOException_Marked_Inaccessible_And_Skipped()
    {
        var insp = new Mock<IFileSystemInspector>(MockBehavior.Strict);
        insp.Setup(i => i.IsHidden(It.IsAny<FileSystemInfo>(), It.IsAny<OSPlatforms>())).Returns(false);
        insp.Setup(i => i.IsSystem(It.IsAny<FileInfo>())).Returns(false);
        insp.Setup(i => i.Exists(It.IsAny<FileInfo>())).Returns(true);
        insp.Setup(i => i.IsOffline(It.IsAny<FileInfo>())).Returns(false);
        insp.Setup(i => i.IsRecallOnDataAccess(It.IsAny<FileInfo>())).Returns(false);
        
        var builder = CreateBuilder(insp.Object);
        
        var root = Directory.CreateDirectory(Path.Combine(_rootTemp, "root_dir_io"));
        var sub = Directory.CreateDirectory(Path.Combine(root.FullName, "BadSub"));
        
        // Throw IOException for this specific subdirectory when checking reparse
        insp.Setup(i => i.IsReparsePoint(It.Is<FileSystemInfo>(fsi => fsi.FullName == sub.FullName)))
            .Throws(new IOException("dir io error"));
        
        // Default to not reparse otherwise
        insp.Setup(i => i.IsReparsePoint(It.Is<FileSystemInfo>(fsi => fsi.FullName != sub.FullName)))
            .Returns(false);
        
        var okFile = Path.Combine(root.FullName, "ok.txt");
        await File.WriteAllTextAsync(okFile, "x");
        
        builder.AddInventoryPart(root.FullName);
        var invPath = Path.Combine(_rootTemp, "inv_dir_io.zip");
        await builder.BuildBaseInventoryAsync(invPath);
        
        var part = builder.Inventory.InventoryParts.Single();
        
        // An inaccessible directory entry should exist for BadSub
        part.DirectoryDescriptions.Any(d => d.RelativePath.EndsWith("/BadSub") && !d.IsAccessible).Should().BeTrue();
        
        // Root file still processed
        part.FileDescriptions.Any(f => f.RelativePath == "/ok.txt").Should().BeTrue();
    }
    
    [Test]
    public async Task Directory_ReparsePoint_Is_Skipped()
    {
        var insp = new Mock<IFileSystemInspector>(MockBehavior.Strict);
        insp.Setup(i => i.IsHidden(It.IsAny<FileSystemInfo>(), It.IsAny<OSPlatforms>())).Returns(false);
        insp.Setup(i => i.IsSystem(It.IsAny<FileInfo>())).Returns(false);
        insp.Setup(i => i.Exists(It.IsAny<FileInfo>())).Returns(true);
        insp.Setup(i => i.IsOffline(It.IsAny<FileInfo>())).Returns(false);
        insp.Setup(i => i.IsRecallOnDataAccess(It.IsAny<FileInfo>())).Returns(false);
        
        // Reparse only for the specific subdir path
        string? reparseDir = null;
        insp.Setup(i => i.IsReparsePoint(It.IsAny<FileSystemInfo>())).Returns<FileSystemInfo>(fsi => fsi.FullName == reparseDir);
        
        var builder = CreateBuilder(insp.Object);
        
        var root = Directory.CreateDirectory(Path.Combine(_rootTemp, "root2"));
        var sub = Directory.CreateDirectory(Path.Combine(root.FullName, "Sub"));
        reparseDir = sub.FullName;
        var filePath = Path.Combine(root.FullName, "ok.txt");
        await File.WriteAllTextAsync(filePath, "x");
        
        builder.AddInventoryPart(root.FullName);
        var invPath = Path.Combine(_rootTemp, "inv6.zip");
        await builder.BuildBaseInventoryAsync(invPath);
        
        var part = builder.Inventory.InventoryParts.Single();
        
        // Ensure Sub directory is skipped due to reparse (no directory description for it)
        part.DirectoryDescriptions.Any(d => d.RelativePath.EndsWith("/Sub")).Should().BeFalse();
        part.FileDescriptions.Should().ContainSingle();
        part.FileDescriptions[0].RelativePath.Should().Be("/ok.txt");
    }
}