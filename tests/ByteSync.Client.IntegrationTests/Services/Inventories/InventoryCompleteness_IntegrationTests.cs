using Autofac;
using ByteSync.Business;
using ByteSync.Business.DataNodes;
using ByteSync.Business.Inventories;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Repositories;
using ByteSync.Services.Inventories;
using ByteSync.TestsCommon;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ByteSync.Client.IntegrationTests.Services.Inventories;

public class InventoryCompleteness_IntegrationTests : IntegrationTest
{
    private IInventoryService _inventoryService = null!;
    private IDataNodeRepository _dataNodeRepository = null!;
    
    [SetUp]
    public void Setup()
    {
        // Repositories and policies
        RegisterType<SessionInvalidationCachePolicy<InventoryFile, string>, ISessionInvalidationCachePolicy<InventoryFile, string>>();
        RegisterType<SessionInvalidationCachePolicy<DataNode, string>, ISessionInvalidationCachePolicy<DataNode, string>>();
        RegisterType<InventoryFileRepository, IInventoryFileRepository>();
        RegisterType<DataNodeRepository, IDataNodeRepository>();
        
        // InventoryService via explicit ctor without ISessionMemberRepository (avoids extra wiring)
        _builder.Register(ctx =>
        {
            var sessionService = ctx.Resolve<ISessionService>();
            var inventoryFileRepository = ctx.Resolve<IInventoryFileRepository>();
            var dataNodeRepository = ctx.Resolve<IDataNodeRepository>();
            var logger = ctx.Resolve<ILogger<InventoryService>>();
            
            return new InventoryService(sessionService, inventoryFileRepository, dataNodeRepository, logger);
        }).As<IInventoryService>().SingleInstance();
        
        BuildMoqContainer();
        
        // Session and environment setup
        var context = new TestContextGenerator(Container);
        context.GenerateSession();
        context.GenerateCurrentEndpoint();
        
        // Ensure a temp test directory exists
        _testDirectoryService.CreateTestDirectory();
        
        // Ensure ApplicationDataPath exists (used by various services if invoked)
        var mockEnvironment = Container.Resolve<Mock<IEnvironmentService>>();
        mockEnvironment.Setup(m => m.AssemblyFullName)
            .Returns(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "Assembly", "ByteSync.exe"));
        
        var mockLocalAppData = Container.Resolve<Mock<ILocalApplicationDataManager>>();
        mockLocalAppData.Setup(m => m.ApplicationDataPath)
            .Returns(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "AppData"));
        
        _inventoryService = Container.Resolve<IInventoryService>();
        _dataNodeRepository = Container.Resolve<IDataNodeRepository>();
    }
    
    private static DataNode Node(string id, string client, string code)
        => new DataNode { Id = id, ClientInstanceId = client, Code = code };
    
    private static InventoryFile FullInventory(string sessionId, string clientInstanceId, string code)
    {
        var sfd = new SharedFileDefinition
        {
            SessionId = sessionId,
            ClientInstanceId = clientInstanceId,
            SharedFileType = SharedFileTypes.FullInventory,
            AdditionalName = $"{code}_IID_test"
        };
        
        // The physical file is not accessed by InventoryService completeness; path can be arbitrary
        return new InventoryFile(sfd, $"{code}.zip");
    }
    
    [Test]
    public async Task Should_wait_for_one_full_inventory_per_data_node()
    {
        // Arrange
        var sessionId = Container.Resolve<ISessionService>().SessionId!;
        
        // Current member (from TestContextGenerator): CII_A
        var c1 = "CII_A";
        var c2 = "CII_B";
        
        _dataNodeRepository.AddOrUpdate([
            Node("nAa", c1, "Aa"),
            Node("nBa", c2, "Ba"),
            Node("nBb", c2, "Bb")
        ]);
        
        bool? full = null;
        using var sub = _inventoryService.InventoryProcessData.AreFullInventoriesComplete.Subscribe(v => full = v);
        
        // Act 1: provide Aa + Ba
        var step1 = new List<InventoryFile>
        {
            FullInventory(sessionId, c1, "Aa"),
            FullInventory(sessionId, c2, "Ba"),
        };
        await _inventoryService.SetLocalInventory(step1, LocalInventoryModes.Full);
        
        // Assert 1
        full.Should().BeFalse("Bb inventory is missing for member CII_B");
        
        // Act 2: add Bb
        var step2 = new List<InventoryFile>(step1)
        {
            FullInventory(sessionId, c2, "Bb")
        };
        await _inventoryService.SetLocalInventory(step2, LocalInventoryModes.Full);
        
        // Assert 2
        full.Should().BeTrue("all nodes Aa, Ba and Bb are present");
    }
    
    [Test]
    public async Task Case_insensitive_code_prefix_and_client_match_are_enforced()
    {
        var sessionId = Container.Resolve<ISessionService>().SessionId!;
        
        _dataNodeRepository.AddOrUpdate([Node("nBb", "CII_B", "Bb")]);
        
        bool? full = null;
        using var sub = _inventoryService.InventoryProcessData.AreFullInventoriesComplete.Subscribe(v => full = v);
        
        // wrong client
        await _inventoryService.SetLocalInventory([FullInventory(sessionId, "CII_X", "Bb")], LocalInventoryModes.Full);
        full.Should().BeFalse();
        
        // wrong delimiter
        var wrong = new SharedFileDefinition
        {
            SessionId = sessionId,
            ClientInstanceId = "CII_B",
            SharedFileType = SharedFileTypes.FullInventory,
            AdditionalName = "Bb-IID_test"
        };
        await _inventoryService.SetLocalInventory([new InventoryFile(wrong, "wrong.zip")], LocalInventoryModes.Full);
        full.Should().BeFalse();
        
        // lowercase prefix should still match
        await _inventoryService.SetLocalInventory([FullInventory(sessionId, "CII_B", "bb")], LocalInventoryModes.Full);
        full.Should().BeTrue();
    }
    
    [Test]
    public async Task No_data_nodes_trivially_reports_full_true()
    {
        bool? full = null;
        using var sub = _inventoryService.InventoryProcessData.AreFullInventoriesComplete.Subscribe(v => full = v);
        
        await _inventoryService.SetLocalInventory(Array.Empty<InventoryFile>(), LocalInventoryModes.Full);
        full.Should().BeTrue();
    }
}