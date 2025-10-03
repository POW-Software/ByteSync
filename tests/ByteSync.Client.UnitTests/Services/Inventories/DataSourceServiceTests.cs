using ByteSync.Business.DataNodes;
using ByteSync.Business.DataSources;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Interfaces;
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
public class DataSourceServiceTests
{
    private Mock<ISessionService> _sessionServiceMock = null!;
    private Mock<IDataSourceChecker> _dataSourceCheckerMock = null!;
    private Mock<IDataEncrypter> _dataEncrypterMock = null!;
    private Mock<IConnectionService> _connectionServiceMock = null!;
    private Mock<IInventoryApiClient> _inventoryApiClientMock = null!;
    private Mock<IDataSourceRepository> _dataSourceRepositoryMock = null!;
    private Mock<IDataSourceCodeGenerator> _codeGeneratorMock = null!;
    private Mock<ILogger<DataSourceService>> _loggerMock = null!;
    private DataSourceService _service = null!;
    
    [SetUp]
    public void Setup()
    {
        _sessionServiceMock = new Mock<ISessionService>();
        _dataSourceCheckerMock = new Mock<IDataSourceChecker>();
        _dataEncrypterMock = new Mock<IDataEncrypter>();
        _connectionServiceMock = new Mock<IConnectionService>();
        _inventoryApiClientMock = new Mock<IInventoryApiClient>();
        _dataSourceRepositoryMock = new Mock<IDataSourceRepository>();
        _codeGeneratorMock = new Mock<IDataSourceCodeGenerator>();
        _loggerMock = new Mock<ILogger<DataSourceService>>();
        
        _service = new DataSourceService(
            _sessionServiceMock.Object,
            _dataSourceCheckerMock.Object,
            _dataEncrypterMock.Object,
            _connectionServiceMock.Object,
            _inventoryApiClientMock.Object,
            _dataSourceRepositoryMock.Object,
            _codeGeneratorMock.Object,
            _loggerMock.Object);
    }
    
    [Test]
    public async Task TryAddDataSource_CallsApiAndAdds_WhenDataSourceCheckPassesAndCloudSessionAndClientMatches()
    {
        var sessionId = "SID";
        var dataSource = new DataSource { Id = "DS1", ClientInstanceId = "CID", DataNodeId = "N1", Path = "/test" };
        var encryptedDataSource = new EncryptedDataSource();
        var existingDataSources = new List<DataSource>();
        
        _dataSourceCheckerMock.Setup(c => c.CheckDataSource(dataSource, existingDataSources))
            .ReturnsAsync(true);
        _sessionServiceMock.SetupGet(s => s.CurrentSession)
            .Returns(new CloudSession { SessionId = sessionId });
        _connectionServiceMock.SetupGet(c => c.ClientInstanceId).Returns("CID");
        _dataEncrypterMock.Setup(e => e.EncryptDataSource(dataSource)).Returns(encryptedDataSource);
        _inventoryApiClientMock.Setup(a => a.AddDataSource(sessionId, "CID", "N1", encryptedDataSource))
            .ReturnsAsync(true);
        
        var result = await _service.TryAddDataSource(dataSource);
        
        result.Should().BeTrue();
        _inventoryApiClientMock.Verify(a => a.AddDataSource(sessionId, "CID", "N1", encryptedDataSource), Times.Once);
        _dataSourceRepositoryMock.Verify(r => r.AddOrUpdate(dataSource), Times.Once);
        _codeGeneratorMock.Verify(g => g.RecomputeCodesForNode("N1"), Times.Once);
    }
    
    [Test]
    public async Task TryAddDataSource_SkipsApi_WhenClientDiffers()
    {
        var sessionId = "SID";
        var dataSource = new DataSource { Id = "DS1", ClientInstanceId = "OTHER", DataNodeId = "N1", Path = "/test" };
        var existingDataSources = new List<DataSource>();
        
        _dataSourceCheckerMock.Setup(c => c.CheckDataSource(dataSource, existingDataSources))
            .ReturnsAsync(true);
        _sessionServiceMock.SetupGet(s => s.CurrentSession)
            .Returns(new CloudSession { SessionId = sessionId });
        _connectionServiceMock.SetupGet(c => c.ClientInstanceId).Returns("CID");
        
        var result = await _service.TryAddDataSource(dataSource);
        
        result.Should().BeTrue();
        _inventoryApiClientMock.Verify(
            a => a.AddDataSource(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EncryptedDataSource>()), Times.Never);
        _dataSourceRepositoryMock.Verify(r => r.AddOrUpdate(dataSource), Times.Once);
        _codeGeneratorMock.Verify(g => g.RecomputeCodesForNode("N1"), Times.Once);
    }
    
