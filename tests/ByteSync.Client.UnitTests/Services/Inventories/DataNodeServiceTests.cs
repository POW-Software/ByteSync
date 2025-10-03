using System.Reactive.Subjects;
using ByteSync.Business.DataNodes;
using ByteSync.Business.DataSources;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Inventories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Inventories;

[TestFixture]
public class DataNodeServiceTests
{
    private Mock<ISessionService> _sessionServiceMock = null!;
    private Mock<IConnectionService> _connectionServiceMock = null!;
    private Mock<IInventoryApiClient> _inventoryApiClientMock = null!;
    private Mock<IDataEncrypter> _dataEncrypterMock = null!;
    private Mock<IDataNodeRepository> _dataNodeRepositoryMock = null!;
    private Mock<IDataNodeCodeGenerator> _codeGeneratorMock = null!;
    private Mock<IDataSourceService> _dataSourceServiceMock = null!;
    private Mock<IDataSourceRepository> _dataSourceRepositoryMock = null!;
    private Mock<ILogger<DataNodeService>> _loggerMock = null!;
    private DataNodeService _service = null!;
    private BehaviorSubject<AbstractSession?> _sessionSubject = null!;
    private BehaviorSubject<SessionStatus> _sessionStatusSubject = null!;
    
    [SetUp]
    public void Setup()
    {
        _sessionServiceMock = new Mock<ISessionService>();
        _connectionServiceMock = new Mock<IConnectionService>();
        _inventoryApiClientMock = new Mock<IInventoryApiClient>();
        _dataEncrypterMock = new Mock<IDataEncrypter>();
        _dataNodeRepositoryMock = new Mock<IDataNodeRepository>();
        _codeGeneratorMock = new Mock<IDataNodeCodeGenerator>();
        _dataSourceServiceMock = new Mock<IDataSourceService>();
        _dataSourceRepositoryMock = new Mock<IDataSourceRepository>();
        _loggerMock = new Mock<ILogger<DataNodeService>>();
        
        // Setup observables
        _sessionSubject = new BehaviorSubject<AbstractSession?>(null);
        _sessionStatusSubject = new BehaviorSubject<SessionStatus>(SessionStatus.None);
        
        _sessionServiceMock.SetupGet(s => s.SessionObservable).Returns(_sessionSubject);
        _sessionServiceMock.SetupGet(s => s.SessionStatusObservable).Returns(_sessionStatusSubject);
        
        _service = new DataNodeService(_sessionServiceMock.Object,
            _connectionServiceMock.Object,
            _dataEncrypterMock.Object,
            _inventoryApiClientMock.Object,
            _dataNodeRepositoryMock.Object,
            _codeGeneratorMock.Object,
            _dataSourceServiceMock.Object,
            _dataSourceRepositoryMock.Object,
            _loggerMock.Object);
    }
    
    [TearDown]
    public void TearDown()
    {
        _sessionSubject?.Dispose();
        _sessionStatusSubject?.Dispose();
    }
    
    [Test]
    public async Task TryAddDataNode_CallsApiAndAdds_WhenCloudSessionAndClientMatches()
    {
        var sessionId = "SID";
        _sessionServiceMock.SetupGet(s => s.CurrentSession)
            .Returns(new CloudSession { SessionId = sessionId });
        _connectionServiceMock.SetupGet(c => c.ClientInstanceId).Returns("CID");
        var node = new DataNode { Id = "N1", ClientInstanceId = "CID" };
        var encrypted = new EncryptedDataNode();
        _dataEncrypterMock.Setup(e => e.EncryptDataNode(node)).Returns(encrypted);
        _inventoryApiClientMock.Setup(a => a.AddDataNode(sessionId, "CID", encrypted))
            .ReturnsAsync(true);
        
        var result = await _service.TryAddDataNode(node);
        
        result.Should().BeTrue();
        _inventoryApiClientMock.Verify(a => a.AddDataNode(sessionId, "CID", encrypted), Times.Once);
        _dataNodeRepositoryMock.Verify(r => r.AddOrUpdate(node), Times.Once);
        _codeGeneratorMock.Verify(g => g.RecomputeCodes(), Times.Once);
    }
    
