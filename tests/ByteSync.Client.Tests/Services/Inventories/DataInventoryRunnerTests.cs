using System.Reactive.Linq;
using System.Reactive.Subjects;
using ByteSync.Business.DataNodes;
using ByteSync.Business.Inventories;
using ByteSync.Business.Misc;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Sessions;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.TimeTracking;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Inventories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Inventories;

[TestFixture]
public class DataInventoryRunnerTests
{
    private InventoryProcessData _processData = null!;
    private Mock<ISessionService> _sessionService = null!;
    private Mock<IInventoryService> _inventoryService = null!;
    private Mock<ITimeTrackingCache> _timeTrackingCache = null!;
    private Mock<ITimeTrackingComputer> _timeTrackingComputer = null!;
    private Mock<ISessionMemberService> _sessionMemberService = null!;
    private Mock<IInventoryBuilderFactory> _inventoryBuilderFactory = null!;
    private Mock<IBaseInventoryRunner> _baseRunner = null!;
    private Mock<IFullInventoryRunner> _fullRunner = null!;
    private Mock<IDataNodeRepository> _dataNodeRepository = null!;
    private Mock<ILogger<DataInventoryRunner>> _logger = null!;
    
    private Subject<SessionStatus> _sessionStatusSubject = null!;
    
    [SetUp]
    public void Setup()
    {
        _processData = new InventoryProcessData();
        
        _sessionService = new Mock<ISessionService>();
        _inventoryService = new Mock<IInventoryService>();
        _timeTrackingCache = new Mock<ITimeTrackingCache>();
        _timeTrackingComputer = new Mock<ITimeTrackingComputer>();
        _sessionMemberService = new Mock<ISessionMemberService>();
        _inventoryBuilderFactory = new Mock<IInventoryBuilderFactory>();
        _baseRunner = new Mock<IBaseInventoryRunner>();
        _fullRunner = new Mock<IFullInventoryRunner>();
        _dataNodeRepository = new Mock<IDataNodeRepository>();
        _logger = new Mock<ILogger<DataInventoryRunner>>();
        
        _inventoryService.SetupGet(x => x.InventoryProcessData).Returns(_processData);
        
        _sessionStatusSubject = new Subject<SessionStatus>();
        _sessionService.SetupGet(x => x.SessionStatusObservable).Returns(_sessionStatusSubject.AsObservable());
        _sessionService.SetupGet(x => x.SessionEnded).Returns(Observable.Return(false));
        _sessionService.SetupGet(x => x.SessionId).Returns("session-123");
        _sessionService.Setup(x => x.SetSessionStatus(It.IsAny<SessionStatus>()))
            .Returns(Task.CompletedTask);
        
        _timeTrackingCache.Setup(x => x.GetTimeTrackingComputer("session-123", TimeTrackingComputerType.Inventory))
            .ReturnsAsync(_timeTrackingComputer.Object);
        
        _baseRunner.Setup(x => x.RunBaseInventory()).ReturnsAsync(true);
        _fullRunner.Setup(x => x.RunFullInventory()).ReturnsAsync(true);
    }
    
    private DataInventoryRunner CreateSut()
    {
        return new DataInventoryRunner(
            _sessionService.Object,
            _inventoryService.Object,
            _timeTrackingCache.Object,
            _sessionMemberService.Object,
            _inventoryBuilderFactory.Object,
            _baseRunner.Object,
            _fullRunner.Object,
            _dataNodeRepository.Object,
            _logger.Object);
    }
    
    [Test]
    public async Task RunDataInventory_SuccessFlow_StartsTimer_BuildsInventories_AndWaitsForCompletion()
    {
        var nodeA = new DataNode { Id = "nA", ClientInstanceId = "c1", OrderIndex = 2 };
        var nodeB = new DataNode { Id = "nB", ClientInstanceId = "c2", OrderIndex = 1 };
        _dataNodeRepository.SetupGet(x => x.SortedCurrentMemberDataNodes).Returns(new[] { nodeA, nodeB });
        
        var builderA = new Mock<IInventoryBuilder>().Object;
        var builderB = new Mock<IInventoryBuilder>().Object;
        
        // Ensure order by OrderIndex
        var seq = new MockSequence();
        _inventoryBuilderFactory.InSequence(seq).Setup(x => x.CreateInventoryBuilder(nodeB)).Returns(builderB);
        _inventoryBuilderFactory.InSequence(seq).Setup(x => x.CreateInventoryBuilder(nodeA)).Returns(builderA);
        
        var sut = CreateSut();
        
        var before = DateTimeOffset.Now;
        var runTask = sut.RunDataInventory();
        
        // Initialization should set statuses
        InventoryTaskStatus? main = null, ident = null, analy = null;
        _processData.MainStatus.Subscribe(s => main = s);
        _processData.IdentificationStatus.Subscribe(s => ident = s);
        _processData.AnalysisStatus.Subscribe(s => analy = s);
        
        // Unblock base inventory stage
        _processData.AreBaseInventoriesComplete.OnNext(true);
        
        // Unblock full inventory stage
        _processData.AreFullInventoriesComplete.OnNext(true);
        
        await runTask;
        
        _sessionService.Verify(x => x.SetSessionStatus(SessionStatus.Inventory), Times.Once);
        
        main.Should().Be(InventoryTaskStatus.Running);
        ident.Should().Be(InventoryTaskStatus.Running);
        analy.Should().Be(InventoryTaskStatus.Pending);
        
        _timeTrackingComputer.Verify(x => x.Start(It.IsAny<DateTimeOffset>()), Times.Once);
        _processData.InventoryStart.Should().BeOnOrAfter(before.AddSeconds(-1));
        
        _inventoryBuilderFactory.Verify(x => x.CreateInventoryBuilder(It.IsAny<DataNode>()), Times.Exactly(2));
        _processData.InventoryBuilders.Should().HaveCount(2);
    }
    