    [Test]
    public async Task TryAddDataSource_DoesNotAdd_WhenApiFails()
    {
        var sessionId = "SID";
        var dataSource = new DataSource { Id = "DS1", ClientInstanceId = "CID", DataNodeId = "N1", Path = "/test" };
        var encryptedDataSource = new EncryptedDataSource();
        var existingDataSources = new List<DataSource>();
        
        _dataSourceCheckerMock.Setup(c => c.CheckDataSource(dataSource, existingDataSources))
            .ReturnsAsync(true);
        _sessionServiceMock.SetupGet(s => s.CurrentSession)
            .Returns(new CloudSession { SessionId = sessionId });
        _connectionServiceMock.SetupGet(c => c.ClientInstanceId).Returns("CID");
        _dataEncrypterMock.Setup(e => e.EncryptDataSource(dataSource)).Returns(encryptedDataSource);
        _inventoryApiClientMock.Setup(a => a.AddDataSource(sessionId, "CID", "N1", encryptedDataSource))
            .ReturnsAsync(false);
        
        var result = await _service.TryAddDataSource(dataSource);
        
        result.Should().BeFalse();
        _dataSourceRepositoryMock.Verify(r => r.AddOrUpdate(It.IsAny<DataSource>()), Times.Never);
        _codeGeneratorMock.Verify(g => g.RecomputeCodesForNode(It.IsAny<string>()), Times.Never);
    }
    
    [Test]
    public async Task TryAddDataSource_ReturnsFalse_WhenDataSourceCheckFails()
    {
        var dataSource = new DataSource { Id = "DS1", ClientInstanceId = "CID", DataNodeId = "N1", Path = "/test" };
        var existingDataSources = new List<DataSource>();
        
        _dataSourceCheckerMock.Setup(c => c.CheckDataSource(dataSource, existingDataSources))
            .ReturnsAsync(false);
        
        var result = await _service.TryAddDataSource(dataSource);
        
        result.Should().BeFalse();
        _inventoryApiClientMock.Verify(
            a => a.AddDataSource(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EncryptedDataSource>()), Times.Never);
        _dataSourceRepositoryMock.Verify(r => r.AddOrUpdate(It.IsAny<DataSource>()), Times.Never);
        _codeGeneratorMock.Verify(g => g.RecomputeCodesForNode(It.IsAny<string>()), Times.Never);
    }
    
    [Test]
    public async Task TryRemoveDataSource_CallsApiAndRemoves_WhenSuccess()
    {
        var sessionId = "SID";
        var dataSource = new DataSource { Id = "DS1", ClientInstanceId = "CID", DataNodeId = "N1", Path = "/test" };
        var encryptedDataSource = new EncryptedDataSource();
        
        _sessionServiceMock.SetupGet(s => s.SessionId).Returns(sessionId);
        _connectionServiceMock.SetupGet(c => c.ClientInstanceId).Returns("CID");
        _dataEncrypterMock.Setup(e => e.EncryptDataSource(dataSource)).Returns(encryptedDataSource);
        _inventoryApiClientMock.Setup(a => a.RemoveDataSource(sessionId, "CID", "N1", encryptedDataSource))
            .ReturnsAsync(true);
        
        var result = await _service.TryRemoveDataSource(dataSource);
        
        result.Should().BeTrue();
        _inventoryApiClientMock.Verify(a => a.RemoveDataSource(sessionId, "CID", "N1", encryptedDataSource), Times.Once);
        _dataSourceRepositoryMock.Verify(r => r.Remove(dataSource), Times.Once);
        _codeGeneratorMock.Verify(g => g.RecomputeCodesForNode("N1"), Times.Once);
    }
    
    [Test]
    public async Task TryRemoveDataSource_DoesNotRemove_WhenApiFails()
    {
        var sessionId = "SID";
        var dataSource = new DataSource { Id = "DS1", ClientInstanceId = "CID", DataNodeId = "N1", Path = "/test" };
        var encryptedDataSource = new EncryptedDataSource();
        
        _sessionServiceMock.SetupGet(s => s.SessionId).Returns(sessionId);
        _connectionServiceMock.SetupGet(c => c.ClientInstanceId).Returns("CID");
        _dataEncrypterMock.Setup(e => e.EncryptDataSource(dataSource)).Returns(encryptedDataSource);
        _inventoryApiClientMock.Setup(a => a.RemoveDataSource(sessionId, "CID", "N1", encryptedDataSource))
            .ReturnsAsync(false);
        
        var result = await _service.TryRemoveDataSource(dataSource);
        
        result.Should().BeFalse();
        _dataSourceRepositoryMock.Verify(r => r.Remove(It.IsAny<DataSource>()), Times.Never);
        _codeGeneratorMock.Verify(g => g.RecomputeCodesForNode(It.IsAny<string>()), Times.Never);
    }
    
