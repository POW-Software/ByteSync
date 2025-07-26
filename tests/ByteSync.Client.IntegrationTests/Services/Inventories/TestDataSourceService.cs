using Autofac;
using ByteSync.Business.DataNodes;
using ByteSync.Business.DataSources;
using ByteSync.Business.SessionMembers;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Repositories;
using ByteSync.Repositories;
using ByteSync.Services.Inventories;
using ByteSync.TestsCommon;
using FluentAssertions;
using Moq;

namespace ByteSync.Client.IntegrationTests.Services.Inventories;

public class TestDataSourceService : IntegrationTest
{
    private string _sessionId;
    private ByteSyncEndpoint _currentEndPoint;
    
    private DataSourceService _dataSourceService;

    [SetUp]
    public void SetUp()
    {
        RegisterType<DataSourceRepository, IDataSourceRepository>();
        RegisterType<SessionMemberRepository, ISessionMemberRepository>();
        RegisterType<DataSourceService>();
        BuildMoqContainer();

        var contextHelper = new TestContextGenerator(Container);
        _sessionId = contextHelper.GenerateSession();
        _currentEndPoint = contextHelper.GenerateCurrentEndpoint();

        _dataSourceService = Container.Resolve<DataSourceService>();
    }
    
    [Test]
    public async Task TryAddDataSource_ShouldAddToRepository_WhenCheckPassesAndApiCallSucceeds()
    {
        // Arrange
        var dataSourceChecker = Container.Resolve<Mock<IDataSourceChecker>>();
        var dataSourceRepository = Container.Resolve<IDataSourceRepository>();
        var inventoryApiClient = Container.Resolve<Mock<IInventoryApiClient>>();
        
        dataSourceChecker
            .Setup(x => x.CheckDataSource(It.IsAny<DataSource>(), It.IsAny<IEnumerable<DataSource>>()))
            .ReturnsAsync(true);
        
        inventoryApiClient.Setup(x => x.AddDataSource(_sessionId, _currentEndPoint.ClientInstanceId, It.IsAny<string>(), It.IsAny<EncryptedDataSource>()))
            .ReturnsAsync(true);

        var dataSource = new DataSource { ClientInstanceId = _currentEndPoint.ClientInstanceId };
        
        // Act
        var result = await _dataSourceService.TryAddDataSource(dataSource);
        
        // Assert
        result.Should().BeTrue();
        dataSourceRepository.Elements.Should().Contain(dataSource);
        inventoryApiClient.Verify(x => x.AddDataSource(_sessionId, _currentEndPoint.ClientInstanceId, It.IsAny<string>(), It.IsAny<EncryptedDataSource>()), Times.Once);
    }
    
    [Test]
    public async Task TryAddDataSource_ShouldAddToRepository_WhenCheckPassesAndClientInstanceIdDiffers()
    {
        // Arrange
        var dataSourceChecker = Container.Resolve<Mock<IDataSourceChecker>>();
        var dataSourceRepository = Container.Resolve<IDataSourceRepository>();
        var inventoryApiClient = Container.Resolve<Mock<IInventoryApiClient>>();
        
        dataSourceChecker
            .Setup(x => x.CheckDataSource(It.IsAny<DataSource>(), It.IsAny<IEnumerable<DataSource>>()))
            .ReturnsAsync(true);
        
        inventoryApiClient.Setup(x => x.AddDataSource(_sessionId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EncryptedDataSource>()))
            .ReturnsAsync(true);

        var dataSource = new DataSource { ClientInstanceId = _currentEndPoint.ClientInstanceId + "_FAKE" };
        
        // Act
        var result = await _dataSourceService.TryAddDataSource(dataSource);
        
        // Assert
        result.Should().BeTrue();
        dataSourceRepository.Elements.Should().Contain(dataSource);
        inventoryApiClient.Verify(x => x.AddDataSource(_sessionId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EncryptedDataSource>()), Times.Never);
    }
    
    [Test]
    public async Task TryAddDataSource_ShouldNotAdd_WhenCheckFails()
    {
        // Arrange
        var dataSourceChecker = Container.Resolve<Mock<IDataSourceChecker>>();
        var dataSourceRepository = Container.Resolve<IDataSourceRepository>();
        
        dataSourceChecker
            .Setup(x => x.CheckDataSource(It.IsAny<DataSource>(), It.IsAny<IEnumerable<DataSource>>()))
            .ReturnsAsync(false);

        var dataSource = new DataSource { ClientInstanceId = _currentEndPoint.ClientInstanceId };
        
        // Act
        var result = await _dataSourceService.TryAddDataSource(dataSource);
        
        // Assert
        result.Should().BeFalse();
        dataSourceRepository.Elements.Should().BeEmpty();
    }
    
