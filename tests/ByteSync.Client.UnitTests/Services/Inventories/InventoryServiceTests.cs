using System.Reactive.Linq;
using ByteSync.Business;
using ByteSync.Business.DataNodes;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Business.Sessions;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Inventories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Inventories;

[TestFixture]
public class InventoryServiceTests
{
    private Mock<ISessionService> _sessionService = null!;
    private Mock<IInventoryFileRepository> _inventoryFileRepository = null!;
    private Mock<IDataNodeRepository> _dataNodeRepository = null!;
    private Mock<ILogger<InventoryService>> _logger = null!;

    private List<InventoryFile> _inventoryFiles = null!;

    [SetUp]
    public void Setup()
    {
        _sessionService = new Mock<ISessionService>();
        _sessionService.SetupGet(s => s.SessionStatusObservable)
            .Returns(Observable.Never<SessionStatus>());

        _inventoryFiles = new List<InventoryFile>();

        _inventoryFileRepository = new Mock<IInventoryFileRepository>();
        _inventoryFileRepository.SetupGet(r => r.Elements)
            .Returns(() => _inventoryFiles);
        _inventoryFileRepository
            .Setup(r => r.AddOrUpdate(It.IsAny<IEnumerable<InventoryFile>>()))
            .Callback<IEnumerable<InventoryFile>>(files =>
            {
                _inventoryFiles.Clear();
                _inventoryFiles.AddRange(files);
            });

        _dataNodeRepository = new Mock<IDataNodeRepository>();

        _logger = new Mock<ILogger<InventoryService>>();
    }

    private static DataNode Node(string id, string client, string code)
        => new DataNode { Id = id, ClientInstanceId = client, Code = code };

    private static InventoryFile FullInventory(string sessionId, string clientInstanceId, string code, string? fileName = null)
    {
        var sfd = new SharedFileDefinition
        {
            SessionId = sessionId,
            ClientInstanceId = clientInstanceId,
            SharedFileType = SharedFileTypes.FullInventory,
            AdditionalName = $"{code}_IID_test"
        };

        return new InventoryFile(sfd, fileName ?? $"{code}.zip");
    }

    [Test]
    public async Task AreFullInventoriesComplete_RequiresOnePerDataNode()
    {
        // Arrange
        var client1 = "client-1";
        var client2 = "client-2";

        var nodes = new List<DataNode>
        {
            Node("nAa", client1, "Aa"),
            Node("nBa", client2, "Ba"),
            Node("nBb", client2, "Bb")
        };

        _dataNodeRepository.SetupGet(r => r.Elements).Returns(nodes);

        var service = new InventoryService(
            _sessionService.Object,
            _inventoryFileRepository.Object,
            _dataNodeRepository.Object,
            _logger.Object);

        bool? last = null;
        using var sub = service.InventoryProcessData.AreFullInventoriesComplete.Subscribe(v => last = v);

        var filesStep1 = new List<InventoryFile>
        {
            FullInventory("S", client1, "Aa"),
            FullInventory("S", client2, "Ba"),
        };

        // Act 1: missing Bb
        await service.SetLocalInventory(filesStep1, LocalInventoryModes.Full);

        // Assert 1
        last.Should().BeFalse("Bb inventory is missing");

        var filesStep2 = new List<InventoryFile>(filesStep1)
        {
            FullInventory("S", client2, "Bb")
        };

        // Act 2: Bb arrives
        await service.SetLocalInventory(filesStep2, LocalInventoryModes.Full);

        // Assert 2
        last.Should().BeTrue("Aa, Ba and Bb inventories are present");
    }

    [Test]
    public async Task AreFullInventoriesComplete_MatchesByClientAndCodePrefix()
    {
        // Arrange
        var client2 = "client-2";
        var nodes = new List<DataNode>
        {
            Node("nBa", client2, "Ba"),
            Node("nBb", client2, "Bb")
        };
        _dataNodeRepository.SetupGet(r => r.Elements).Returns(nodes);

        var service = new InventoryService(
            _sessionService.Object,
            _inventoryFileRepository.Object,
            _dataNodeRepository.Object,
            _logger.Object);

        bool? last = null;
        using var sub = service.InventoryProcessData.AreFullInventoriesComplete.Subscribe(v => last = v);

        var wrongFiles = new List<InventoryFile>
        {
            FullInventory("S", client2, "Ba"),
            // Wrong: Bc_... does not match Bb node code
            FullInventory("S", client2, "Bc")
        };

        // Act 1: wrong prefix for second node
        await service.SetLocalInventory(wrongFiles, LocalInventoryModes.Full);

        // Assert 1
        last.Should().BeFalse("node Bb has no matching file with prefix 'Bb_'");

        var correctFiles = new List<InventoryFile>
        {
            FullInventory("S", client2, "Ba"),
            FullInventory("S", client2, "Bb")
        };

        // Act 2: provide correct Bb file
        await service.SetLocalInventory(correctFiles, LocalInventoryModes.Full);

        // Assert 2
        last.Should().BeTrue();
    }