    [Test]
    public async Task CreateAndTryAddDataSource_ShouldCreateDataSourceWithCorrectProperties()
    {
        var sessionId = "SID";
        var path = "/test/path";
        var fileSystemType = FileSystemTypes.Directory;
        var dataNode = new DataNode { Id = "N1", ClientInstanceId = "CID" };
        var existingDataSources = new List<DataSource>();
        
        _sessionServiceMock.SetupGet(s => s.CurrentSession)
            .Returns(new CloudSession { SessionId = sessionId });
        _connectionServiceMock.SetupGet(c => c.ClientInstanceId).Returns("CID");
        _dataSourceCheckerMock.Setup(c => c.CheckDataSource(It.IsAny<DataSource>(), existingDataSources))
            .ReturnsAsync(true);
        _inventoryApiClientMock.Setup(a =>
                a.AddDataSource(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EncryptedDataSource>()))
            .ReturnsAsync(true);
        
        await _service.CreateAndTryAddDataSource(path, fileSystemType, dataNode);
        _dataSourceCheckerMock.Verify(c => c.CheckDataSource(It.Is<DataSource>(ds =>
                ds.Path == path &&
                ds.Type == fileSystemType &&
                ds.ClientInstanceId == "CID" &&
                ds.DataNodeId == "N1" &&
                !string.IsNullOrEmpty(ds.Id)),
            It.IsAny<ICollection<DataSource>>()), Times.Once);
    }
    
    [Test]
    public void ApplyAddDataSourceLocally_ShouldAddToRepositoryAndRecomputeCodes()
    {
        var dataSource = new DataSource { Id = "DS1", DataNodeId = "N1", Path = "/test" };
        
        _service.ApplyAddDataSourceLocally(dataSource);
        
        _dataSourceRepositoryMock.Verify(r => r.AddOrUpdate(dataSource), Times.Once);
        _codeGeneratorMock.Verify(g => g.RecomputeCodesForNode("N1"), Times.Once);
    }
    
    [Test]
    public void ApplyRemoveDataSourceLocally_ShouldRemoveFromRepositoryAndRecomputeCodes()
    {
        var dataSource = new DataSource { Id = "DS1", DataNodeId = "N1", Path = "/test" };
        
        _service.ApplyRemoveDataSourceLocally(dataSource);
        
        _dataSourceRepositoryMock.Verify(r => r.Remove(dataSource), Times.Once);
        _codeGeneratorMock.Verify(g => g.RecomputeCodesForNode("N1"), Times.Once);
    }
    
    [Test]
    public async Task TryAddDataSource_SkipsApiCall_WhenNotCloudSession()
    {
        var dataSource = new DataSource { Id = "DS1", ClientInstanceId = "CID", DataNodeId = "N1", Path = "/test" };
        var existingDataSources = new List<DataSource>();
        
        _dataSourceCheckerMock.Setup(c => c.CheckDataSource(dataSource, existingDataSources))
            .ReturnsAsync(true);
        _sessionServiceMock.SetupGet(s => s.CurrentSession)
            .Returns((CloudSession?)null); // Not a cloud session
        _connectionServiceMock.SetupGet(c => c.ClientInstanceId).Returns("CID");
        
        var result = await _service.TryAddDataSource(dataSource);
        
        result.Should().BeTrue();
        _inventoryApiClientMock.Verify(
            a => a.AddDataSource(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EncryptedDataSource>()), Times.Never);
        _dataSourceRepositoryMock.Verify(r => r.AddOrUpdate(dataSource), Times.Once);
        _codeGeneratorMock.Verify(g => g.RecomputeCodesForNode("N1"), Times.Once);
    }
    
    [Test]
    public async Task TryAddDataSource_LogsWarning_WhenApiFails()
    {
        var sessionId = "SID";
        var dataSource = new DataSource { Id = "DS1", ClientInstanceId = "CID", DataNodeId = "N1", Path = "/test" };
        var encryptedDataSource = new EncryptedDataSource();
        var existingDataSources = new List<DataSource>();
        
        _dataSourceCheckerMock.Setup(c => c.CheckDataSource(dataSource, existingDataSources))
            .ReturnsAsync(true);
        _sessionServiceMock.SetupGet(s => s.CurrentSession)
            .Returns(new CloudSession { SessionId = sessionId });
        _connectionServiceMock.SetupGet(c => c.ClientInstanceId).Returns("CID");
        _dataEncrypterMock.Setup(e => e.EncryptDataSource(dataSource)).Returns(encryptedDataSource);
        _inventoryApiClientMock.Setup(a => a.AddDataSource(sessionId, "CID", "N1", encryptedDataSource))
            .ReturnsAsync(false);
        
        var result = await _service.TryAddDataSource(dataSource);
        
        result.Should().BeFalse();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to add DataSource")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    
    [Test]
    public async Task TryRemoveDataSource_LogsWarning_WhenApiFails()
    {
        var sessionId = "SID";
        var dataSource = new DataSource { Id = "DS1", ClientInstanceId = "CID", DataNodeId = "N1", Path = "/test" };
        var encryptedDataSource = new EncryptedDataSource();
        
        _sessionServiceMock.SetupGet(s => s.SessionId).Returns(sessionId);
        _connectionServiceMock.SetupGet(c => c.ClientInstanceId).Returns("CID");
        _dataEncrypterMock.Setup(e => e.EncryptDataSource(dataSource)).Returns(encryptedDataSource);
        _inventoryApiClientMock.Setup(a => a.RemoveDataSource(sessionId, "CID", "N1", encryptedDataSource))
            .ReturnsAsync(false);
        
        var result = await _service.TryRemoveDataSource(dataSource);
        
        result.Should().BeFalse();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to remove DataSource")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}