    [Test]
    public async Task TryAddDataNode_SkipsApi_WhenClientDiffers()
    {
        var sessionId = "SID";
        _sessionServiceMock.SetupGet(s => s.CurrentSession)
            .Returns(new CloudSession { SessionId = sessionId });
        _connectionServiceMock.SetupGet(c => c.ClientInstanceId).Returns("CID");
        var node = new DataNode { Id = "N1", ClientInstanceId = "OTHER" };
        
        var result = await _service.TryAddDataNode(node);
        
        result.Should().BeTrue();
        _inventoryApiClientMock.Verify(a => a.AddDataNode(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EncryptedDataNode>()),
            Times.Never);
        _dataNodeRepositoryMock.Verify(r => r.AddOrUpdate(node), Times.Once);
        _codeGeneratorMock.Verify(g => g.RecomputeCodes(), Times.Once);
    }
    
    [Test]
    public async Task TryAddDataNode_DoesNotAdd_WhenApiFails()
    {
        var sessionId = "SID";
        _sessionServiceMock.SetupGet(s => s.CurrentSession)
            .Returns(new CloudSession { SessionId = sessionId });
        _connectionServiceMock.SetupGet(c => c.ClientInstanceId).Returns("CID");
        var node = new DataNode { Id = "N1", ClientInstanceId = "CID" };
        var encrypted = new EncryptedDataNode();
        _dataEncrypterMock.Setup(e => e.EncryptDataNode(node)).Returns(encrypted);
        _inventoryApiClientMock.Setup(a => a.AddDataNode(sessionId, "CID", encrypted))
            .ReturnsAsync(false);
        
        var result = await _service.TryAddDataNode(node);
        
        result.Should().BeFalse();
        _dataNodeRepositoryMock.Verify(r => r.AddOrUpdate(It.IsAny<DataNode>()), Times.Never);
        _codeGeneratorMock.Verify(g => g.RecomputeCodes(), Times.Never);
    }
    
    [Test]
    public async Task TryRemoveDataNode_CallsApiAndRemoves_WhenCloudSessionAndClientMatches()
    {
        var sessionId = "SID";
        _sessionServiceMock.SetupGet(s => s.CurrentSession)
            .Returns(new CloudSession { SessionId = sessionId });
        _connectionServiceMock.SetupGet(c => c.ClientInstanceId).Returns("CID");
        var node = new DataNode { Id = "N1", ClientInstanceId = "CID" };
        var dataSource1 = new DataSource { Id = "DS1", DataNodeId = "N1" };
        var dataSource2 = new DataSource { Id = "DS2", DataNodeId = "N1" };
        var dataSources = new List<DataSource> { dataSource1, dataSource2 };
        
        _dataSourceRepositoryMock.SetupGet(r => r.Elements).Returns(dataSources);
        _inventoryApiClientMock.Setup(a => a.RemoveDataNode(sessionId, "CID", It.IsAny<EncryptedDataNode>()))
            .ReturnsAsync(true);
        
        var result = await _service.TryRemoveDataNode(node);
        
        result.Should().BeTrue();
        _inventoryApiClientMock.Verify(a => a.RemoveDataNode(sessionId, "CID", It.IsAny<EncryptedDataNode>()), Times.Once);
        _dataNodeRepositoryMock.Verify(r => r.Remove(node), Times.Once);
        _codeGeneratorMock.Verify(g => g.RecomputeCodes(), Times.Once);
        _dataSourceServiceMock.Verify(s => s.ApplyRemoveDataSourceLocally(It.IsAny<DataSource>()), Times.Exactly(2));
    }
    