    [Test]
    public async Task RunDataInventory_BaseCancelled_UpdatesStatusToCancelled()
    {
        _dataNodeRepository.SetupGet(x => x.SortedCurrentMemberDataNodes).Returns(Array.Empty<DataNode>());
        
        var sut = CreateSut();
        
        var run = sut.RunDataInventory();
        
        // Signal cancellation during base stage
        _processData.InventoryAbortionRequested.OnNext(true);
        _processData.AreBaseInventoriesComplete.OnNext(false);
        
        await run;
        
        _sessionMemberService.Verify(x => x.UpdateCurrentMemberGeneralStatus(SessionMemberGeneralStatus.InventoryCancelled), Times.Once);
        InventoryTaskStatus? main = null;
        _processData.MainStatus.Subscribe(s => main = s);
        main.Should().Be(InventoryTaskStatus.Cancelled);
    }
    
    [Test]
    public async Task RunDataInventory_BaseError_UpdatesStatusToError()
    {
        _dataNodeRepository.SetupGet(x => x.SortedCurrentMemberDataNodes).Returns(Array.Empty<DataNode>());
        
        var sut = CreateSut();
        var run = sut.RunDataInventory();
        
        // Signal error during base stage
        _processData.ErrorEvent.OnNext(true);
        _processData.AreBaseInventoriesComplete.OnNext(false);
        
        await run;
        
        _sessionMemberService.Verify(x => x.UpdateCurrentMemberGeneralStatus(SessionMemberGeneralStatus.InventoryError), Times.AtLeastOnce);
        InventoryTaskStatus? main = null;
        _processData.MainStatus.Subscribe(s => main = s);
        main.Should().Be(InventoryTaskStatus.Error);
    }
    
    [Test]
    public async Task RunDataInventory_InitializationThrows_SetsErrorAndSkipsTimerStart()
    {
        _dataNodeRepository.SetupGet(x => x.SortedCurrentMemberDataNodes).Throws(new InvalidOperationException("boom"));
        
        var sut = CreateSut();
        await sut.RunDataInventory();
        
        _sessionMemberService.Verify(x => x.UpdateCurrentMemberGeneralStatus(SessionMemberGeneralStatus.InventoryError), Times.AtLeast(2));
        _timeTrackingComputer.Verify(x => x.Start(It.IsAny<DateTimeOffset>()), Times.Never);
        _processData.MainStatus.FirstAsync().Wait().Should().Be(InventoryTaskStatus.Error);
        _processData.LastException.Should().NotBeNull();
    }
    
    [Test]
    public void Constructor_StopsTimer_OnMainStatusNotRunning()
    {
        var sut = CreateSut();
        
        _processData.MainStatus.OnNext(InventoryTaskStatus.Pending);
        
        _timeTrackingComputer.Verify(x => x.Stop(), Times.Once);
    }
    
    [Test]
    public void Constructor_StopsTimer_OnSessionPreparation()
    {
        var sut = CreateSut();
        _timeTrackingComputer.Invocations.Clear();
        _sessionStatusSubject.OnNext(SessionStatus.Preparation);
        _timeTrackingComputer.Verify(x => x.Stop(), Times.AtLeastOnce());
    }
    
    [Test]
    public async Task StartDataInventoryInitialization_LogsAndSkipsFaultyBuilder()
    {
        var nodeOk = new DataNode { Id = "ok", ClientInstanceId = "c1", OrderIndex = 1 };
        var nodeBad = new DataNode { Id = "bad", ClientInstanceId = "c2", OrderIndex = 2 };
        _dataNodeRepository.SetupGet(x => x.SortedCurrentMemberDataNodes).Returns(new[] { nodeOk, nodeBad });
        
        _inventoryBuilderFactory.Setup(x => x.CreateInventoryBuilder(nodeOk)).Returns(new Mock<IInventoryBuilder>().Object);
        _inventoryBuilderFactory.Setup(x => x.CreateInventoryBuilder(nodeBad)).Throws(new Exception("factory failed"));
        
        var sut = CreateSut();
        var run = sut.RunDataInventory();
        
        _processData.AreBaseInventoriesComplete.OnNext(true);
        _processData.AreFullInventoriesComplete.OnNext(true);
        
        await run;
        
        _processData.InventoryBuilders.Should().HaveCount(1);
    }
}