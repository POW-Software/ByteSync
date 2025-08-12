using Autofac;
using ByteSync.Business.DataNodes;
using ByteSync.Business.DataSources;
using ByteSync.Common.Business.Sessions;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Repositories;
using ByteSync.Services.Inventories;
using ByteSync.TestsCommon;
using FluentAssertions;
using Moq;

namespace ByteSync.Client.IntegrationTests.Services.Inventories;

public class TestDataNodeService : IntegrationTest
{
    private string _sessionId = null!;
    private ByteSyncEndpoint _currentEndPoint = null!;
    private DataNodeService _service = null!;

    [SetUp]
    public void SetUp()
    {
        RegisterType<DataNodeRepository, IDataNodeRepository>();
        RegisterType<DataSourceRepository, IDataSourceRepository>();
        RegisterType<DataNodeService>();
        RegisterType<DataSourceService, IDataSourceService>();
        BuildMoqContainer();

        var contextHelper = new TestContextGenerator(Container);
        _sessionId = contextHelper.GenerateSession();
        _currentEndPoint = contextHelper.GenerateCurrentEndpoint();

        _service = Container.Resolve<DataNodeService>();
    }

    [Test]
    public async Task TryAddDataNode_ShouldAddToRepository_WhenApiSucceeds()
    {
        var repository = Container.Resolve<IDataNodeRepository>();
        var apiClient = Container.Resolve<Mock<IInventoryApiClient>>();
        var encrypted = new EncryptedDataNode();
        apiClient.Setup(a => a.AddDataNode(_sessionId, _currentEndPoint.ClientInstanceId, encrypted)).ReturnsAsync(true);
        var encrypter = Container.Resolve<Mock<IDataEncrypter>>();
        encrypter.Setup(e => e.EncryptDataNode(It.Is<DataNode>(n => n.Id == "NODE"))).Returns(encrypted);
        var node = new DataNode { Id = "NODE", ClientInstanceId = _currentEndPoint.ClientInstanceId };

        var result = await _service.TryAddDataNode(node);
        
        result.Should().BeTrue();
        repository.Elements.Should().Contain(n => n.Id == "NODE");
    }

    [Test]
    public async Task TryAddDataNode_ShouldNotAddToRepository_WhenApiFails()
    {
        var repository = Container.Resolve<IDataNodeRepository>();
        var apiClient = Container.Resolve<Mock<IInventoryApiClient>>();
        var encrypted = new EncryptedDataNode();
        apiClient.Setup(a => a.AddDataNode(_sessionId, _currentEndPoint.ClientInstanceId, encrypted)).ReturnsAsync(false);
        var encrypter = Container.Resolve<Mock<IDataEncrypter>>();
        encrypter.Setup(e => e.EncryptDataNode(It.Is<DataNode>(n => n.Id == "NODE"))).Returns(encrypted);
        var node = new DataNode { Id = "NODE", ClientInstanceId = _currentEndPoint.ClientInstanceId };

        var result = await _service.TryAddDataNode(node);
        
        result.Should().BeFalse();
        repository.Elements.Should().NotContain(n => n.Id == "NODE");
    }

    [Test]
    public async Task TryRemoveDataNode_ShouldRemoveFromRepository_WhenApiSucceeds()
    {
        var nodeRepository = Container.Resolve<IDataNodeRepository>();
        var apiClient = Container.Resolve<Mock<IInventoryApiClient>>();
        var encrypter = Container.Resolve<Mock<IDataEncrypter>>();
        var node = new DataNode { Id = "NODE", ClientInstanceId = _currentEndPoint.ClientInstanceId };
        var encrypted = new EncryptedDataNode();
        
        // Add node first  
        nodeRepository.AddOrUpdate(node);
        nodeRepository.Elements.Should().Contain(n => n.Id == "NODE");
        
        // Setup mocks for removal
        encrypter.Setup(e => e.EncryptDataNode(It.Is<DataNode>(n => n.Id == "NODE"))).Returns(encrypted);
        apiClient.Setup(a => a.RemoveDataNode(_sessionId, _currentEndPoint.ClientInstanceId, encrypted)).ReturnsAsync(true);

        var result = await _service.TryRemoveDataNode(node);
        
        result.Should().BeTrue();
        nodeRepository.Elements.Should().NotContain(n => n.Id == "NODE");
    }