    [Test]
    public async Task TryRemoveDataNode_SkipsApi_WhenClientDiffers()
    {
        var sessionId = "SID";
        _sessionServiceMock.SetupGet(s => s.CurrentSession)
            .Returns(new CloudSession { SessionId = sessionId });
        _connectionServiceMock.SetupGet(c => c.ClientInstanceId).Returns("CID");
        var node = new DataNode { Id = "N1", ClientInstanceId = "OTHER" };
        var dataSource = new DataSource { Id = "DS1", DataNodeId = "N1" };
        var dataSources = new List<DataSource> { dataSource };
        
        _dataSourceRepositoryMock.SetupGet(r => r.Elements).Returns(dataSources);
        
        var result = await _service.TryRemoveDataNode(node);
        
        result.Should().BeTrue();
        _inventoryApiClientMock.Verify(a => a.RemoveDataNode(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EncryptedDataNode>()),
            Times.Never);
        _dataNodeRepositoryMock.Verify(r => r.Remove(node), Times.Once);
        _codeGeneratorMock.Verify(g => g.RecomputeCodes(), Times.Once);
        _dataSourceServiceMock.Verify(s => s.ApplyRemoveDataSourceLocally(dataSource), Times.Once);
    }
    
    [Test]
    public async Task TryRemoveDataNode_DoesNotRemove_WhenApiFails()
    {
        var sessionId = "SID";
        _sessionServiceMock.SetupGet(s => s.CurrentSession)
            .Returns(new CloudSession { SessionId = sessionId });
        _connectionServiceMock.SetupGet(c => c.ClientInstanceId).Returns("CID");
        var node = new DataNode { Id = "N1", ClientInstanceId = "CID" };
        var dataSource = new DataSource { Id = "DS1", DataNodeId = "N1" };
        var dataSources = new List<DataSource> { dataSource };
        
        _dataSourceRepositoryMock.SetupGet(r => r.Elements).Returns(dataSources);
        _inventoryApiClientMock.Setup(a => a.RemoveDataNode(sessionId, "CID", It.IsAny<EncryptedDataNode>()))
            .ReturnsAsync(false);
        
        var result = await _service.TryRemoveDataNode(node);
        
        result.Should().BeFalse();
        _dataNodeRepositoryMock.Verify(r => r.Remove(It.IsAny<DataNode>()), Times.Never);
        _dataSourceServiceMock.Verify(s => s.ApplyRemoveDataSourceLocally(It.IsAny<DataSource>()), Times.Never);
        _codeGeneratorMock.Verify(g => g.RecomputeCodes(), Times.Never);
    }
    
    [Test]
    public async Task TryRemoveDataNode_RemovesDataSourcesInCorrectOrder_WhenDataSourcesExist()
    {
        var sessionId = "SID";
        _sessionServiceMock.SetupGet(s => s.CurrentSession)
            .Returns(new CloudSession { SessionId = sessionId });
        _connectionServiceMock.SetupGet(c => c.ClientInstanceId).Returns("CID");
        var node = new DataNode { Id = "N1", ClientInstanceId = "CID" };
        var dataSource1 = new DataSource { Id = "DS1", DataNodeId = "N1" };
        var dataSource2 = new DataSource { Id = "DS2", DataNodeId = "N1" };
        var dataSources = new List<DataSource> { dataSource1, dataSource2 };
        
        _dataSourceRepositoryMock.SetupGet(r => r.Elements).Returns(dataSources);
        _inventoryApiClientMock.Setup(a => a.RemoveDataNode(sessionId, "CID", It.IsAny<EncryptedDataNode>()))
            .ReturnsAsync(true);
        
        var result = await _service.TryRemoveDataNode(node);
        
        result.Should().BeTrue();
        
        // Verify that DataNode is removed first, then DataSources
        _dataNodeRepositoryMock.Verify(r => r.Remove(node), Times.Once);
        _dataSourceServiceMock.Verify(s => s.ApplyRemoveDataSourceLocally(It.IsAny<DataSource>()), Times.Exactly(2));
        _codeGeneratorMock.Verify(g => g.RecomputeCodes(), Times.Once);
    }
    
