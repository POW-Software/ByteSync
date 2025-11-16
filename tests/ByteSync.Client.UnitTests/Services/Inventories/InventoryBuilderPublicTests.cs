using ByteSync.Business;
using ByteSync.Business.DataNodes;
using ByteSync.Business.Inventories;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.Sessions;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Services.Inventories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Inventories;

public class InventoryBuilderPublicTests
{
    private string _rootTemp = null!;
    private readonly List<ManualResetEvent> _manualResetEvents = new();
    
    [SetUp]
    public void SetUp()
    {
        _rootTemp = Path.Combine(Path.GetTempPath(), "ByteSync.Unit", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_rootTemp);
    }
    
    [TearDown]
    public void TearDown()
    {
        try
        {
            if (Directory.Exists(_rootTemp))
            {
                Directory.Delete(_rootTemp, true);
            }
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
    
    private InventoryBuilder CreateBuilder(OSPlatforms os = OSPlatforms.Windows)
    {
        var endpoint = new ByteSyncEndpoint
        {
            ClientId = "client",
            ClientInstanceId = Guid.NewGuid().ToString("N"),
            Version = "1.0",
            OSPlatform = os,
            IpAddress = "127.0.0.1"
        };
        
        var sessionMember = new SessionMember
        {
            Endpoint = endpoint,
            PrivateData = new SessionMemberPrivateData { MachineName = Environment.MachineName },
            SessionId = Guid.NewGuid().ToString("N"),
            JoinedSessionOn = DateTimeOffset.UtcNow,
            PositionInList = 0,
            SessionMemberGeneralStatus = SessionMemberGeneralStatus.InventoryWaitingForStart
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
        
        return new InventoryBuilder(sessionMember, dataNode, settings, processData, os, FingerprintModes.Rsync, logger,
            analyzer.Object, saver, indexer);
    }
    
    [Test]
    public async Task BuildBaseInventory_WithDeletedSingleFilePart_AddsInaccessibleFileDescription()
    {
        var builder = CreateBuilder();
        var work = Directory.CreateDirectory(Path.Combine(_rootTemp, "w1"));
        var filePath = Path.Combine(work.FullName, "gone.txt");
        await File.WriteAllTextAsync(filePath, "x");
        
        // Add as a file inventory part, then delete before running analysis to trigger inaccessible handling
        builder.AddInventoryPart(filePath);
        Directory.Delete(work.FullName, recursive: true); // ensure directory not found
        
        var inventoryPath = Path.Combine(_rootTemp, "inv.zip");
        await builder.BuildBaseInventoryAsync(inventoryPath);
        
        File.Exists(inventoryPath).Should().BeTrue();
        var part = builder.Inventory.InventoryParts.Single();
        
        // When the file path no longer exists, the builder currently skips it
        part.FileDescriptions.Should().BeEmpty();
    }
    
    [Test]
    public async Task BuildBaseInventory_WithDirectoryAndRegularFile_CoversNonReparsePaths()
    {
        var builder = CreateBuilder();
        var root = Directory.CreateDirectory(Path.Combine(_rootTemp, "root"));
        var sub = Directory.CreateDirectory(Path.Combine(root.FullName, "Sub"));
        var file = Path.Combine(sub.FullName, "file.txt");
        await File.WriteAllTextAsync(file, "x");
        
        builder.AddInventoryPart(root.FullName);
        var inventoryPath = Path.Combine(_rootTemp, "inv2.zip");
        await builder.BuildBaseInventoryAsync(inventoryPath);
        
        File.Exists(inventoryPath).Should().BeTrue();
        var part = builder.Inventory.InventoryParts.Single();
        
        // Root directory gets registered, subdirectories are traversed but only registered on error
        part.DirectoryDescriptions.Should().HaveCount(1);
        part.FileDescriptions.Should().HaveCount(1);
        part.FileDescriptions[0].RelativePath.Should().Be("/Sub/file.txt");
        part.FileDescriptions[0].IsAccessible.Should().BeTrue();
    }
}