    [Test]
    public async Task TryRemoveDataNode_ShouldRemoveDataSourcesAndNode_WhenBothExist()
    {
        var nodeRepository = Container.Resolve<IDataNodeRepository>();
        var dataSourceRepository = Container.Resolve<IDataSourceRepository>();
        var apiClient = Container.Resolve<Mock<IInventoryApiClient>>();
        var encrypter = Container.Resolve<Mock<IDataEncrypter>>();
        
        var node = new DataNode { Id = "NODE", ClientInstanceId = _currentEndPoint.ClientInstanceId };
        var dataSource1 = new DataSource { Id = "DS1", DataNodeId = "NODE", ClientInstanceId = _currentEndPoint.ClientInstanceId, Path = "/path1", Type = ByteSync.Common.Business.Inventories.FileSystemTypes.Directory };
        var dataSource2 = new DataSource { Id = "DS2", DataNodeId = "NODE", ClientInstanceId = _currentEndPoint.ClientInstanceId, Path = "/path2", Type = ByteSync.Common.Business.Inventories.FileSystemTypes.File };
        var unrelatedDataSource = new DataSource { Id = "DS3", DataNodeId = "OTHER_NODE", ClientInstanceId = _currentEndPoint.ClientInstanceId, Path = "/path3", Type = ByteSync.Common.Business.Inventories.FileSystemTypes.File };
        
        // Add node and datasources first
        nodeRepository.AddOrUpdate(node);
        dataSourceRepository.AddOrUpdate(dataSource1);
        dataSourceRepository.AddOrUpdate(dataSource2);
        dataSourceRepository.AddOrUpdate(unrelatedDataSource);
        
        // Verify they exist
        nodeRepository.Elements.Should().Contain(n => n.Id == "NODE");
        dataSourceRepository.Elements.Should().Contain(ds => ds.Id == "DS1");
        dataSourceRepository.Elements.Should().Contain(ds => ds.Id == "DS2");
        dataSourceRepository.Elements.Should().Contain(ds => ds.Id == "DS3");
        
        // Setup mocks for removal
        var encrypted = new EncryptedDataNode();
        encrypter.Setup(e => e.EncryptDataNode(It.Is<DataNode>(n => n.Id == "NODE"))).Returns(encrypted);
        apiClient.Setup(a => a.RemoveDataNode(_sessionId, _currentEndPoint.ClientInstanceId, encrypted)).ReturnsAsync(true);

        var result = await _service.TryRemoveDataNode(node);
        
        result.Should().BeTrue();
        
        // Verify node is removed
        nodeRepository.Elements.Should().NotContain(n => n.Id == "NODE");
        
        // Verify only associated datasources are removed
        dataSourceRepository.Elements.Should().NotContain(ds => ds.Id == "DS1");
        dataSourceRepository.Elements.Should().NotContain(ds => ds.Id == "DS2");
        dataSourceRepository.Elements.Should().Contain(ds => ds.Id == "DS3"); // Should remain
    }

    [Test]
    public async Task TryRemoveDataNode_ShouldNotRemoveFromRepository_WhenApiFails()
    {
        var nodeRepository = Container.Resolve<IDataNodeRepository>();
        var dataSourceRepository = Container.Resolve<IDataSourceRepository>();
        var apiClient = Container.Resolve<Mock<IInventoryApiClient>>();
        var encrypter = Container.Resolve<Mock<IDataEncrypter>>();
        
        var node = new DataNode { Id = "NODE", ClientInstanceId = _currentEndPoint.ClientInstanceId };
        var dataSource = new DataSource { Id = "DS1", DataNodeId = "NODE", ClientInstanceId = _currentEndPoint.ClientInstanceId, Path = "/path1", Type = ByteSync.Common.Business.Inventories.FileSystemTypes.Directory };
        
        // Add node and datasource first
        nodeRepository.AddOrUpdate(node);
        dataSourceRepository.AddOrUpdate(dataSource);
        
        // Setup mocks for failed removal
        var encrypted = new EncryptedDataNode();
        encrypter.Setup(e => e.EncryptDataNode(It.Is<DataNode>(n => n.Id == "NODE"))).Returns(encrypted);
        apiClient.Setup(a => a.RemoveDataNode(_sessionId, _currentEndPoint.ClientInstanceId, encrypted)).ReturnsAsync(false);

        var result = await _service.TryRemoveDataNode(node);
        
        result.Should().BeFalse();
        
        // Verify nothing is removed when API fails
        nodeRepository.Elements.Should().Contain(n => n.Id == "NODE");
        dataSourceRepository.Elements.Should().Contain(ds => ds.Id == "DS1");
    }
}