    [Test]
    public async Task TryRemoveDataNode_OnlyRemovesAssociatedDataSources_WhenMultipleDataSourcesExist()
    {
        var sessionId = "SID";
        _sessionServiceMock.SetupGet(s => s.CurrentSession)
            .Returns(new CloudSession { SessionId = sessionId });
        _connectionServiceMock.SetupGet(c => c.ClientInstanceId).Returns("CID");
        var node = new DataNode { Id = "N1", ClientInstanceId = "CID" };
        var associatedDataSource1 = new DataSource { Id = "DS1", DataNodeId = "N1" };
        var associatedDataSource2 = new DataSource { Id = "DS2", DataNodeId = "N1" };
        var unrelatedDataSource = new DataSource { Id = "DS3", DataNodeId = "N2" }; // Different DataNode
        var dataSources = new List<DataSource> { associatedDataSource1, associatedDataSource2, unrelatedDataSource };
        
        _dataSourceRepositoryMock.SetupGet(r => r.Elements).Returns(dataSources);
        _inventoryApiClientMock.Setup(a => a.RemoveDataNode(sessionId, "CID", It.IsAny<EncryptedDataNode>()))
            .ReturnsAsync(true);
        
        var result = await _service.TryRemoveDataNode(node);
        
        result.Should().BeTrue();
        
        // Verify only associated DataSources are removed (2 calls for associated DataSources)
        _dataSourceServiceMock.Verify(s => s.ApplyRemoveDataSourceLocally(It.IsAny<DataSource>()), Times.Exactly(2));
        _dataSourceServiceMock.Verify(s => s.ApplyRemoveDataSourceLocally(unrelatedDataSource), Times.Never);
    }
    
    [Test]
    public async Task TryRemoveDataNode_SucceedsWithoutDataSources_WhenNoDataSourcesExist()
    {
        var sessionId = "SID";
        _sessionServiceMock.SetupGet(s => s.CurrentSession)
            .Returns(new CloudSession { SessionId = sessionId });
        _connectionServiceMock.SetupGet(c => c.ClientInstanceId).Returns("CID");
        var node = new DataNode { Id = "N1", ClientInstanceId = "CID" };
        var dataSources = new List<DataSource>(); // Empty list
        
        _dataSourceRepositoryMock.SetupGet(r => r.Elements).Returns(dataSources);
        _inventoryApiClientMock.Setup(a => a.RemoveDataNode(sessionId, "CID", It.IsAny<EncryptedDataNode>()))
            .ReturnsAsync(true);
        
        var result = await _service.TryRemoveDataNode(node);
        
        result.Should().BeTrue();
        
        // Verify DataNode is removed and no DataSource operations are called
        _dataNodeRepositoryMock.Verify(r => r.Remove(node), Times.Once);
        _dataSourceServiceMock.Verify(s => s.ApplyRemoveDataSourceLocally(It.IsAny<DataSource>()), Times.Never);
        _codeGeneratorMock.Verify(g => g.RecomputeCodes(), Times.Once);
    }
    
    [Test]
    public async Task CreateAndTryAddDataNode_ShouldCreateNodeWithClientInstance()
    {
        var sessionId = "SID";
        _sessionServiceMock.SetupGet(s => s.CurrentSession)
            .Returns(new CloudSession { SessionId = sessionId });
        _connectionServiceMock.SetupGet(c => c.ClientInstanceId).Returns("CID");
        var encrypted = new EncryptedDataNode();
        _dataEncrypterMock.Setup(e => e.EncryptDataNode(It.Is<DataNode>(n => n.Id == "NODE" && n.ClientInstanceId == "CID")))
            .Returns(encrypted);
        _inventoryApiClientMock.Setup(a => a.AddDataNode(sessionId, "CID", encrypted))
            .ReturnsAsync(true);
        
        await _service.CreateAndTryAddDataNode("NODE");
        
        _inventoryApiClientMock.Verify(a => a.AddDataNode(sessionId, "CID", encrypted), Times.Once);
        _dataNodeRepositoryMock.Verify(r => r.AddOrUpdate(It.Is<DataNode>(n => n.Id == "NODE" && n.ClientInstanceId == "CID")), Times.Once);
        _codeGeneratorMock.Verify(g => g.RecomputeCodes(), Times.Once);
    }
}