    [Test]
    public async Task AreFullInventoriesComplete_CaseInsensitivePrefix()
    {
        // Arrange
        var client = "client";
        var nodes = new List<DataNode>
        {
            Node("nBb", client, "Bb")
        };
        _dataNodeRepository.SetupGet(r => r.Elements).Returns(nodes);

        var service = new InventoryService(
            _sessionService.Object,
            _inventoryFileRepository.Object,
            _dataNodeRepository.Object,
            _logger.Object);

        bool? full = null;
        using var sub = service.InventoryProcessData.AreFullInventoriesComplete.Subscribe(v => full = v);

        var files = new List<InventoryFile>
        {
            // Lowercase code in AdditionalName should match (case-insensitive)
            FullInventory("S", client, "bb")
        };

        await service.SetLocalInventory(files, LocalInventoryModes.Full);
        full.Should().BeTrue();
    }

    [Test]
    public async Task AreFullInventoriesComplete_NoDataNodes_True()
    {
        // Arrange
        _dataNodeRepository.SetupGet(r => r.Elements).Returns(new List<DataNode>());
        var service = new InventoryService(
            _sessionService.Object,
            _inventoryFileRepository.Object,
            _dataNodeRepository.Object,
            _logger.Object);

        bool? full = null;
        using var sub = service.InventoryProcessData.AreFullInventoriesComplete.Subscribe(v => full = v);

        await service.SetLocalInventory(new List<InventoryFile>(), LocalInventoryModes.Full);
        full.Should().BeTrue("with no DataNodes, completeness is trivially true");
    }

    [Test]
    public async Task AreBaseDoesNotAffectFull_Completeness()
    {
        // Arrange
        var client = "client";
        var nodes = new List<DataNode> { Node("nBa", client, "Ba") };
        _dataNodeRepository.SetupGet(r => r.Elements).Returns(nodes);

        var service = new InventoryService(
            _sessionService.Object,
            _inventoryFileRepository.Object,
            _dataNodeRepository.Object,
            _logger.Object);

        bool? baseComplete = null;
        bool? fullComplete = null;
        using var sub1 = service.InventoryProcessData.AreBaseInventoriesComplete.Subscribe(v => baseComplete = v);
        using var sub2 = service.InventoryProcessData.AreFullInventoriesComplete.Subscribe(v => fullComplete = v);

        // Only Base inventories present
        var baseSfd = new SharedFileDefinition
        {
            SessionId = "S",
            ClientInstanceId = client,
            SharedFileType = SharedFileTypes.BaseInventory,
            AdditionalName = "Ba_IID_test"
        };
        var baseFiles = new List<InventoryFile> { new InventoryFile(baseSfd, "base.zip") };

        await service.SetLocalInventory(baseFiles, LocalInventoryModes.Base);

        baseComplete.Should().BeTrue();
        fullComplete.Should().BeFalse("no Full inventories yet");
    }

    [Test]
    public async Task AreFullInventoriesComplete_IncorrectDelimiter_Fails()
    {
        var client = "client";
        var nodes = new List<DataNode> { Node("nBb", client, "Bb") };
        _dataNodeRepository.SetupGet(r => r.Elements).Returns(nodes);

        var service = new InventoryService(
            _sessionService.Object,
            _inventoryFileRepository.Object,
            _dataNodeRepository.Object,
            _logger.Object);

        bool? full = null;
        using var sub = service.InventoryProcessData.AreFullInventoriesComplete.Subscribe(v => full = v);

        // Hyphen instead of underscore -> should not match
        var wrong = new SharedFileDefinition
        {
            SessionId = "S",
            ClientInstanceId = client,
            SharedFileType = SharedFileTypes.FullInventory,
            AdditionalName = "Bb-IID_test"
        };
        await service.SetLocalInventory(new List<InventoryFile> { new InventoryFile(wrong, "wrong.zip") }, LocalInventoryModes.Full);
        full.Should().BeFalse();

        // Correct prefix now -> true
        await service.SetLocalInventory(new List<InventoryFile> { FullInventory("S", client, "Bb") }, LocalInventoryModes.Full);
        full.Should().BeTrue();
    }

    [Test]
    public async Task AreFullInventoriesComplete_MismatchedClientInstance_Fails()
    {
        var nodes = new List<DataNode> { Node("nBb", "client-A", "Bb") };
        _dataNodeRepository.SetupGet(r => r.Elements).Returns(nodes);

        var service = new InventoryService(
            _sessionService.Object,
            _inventoryFileRepository.Object,
            _dataNodeRepository.Object,
            _logger.Object);

        bool? full = null;
        using var sub = service.InventoryProcessData.AreFullInventoriesComplete.Subscribe(v => full = v);

        // File for another client -> should not match DataNode
        await service.SetLocalInventory(new List<InventoryFile> { FullInventory("S", "client-B", "Bb") }, LocalInventoryModes.Full);
        full.Should().BeFalse();

        // Correct client -> OK
        await service.SetLocalInventory(new List<InventoryFile> { FullInventory("S", "client-A", "Bb") }, LocalInventoryModes.Full);
        full.Should().BeTrue();
    }
}
