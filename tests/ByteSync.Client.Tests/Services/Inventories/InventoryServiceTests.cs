using System.Reactive.Subjects;
using ByteSync.Business;
using ByteSync.Business.Communications;
using ByteSync.Business.DataNodes;
using ByteSync.Business.Inventories;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Inventories;
using DynamicData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Inventories;

[TestFixture]
public class InventoryServiceTests
{
    private Mock<ISessionService> _sessionServiceMock = null!;
    private Mock<IInventoryFileRepository> _inventoryFileRepositoryMock = null!;
    private Mock<IDataNodeRepository> _dataNodeRepositoryMock = null!;
    private Mock<ILogger<InventoryService>> _loggerMock = null!;
    private InventoryService _inventoryService = null!;
    
    [SetUp]
    public void Setup()
    {
        _sessionServiceMock = new Mock<ISessionService>();
        _inventoryFileRepositoryMock = new Mock<IInventoryFileRepository>();
        _dataNodeRepositoryMock = new Mock<IDataNodeRepository>();
        _loggerMock = new Mock<ILogger<InventoryService>>();
        
        var sessionStatusSubject = new Subject<SessionStatus>();
        _sessionServiceMock.Setup(x => x.SessionStatusObservable).Returns(sessionStatusSubject);
        
        _inventoryService = new InventoryService(
            _sessionServiceMock.Object,
            _inventoryFileRepositoryMock.Object,
            _dataNodeRepositoryMock.Object,
            _loggerMock.Object);
    }
    