    [Test]
    public async Task TryAddDataSource_ShouldNotAdd_WhenApiCallFails()
    {
        // Arrange
        var dataSourceChecker = Container.Resolve<Mock<IDataSourceChecker>>();
        var dataSourceRepository = Container.Resolve<IDataSourceRepository>();
        var inventoryApiClient = Container.Resolve<Mock<IInventoryApiClient>>();
        
        dataSourceChecker
            .Setup(x => x.CheckDataSource(It.IsAny<DataSource>(), It.IsAny<IEnumerable<DataSource>>()))
            .ReturnsAsync(true);
        
        inventoryApiClient.Setup(x => x.AddDataSource(_sessionId, _currentEndPoint.ClientInstanceId, It.IsAny<string>(), It.IsAny<EncryptedDataSource>()))
            .ReturnsAsync(false);

        var dataSource = new DataSource { ClientInstanceId = _currentEndPoint.ClientInstanceId };
        
        // Act
        var result = await _dataSourceService.TryAddDataSource(dataSource);
        
        // Assert
        result.Should().BeFalse();
        dataSourceRepository.Elements.Should().BeEmpty();
    }
    
    
    [Test]
    public async Task TryRemoveDataSource_ShouldRemoveFromRepository_WhenApiCallSucceeds()
    {
        // Arrange
        var dataEncrypter = Container.Resolve<Mock<IDataEncrypter>>();
        var inventoryApiClient = Container.Resolve<Mock<IInventoryApiClient>>();
        var dataSourceRepository = Container.Resolve<IDataSourceRepository>();
        
        var dataSource = new DataSource { ClientInstanceId = _currentEndPoint.ClientInstanceId };
        dataEncrypter.Setup(x => x.EncryptDataSource(dataSource)).Returns(new EncryptedDataSource());
        
        inventoryApiClient.Setup(x => x.RemoveDataSource(_sessionId, _currentEndPoint.ClientInstanceId, It.IsAny<string>(), It.IsAny<EncryptedDataSource>()))
            .ReturnsAsync(true);
        
        dataSourceRepository.AddOrUpdate(dataSource);

        // Act
        var result = await _dataSourceService.TryRemoveDataSource(dataSource);
        
        // Assert
        result.Should().BeTrue();
        dataSourceRepository.Elements.Should().BeEmpty();
    }
    
    [Test]
    public async Task TryRemoveDataSource_ShouldNotRemoveFromRepository_WhenApiCallFails()
    {
        // Arrange
        var dataEncrypter = Container.Resolve<Mock<IDataEncrypter>>();
        var inventoryApiClient = Container.Resolve<Mock<IInventoryApiClient>>();
        var dataSourceRepository = Container.Resolve<IDataSourceRepository>();
        
        var dataSource = new DataSource { ClientInstanceId = _currentEndPoint.ClientInstanceId };
        dataEncrypter.Setup(x => x.EncryptDataSource(dataSource)).Returns(new EncryptedDataSource());
        
        inventoryApiClient.Setup(x => x.RemoveDataSource(_sessionId, _currentEndPoint.ClientInstanceId, It.IsAny<string>(), It.IsAny<EncryptedDataSource>()))
            .ReturnsAsync(false);
        
        dataSourceRepository.AddOrUpdate(dataSource);

        // Act
        var result = await _dataSourceService.TryRemoveDataSource(dataSource);
        
        // Assert
        result.Should().BeFalse();
        dataSourceRepository.Elements.Should().Contain(dataSource);
    }
    
    [Test]
    public async Task CreateAndTryAddDataSource_ShouldAddDataSourceToRepository()
    {
        // Arrange
        var path = "test/path";
        var fileSystemType = FileSystemTypes.File;
        var dataSourceRepository = Container.Resolve<IDataSourceRepository>();
        var sessionMemberRepository = Container.Resolve<ISessionMemberRepository>();
        var sessionMemberInfo = new SessionMember
        {
            Endpoint = _currentEndPoint,
        };
        var dataNode = new DataNode
        {
            Id = "test-node-id",
            ClientInstanceId = _currentEndPoint.ClientInstanceId
        };
        
        var dataSourceChecker = Container.Resolve<Mock<IDataSourceChecker>>();
        var inventoryApiClient = Container.Resolve<Mock<IInventoryApiClient>>();
        
        dataSourceChecker
            .Setup(x => x.CheckDataSource(It.IsAny<DataSource>(), It.IsAny<IEnumerable<DataSource>>()))
            .ReturnsAsync(true);
        
        inventoryApiClient.Setup(x => x.AddDataSource(_sessionId, _currentEndPoint.ClientInstanceId, It.IsAny<string>(), It.IsAny<EncryptedDataSource>()))
            .ReturnsAsync(true);
        
        sessionMemberRepository.AddOrUpdate(sessionMemberInfo);
        
        // Act
        await _dataSourceService.CreateAndTryAddDataSource(path, fileSystemType, dataNode);

        // Assert
        dataSourceRepository.Elements.Should().ContainSingle(ds => ds.Path == path && ds.Type == fileSystemType);
    }

    [Test]
    public void ApplyAddDataSourceLocally_ShouldAddDataSourceToRepository()
    {
        // Arrange
        var dataSource = new DataSource { ClientInstanceId = _currentEndPoint.ClientInstanceId };
        var dataSourceRepository = Container.Resolve<IDataSourceRepository>();

        // Act
        _dataSourceService.ApplyAddDataSourceLocally(dataSource);

        // Assert
        dataSourceRepository.Elements.Should().Contain(dataSource);
    }

    [Test]
    public void ApplyRemoveDataSourceLocally_ShouldRemoveDataSourceFromRepository()
    {
        // Arrange
        var dataSource = new DataSource { ClientInstanceId = _currentEndPoint.ClientInstanceId };
        var dataSourceRepository = Container.Resolve<IDataSourceRepository>();
        var sessionMemberRepository = Container.Resolve<Mock<ISessionMemberRepository>>();
        var sessionMemberInfo = new SessionMember
        {
            Endpoint = _currentEndPoint,
        };

        dataSourceRepository.AddOrUpdate(dataSource);
        sessionMemberRepository.Setup(x => x.GetElement(It.IsAny<string>())).Returns(sessionMemberInfo);

        // Act
        _dataSourceService.ApplyRemoveDataSourceLocally(dataSource);

        // Assert
        dataSourceRepository.Elements.Should().NotContain(dataSource);
    }
}