    [Test]
    public async Task CheckInventoriesReady_WhenAllDataNodesHaveInventories_ShouldSetCompleteToTrue()
    {
        // Arrange
        var dataNodes = new List<DataNode>
        {
            new() { Id = "node1", ClientInstanceId = "other_client_1", Code = "A" },
            new() { Id = "node2", ClientInstanceId = "other_client_2", Code = "B" },
            new() { Id = "node3", ClientInstanceId = "current_client_instance_id", Code = "C" }
        };
        
        var inventoryFiles = new List<InventoryFile>
        {
            CreateInventoryFile("other_client_1", "A", SharedFileTypes.BaseInventory),
            CreateInventoryFile("other_client_2", "B", SharedFileTypes.BaseInventory),
            CreateInventoryFile("current_client_instance_id", "C", SharedFileTypes.BaseInventory)
        };
        
        _dataNodeRepositoryMock.Setup(x => x.Elements).Returns(dataNodes);
        _inventoryFileRepositoryMock.Setup(x => x.Elements).Returns(inventoryFiles);
        
        bool? result = null;
        _inventoryService.InventoryProcessData.AreBaseInventoriesComplete
            .Subscribe(value => result = value);
        
        // Act
        await _inventoryService.SetLocalInventory(inventoryFiles, LocalInventoryModes.Base);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public async Task CheckInventoriesReady_WhenMissingInventoryForOtherDataNode_ShouldSetCompleteToFalse()
    {
        // Arrange
        var dataNodes = new List<DataNode>
        {
            new() { Id = "node1", ClientInstanceId = "other_client_1", Code = "A" },
            new() { Id = "node2", ClientInstanceId = "other_client_2", Code = "B" },
            new() { Id = "node3", ClientInstanceId = "current_client_instance_id", Code = "C" }
        };
        
        var inventoryFiles = new List<InventoryFile>
        {
            CreateInventoryFile("other_client_1", "A", SharedFileTypes.BaseInventory),
            
            // Missing inventory for other_client_2 with code "B"
            CreateInventoryFile("current_client_instance_id", "C", SharedFileTypes.BaseInventory)
        };
        
        _dataNodeRepositoryMock.Setup(x => x.Elements).Returns(dataNodes);
        _inventoryFileRepositoryMock.Setup(x => x.Elements).Returns(inventoryFiles);
        
        bool? result = null;
        _inventoryService.InventoryProcessData.AreBaseInventoriesComplete
            .Subscribe(value => result = value);
        
        // Act
        await _inventoryService.SetLocalInventory(inventoryFiles, LocalInventoryModes.Base);
        
        // Assert
        result.Should().BeFalse();
    }
    
    [Test]
    public async Task CheckInventoriesReady_WhenCurrentMemberHasNoInventory_ShouldSetCompleteToFalse()
    {
        // Arrange
        var dataNodes = new List<DataNode>
        {
            new() { Id = "node1", ClientInstanceId = "other_client_1", Code = "A" },
            new() { Id = "node2", ClientInstanceId = "current_client_instance_id", Code = "B" }
        };
        
        var inventoryFiles = new List<InventoryFile>
        {
            CreateInventoryFile("other_client_1", "A", SharedFileTypes.BaseInventory)
            
            // Missing inventory for current member
        };
        
        _dataNodeRepositoryMock.Setup(x => x.Elements).Returns(dataNodes);
        _inventoryFileRepositoryMock.Setup(x => x.Elements).Returns(inventoryFiles);
        
        bool? result = null;
        _inventoryService.InventoryProcessData.AreBaseInventoriesComplete
            .Subscribe(value => result = value);
        
        // Act
        await _inventoryService.SetLocalInventory(inventoryFiles, LocalInventoryModes.Base);
        
        // Assert
        result.Should().BeFalse();
    }
    
    [Test]
    public async Task CheckInventoriesReady_WhenAllDataNodesHaveInventoriesForCurrentMember_ShouldSetCompleteToTrue()
    {
        // Arrange
        var dataNodes = new List<DataNode>
        {
            new() { Id = "node1", ClientInstanceId = "current_client_instance_id", Code = "A" },
            new() { Id = "node2", ClientInstanceId = "current_client_instance_id", Code = "B" }
        };
        
        var inventoryFiles = new List<InventoryFile>
        {
            CreateInventoryFile("current_client_instance_id", "A", SharedFileTypes.BaseInventory),
            CreateInventoryFile("current_client_instance_id", "B", SharedFileTypes.BaseInventory)
        };
        
        _dataNodeRepositoryMock.Setup(x => x.Elements).Returns(dataNodes);
        _inventoryFileRepositoryMock.Setup(x => x.Elements).Returns(inventoryFiles);
        
        bool? result = null;
        _inventoryService.InventoryProcessData.AreBaseInventoriesComplete
            .Subscribe(value => result = value);
        
        // Act
        await _inventoryService.SetLocalInventory(inventoryFiles, LocalInventoryModes.Base);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public async Task CheckInventoriesReady_WhenFullInventoryMode_ShouldCheckFullInventories()
    {
        // Arrange
        var dataNodes = new List<DataNode>
        {
            new() { Id = "node1", ClientInstanceId = "other_client_1", Code = "A" },
            new() { Id = "node2", ClientInstanceId = "current_client_instance_id", Code = "B" }
        };
        
        var inventoryFiles = new List<InventoryFile>
        {
            CreateInventoryFile("other_client_1", "A", SharedFileTypes.FullInventory),
            CreateInventoryFile("current_client_instance_id", "B", SharedFileTypes.FullInventory)
        };
        
        _dataNodeRepositoryMock.Setup(x => x.Elements).Returns(dataNodes);
        _inventoryFileRepositoryMock.Setup(x => x.Elements).Returns(inventoryFiles);
        
        bool? result = null;
        _inventoryService.InventoryProcessData.AreFullInventoriesComplete
            .Subscribe(value => result = value);
        
        // Act
        await _inventoryService.SetLocalInventory(inventoryFiles, LocalInventoryModes.Full);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public async Task OnFileIsFullyDownloaded_WhenInventoryFile_ShouldCheckInventoriesReady()
    {
        // Arrange
        var dataNodes = new List<DataNode>
        {
            new() { Id = "node1", ClientInstanceId = "other_client_1", Code = "A" },
            new() { Id = "node2", ClientInstanceId = "current_client_instance_id", Code = "B" }
        };
        
        var inventoryFiles = new List<InventoryFile>
        {
            CreateInventoryFile("other_client_1", "A", SharedFileTypes.BaseInventory),
            CreateInventoryFile("current_client_instance_id", "B", SharedFileTypes.BaseInventory)
        };
        
        var localSharedFile = new LocalSharedFile(
            CreateSharedFileDefinition("other_client_1", "A", SharedFileTypes.BaseInventory),
            "test_path");
        
        _dataNodeRepositoryMock.Setup(x => x.Elements).Returns(dataNodes);
        _inventoryFileRepositoryMock.Setup(x => x.Elements).Returns(inventoryFiles);
        
        bool? result = null;
        _inventoryService.InventoryProcessData.AreBaseInventoriesComplete
            .Subscribe(value => result = value);
        
        // Act
        await _inventoryService.OnFileIsFullyDownloaded(localSharedFile);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public async Task CheckInventoriesReady_ShouldNotDependOnCodeProperty()
    {
        // Arrange
        var dataNodes = new List<DataNode>
        {
            new() { Id = "node1", ClientInstanceId = "other_client_1", Code = "X" },
            new() { Id = "node2", ClientInstanceId = "current_client_instance_id", Code = "Y" }
        };
        
        var inventoryFiles = new List<InventoryFile>
        {
            // Codes don't match, but ClientInstanceId does
            CreateInventoryFile("other_client_1", "DIFFERENT_CODE", SharedFileTypes.BaseInventory),
            CreateInventoryFile("current_client_instance_id", "ANOTHER_CODE", SharedFileTypes.BaseInventory)
        };
        
        _dataNodeRepositoryMock.Setup(x => x.Elements).Returns(dataNodes);
        _inventoryFileRepositoryMock.Setup(x => x.Elements).Returns(inventoryFiles);
        
        bool? result = null;
        _inventoryService.InventoryProcessData.AreBaseInventoriesComplete
            .Subscribe(value => result = value);
        
        // Act
        await _inventoryService.SetLocalInventory(inventoryFiles, LocalInventoryModes.Base);
        
        // Assert
        result.Should().BeTrue();
    }
    
    private static InventoryFile CreateInventoryFile(string clientInstanceId, string code, SharedFileTypes sharedFileType)
    {
        var sharedFileDefinition = CreateSharedFileDefinition(clientInstanceId, code, sharedFileType);
        
        return new InventoryFile(sharedFileDefinition, $"test_path_{clientInstanceId}_{code}");
    }
    
    private static SharedFileDefinition CreateSharedFileDefinition(string clientInstanceId, string code, SharedFileTypes sharedFileType)
    {
        return new SharedFileDefinition
        {
            ClientInstanceId = clientInstanceId,
            AdditionalName = $"{clientInstanceId}_{code}",
            SharedFileType = sharedFileType
        };
    }
    
    [Test]
    public async Task AbortInventory_SetsAbortionRequested_True()
    {
        bool? abortion = null;
        _inventoryService.InventoryProcessData.InventoryAbortionRequested.Subscribe(v => abortion = v);
        
        await _inventoryService.AbortInventory();
        
        abortion.Should().BeTrue();
    }
    
    [Test]
    public async Task OnFileIsFullyDownloaded_NonInventory_DoesNothing()
    {
        var nonInventory = new LocalSharedFile(
            CreateSharedFileDefinition("c1", "ANY", SharedFileTypes.FullSynchronization),
            "local_path");
        
        await _inventoryService.OnFileIsFullyDownloaded(nonInventory);
        
        _inventoryFileRepositoryMock.Verify(x => x.AddOrUpdate(It.IsAny<InventoryFile>()), Times.Never);
    }
    
    [Test]
    public async Task SetLocalInventory_AddsToRepository_AndUpdatesBothFlags()
    {
        var dataNodes = new List<DataNode>
        {
            new() { Id = "n1", ClientInstanceId = "c1" },
            new() { Id = "n2", ClientInstanceId = "c2" },
        };
        _dataNodeRepositoryMock.Setup(x => x.Elements).Returns(dataNodes);
        
        var files = new List<InventoryFile>
        {
            CreateInventoryFile("c1", "A", SharedFileTypes.BaseInventory),
            CreateInventoryFile("c2", "B", SharedFileTypes.BaseInventory),
            CreateInventoryFile("c1", "A", SharedFileTypes.FullInventory),
            CreateInventoryFile("c2", "B", SharedFileTypes.FullInventory),
        };
        _inventoryFileRepositoryMock.Setup(x => x.Elements).Returns(files);
        
        bool? baseReady = null, fullReady = null;
        _inventoryService.InventoryProcessData.AreBaseInventoriesComplete.Subscribe(v => baseReady = v);
        _inventoryService.InventoryProcessData.AreFullInventoriesComplete.Subscribe(v => fullReady = v);
        
        await _inventoryService.SetLocalInventory(files, LocalInventoryModes.Full);
        
        _inventoryFileRepositoryMock.Verify(x => x.AddOrUpdate(files), Times.Once);
        baseReady.Should().BeTrue();
        fullReady.Should().BeTrue();
    }
    
    [Test]
    public void Preparation_ResetsProcessData()
    {
        var sessionStatusSubject = new Subject<SessionStatus>();
        _sessionServiceMock.SetupGet(x => x.SessionStatusObservable).Returns(sessionStatusSubject);
        
        // Recreate service so it subscribes to this subject
        _inventoryService = new InventoryService(
            _sessionServiceMock.Object,
            _inventoryFileRepositoryMock.Object,
            _dataNodeRepositoryMock.Object,
            _loggerMock.Object);
        
        _inventoryService.InventoryProcessData.AreBaseInventoriesComplete.OnNext(true);
        _inventoryService.InventoryProcessData.GlobalMainStatus.OnNext(InventoryTaskStatus.Success);
        
        sessionStatusSubject.OnNext(SessionStatus.Preparation);
        
        bool? baseReady = null;
        _inventoryService.InventoryProcessData.AreBaseInventoriesComplete.Subscribe(v => baseReady = v);
        InventoryTaskStatus? global = null;
        _inventoryService.InventoryProcessData.GlobalMainStatus.Subscribe(v => global = v);
        
        baseReady.Should().BeFalse();
        global.Should().Be(InventoryTaskStatus.Pending);
    }
    
    [Test]
    public void AggregatedGlobalStatus_FollowsSessionMembers()
    {
        var changesSubject = new Subject<ISortedChangeSet<SessionMember, string>>();
        var sessionMemberRepoMock = new Mock<ISessionMemberRepository>();
        sessionMemberRepoMock.SetupGet(x => x.SortedSessionMembersObservable).Returns(changesSubject);
        
        // Recreate service with sessionMemberRepository to enable aggregation
        _inventoryService = new InventoryService(
            _sessionServiceMock.Object,
            _inventoryFileRepositoryMock.Object,
            _dataNodeRepositoryMock.Object,
            sessionMemberRepoMock.Object,
            _loggerMock.Object);
        
        InventoryTaskStatus? observed = null;
        _inventoryService.InventoryProcessData.GlobalMainStatus.Subscribe(s => observed = s);
        
        // Running dominates
        var runningList = new List<KeyValuePair<string, SessionMember>>
        {
            new("a",
                new SessionMember
                {
                    SessionMemberGeneralStatus = SessionMemberGeneralStatus.InventoryRunningIdentification,
                    Endpoint = new ByteSyncEndpoint()
                }),
            new("b",
                new SessionMember
                    { SessionMemberGeneralStatus = SessionMemberGeneralStatus.InventoryCancelled, Endpoint = new ByteSyncEndpoint() }),
        };
        var runningKvc = new Mock<IKeyValueCollection<SessionMember, string>>();
        runningKvc.As<IEnumerable<KeyValuePair<string, SessionMember>>>()
            .Setup(m => m.GetEnumerator())
            .Returns(() => runningList.GetEnumerator());
        var runningChange = new Mock<ISortedChangeSet<SessionMember, string>>();
        runningChange.SetupGet(c => c.SortedItems).Returns(runningKvc.Object);
        changesSubject.OnNext(runningChange.Object);
        observed.Should().Be(InventoryTaskStatus.Running);
        
        // Pending when no running but pending present even with cancelled
        var pendingList = new List<KeyValuePair<string, SessionMember>>
        {
            new("a",
                new SessionMember
                {
                    SessionMemberGeneralStatus = SessionMemberGeneralStatus.InventoryWaitingForStart, Endpoint = new ByteSyncEndpoint()
                }),
            new("b",
                new SessionMember
                    { SessionMemberGeneralStatus = SessionMemberGeneralStatus.InventoryCancelled, Endpoint = new ByteSyncEndpoint() }),
        };
        var pendingKvc = new Mock<IKeyValueCollection<SessionMember, string>>();
        pendingKvc.As<IEnumerable<KeyValuePair<string, SessionMember>>>()
            .Setup(m => m.GetEnumerator())
            .Returns(() => pendingList.GetEnumerator());
        var pendingChange = new Mock<ISortedChangeSet<SessionMember, string>>();
        pendingChange.SetupGet(c => c.SortedItems).Returns(pendingKvc.Object);
        changesSubject.OnNext(pendingChange.Object);
        observed.Should().Be(InventoryTaskStatus.Pending);
        
        // Success when only finished
        var successList = new List<KeyValuePair<string, SessionMember>>
        {
            new("a",
                new SessionMember
                    { SessionMemberGeneralStatus = SessionMemberGeneralStatus.InventoryFinished, Endpoint = new ByteSyncEndpoint() }),
            new("b",
                new SessionMember
                    { SessionMemberGeneralStatus = SessionMemberGeneralStatus.InventoryFinished, Endpoint = new ByteSyncEndpoint() }),
        };
        var successKvc = new Mock<IKeyValueCollection<SessionMember, string>>();
        successKvc.As<IEnumerable<KeyValuePair<string, SessionMember>>>()
            .Setup(m => m.GetEnumerator())
            .Returns(() => successList.GetEnumerator());
        var successChange = new Mock<ISortedChangeSet<SessionMember, string>>();
        successChange.SetupGet(c => c.SortedItems).Returns(successKvc.Object);
        changesSubject.OnNext(successChange.Object);
        observed.Should().Be(InventoryTaskStatus.Success);
    }
}