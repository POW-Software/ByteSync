using Autofac;
using ByteSync.Business.PathItems;
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

public class TestPathItemsService : IntegrationTest
{
    private string _sessionId;
    private ByteSyncEndpoint _currentEndPoint;
    
    private PathItemsService _pathItemsService;

    [SetUp]
    public void SetUp()
    {
        RegisterType<PathItemRepository, IPathItemRepository>();
        RegisterType<SessionMemberRepository, ISessionMemberRepository>();
        RegisterType<PathItemsService>();
        BuildMoqContainer();

        var contextHelper = new TestContextGenerator(Container);
        _sessionId = contextHelper.GenerateSession();
        _currentEndPoint = contextHelper.GenerateCurrentEndpoint();

        _pathItemsService = Container.Resolve<PathItemsService>();
    }
    
    [Test]
    public async Task TryAddPathItem_ShouldAddToRepository_WhenCheckPassesAndApiCallSucceeds()
    {
        // Arrange
        var pathItemChecker = Container.Resolve<Mock<IPathItemChecker>>();
        var pathItemRepository = Container.Resolve<IPathItemRepository>();
        var inventoryApiClient = Container.Resolve<Mock<IInventoryApiClient>>();
        
        pathItemChecker
            .Setup(x => x.CheckPathItem(It.IsAny<PathItem>(), It.IsAny<IEnumerable<PathItem>>()))
            .ReturnsAsync(true);
        
        inventoryApiClient.Setup(x => x.AddPathItem(_sessionId, It.IsAny<EncryptedPathItem>()))
            .ReturnsAsync(true);

        var pathItem = new PathItem { ClientInstanceId = _currentEndPoint.ClientInstanceId };
        
        // Act
        var result = await _pathItemsService.TryAddPathItem(pathItem);
        
        // Assert
        result.Should().BeTrue();
        pathItemRepository.Elements.Should().Contain(pathItem);
        inventoryApiClient.Verify(x => x.AddPathItem(_sessionId, It.IsAny<EncryptedPathItem>()), Times.Once);
    }
    
    [Test]
    public async Task TryAddPathItem_ShouldAddToRepository_WhenCheckPassesAndClientInstanceIdDiffers()
    {
        // Arrange
        var pathItemChecker = Container.Resolve<Mock<IPathItemChecker>>();
        var pathItemRepository = Container.Resolve<IPathItemRepository>();
        var inventoryApiClient = Container.Resolve<Mock<IInventoryApiClient>>();
        
        pathItemChecker
            .Setup(x => x.CheckPathItem(It.IsAny<PathItem>(), It.IsAny<IEnumerable<PathItem>>()))
            .ReturnsAsync(true);
        
        inventoryApiClient.Setup(x => x.AddPathItem(_sessionId, It.IsAny<EncryptedPathItem>()))
            .ReturnsAsync(true);

        var pathItem = new PathItem { ClientInstanceId = _currentEndPoint.ClientInstanceId + "_FAKE" };
        
        // Act
        var result = await _pathItemsService.TryAddPathItem(pathItem);
        
        // Assert
        result.Should().BeTrue();
        pathItemRepository.Elements.Should().Contain(pathItem);
        inventoryApiClient.Verify(x => x.AddPathItem(_sessionId, It.IsAny<EncryptedPathItem>()), Times.Never);
    }
    
    [Test]
    public async Task TryAddPathItem_ShouldNotAdd_WhenCheckFails()
    {
        // Arrange
        var pathItemChecker = Container.Resolve<Mock<IPathItemChecker>>();
        var pathItemRepository = Container.Resolve<IPathItemRepository>();
        
        pathItemChecker
            .Setup(x => x.CheckPathItem(It.IsAny<PathItem>(), It.IsAny<IEnumerable<PathItem>>()))
            .ReturnsAsync(false);

        var pathItem = new PathItem { ClientInstanceId = _currentEndPoint.ClientInstanceId };
        
        // Act
        var result = await _pathItemsService.TryAddPathItem(pathItem);
        
        // Assert
        result.Should().BeFalse();
        pathItemRepository.Elements.Should().BeEmpty();
    }
    
    [Test]
    public async Task TryAddPathItem_ShouldNotAdd_WhenApiCallFails()
    {
        // Arrange
        var pathItemChecker = Container.Resolve<Mock<IPathItemChecker>>();
        var pathItemRepository = Container.Resolve<IPathItemRepository>();
        var inventoryApiClient = Container.Resolve<Mock<IInventoryApiClient>>();
        
        pathItemChecker
            .Setup(x => x.CheckPathItem(It.IsAny<PathItem>(), It.IsAny<IEnumerable<PathItem>>()))
            .ReturnsAsync(true);
        
        inventoryApiClient.Setup(x => x.AddPathItem(_sessionId, It.IsAny<EncryptedPathItem>()))
            .ReturnsAsync(false);

        var pathItem = new PathItem { ClientInstanceId = _currentEndPoint.ClientInstanceId };
        
        // Act
        var result = await _pathItemsService.TryAddPathItem(pathItem);
        
        // Assert
        result.Should().BeFalse();
        pathItemRepository.Elements.Should().BeEmpty();
    }
    
    
    [Test]
    public async Task TryRemovePathItem_ShouldRemoveFromRepository_WhenApiCallSucceeds()
    {
        // Arrange
        var dataEncrypter = Container.Resolve<Mock<IDataEncrypter>>();
        var inventoryApiClient = Container.Resolve<Mock<IInventoryApiClient>>();
        var pathItemRepository = Container.Resolve<IPathItemRepository>();
        
        var pathItem = new PathItem { ClientInstanceId = _currentEndPoint.ClientInstanceId };
        dataEncrypter.Setup(x => x.EncryptPathItem(pathItem)).Returns(new EncryptedPathItem());
        
        inventoryApiClient.Setup(x => x.RemovePathItem(_sessionId, It.IsAny<EncryptedPathItem>()))
            .ReturnsAsync(true);
        
        pathItemRepository.AddOrUpdate(pathItem);

        // Act
        var result = await _pathItemsService.TryRemovePathItem(pathItem);
        
        // Assert
        result.Should().BeTrue();
        pathItemRepository.Elements.Should().BeEmpty();
    }
    
    [Test]
    public async Task TryRemovePathItem_ShouldNotRemoveFromRepository_WhenApiCallFails()
    {
        // Arrange
        var dataEncrypter = Container.Resolve<Mock<IDataEncrypter>>();
        var inventoryApiClient = Container.Resolve<Mock<IInventoryApiClient>>();
        var pathItemRepository = Container.Resolve<IPathItemRepository>();
        
        var pathItem = new PathItem { ClientInstanceId = _currentEndPoint.ClientInstanceId };
        dataEncrypter.Setup(x => x.EncryptPathItem(pathItem)).Returns(new EncryptedPathItem());
        
        inventoryApiClient.Setup(x => x.RemovePathItem(_sessionId, It.IsAny<EncryptedPathItem>()))
            .ReturnsAsync(false);
        
        pathItemRepository.AddOrUpdate(pathItem);

        // Act
        var result = await _pathItemsService.TryRemovePathItem(pathItem);
        
        // Assert
        result.Should().BeFalse();
        pathItemRepository.Elements.Should().Contain(pathItem);
    }
    
    [Test]
    public async Task CreateAndTryAddPathItem_ShouldAddPathItemToRepository()
    {
        // Arrange
        var path = "test/path";
        var fileSystemType = FileSystemTypes.File;
        var pathItemRepository = Container.Resolve<IPathItemRepository>();
        var sessionMemberRepository = Container.Resolve<ISessionMemberRepository>();
        var sessionMemberInfo = new SessionMemberInfo
        {
            Endpoint = _currentEndPoint,
        };
        
        var pathItemChecker = Container.Resolve<Mock<IPathItemChecker>>();
        var inventoryApiClient = Container.Resolve<Mock<IInventoryApiClient>>();
        
        pathItemChecker
            .Setup(x => x.CheckPathItem(It.IsAny<PathItem>(), It.IsAny<IEnumerable<PathItem>>()))
            .ReturnsAsync(true);
        
        inventoryApiClient.Setup(x => x.AddPathItem(_sessionId, It.IsAny<EncryptedPathItem>()))
            .ReturnsAsync(true);
        
        sessionMemberRepository.AddOrUpdate(sessionMemberInfo);
        
        // Act
        await _pathItemsService.CreateAndTryAddPathItem(path, fileSystemType);

        // Assert
        pathItemRepository.Elements.Should().ContainSingle(pi => pi.Path == path && pi.Type == fileSystemType);
    }

    [Test]
    public void ApplyAddPathItemLocally_ShouldAddPathItemToRepository()
    {
        // Arrange
        var pathItem = new PathItem { ClientInstanceId = _currentEndPoint.ClientInstanceId };
        var pathItemRepository = Container.Resolve<IPathItemRepository>();

        // Act
        _pathItemsService.ApplyAddPathItemLocally(pathItem);

        // Assert
        pathItemRepository.Elements.Should().Contain(pathItem);
    }

    [Test]
    public void ApplyRemovePathItemLocally_ShouldRemovePathItemFromRepository()
    {
        // Arrange
        var pathItem = new PathItem { ClientInstanceId = _currentEndPoint.ClientInstanceId };
        var pathItemRepository = Container.Resolve<IPathItemRepository>();
        var sessionMemberRepository = Container.Resolve<Mock<ISessionMemberRepository>>();
        var sessionMemberInfo = new SessionMemberInfo
        {
            Endpoint = _currentEndPoint,
        };

        pathItemRepository.AddOrUpdate(pathItem);
        sessionMemberRepository.Setup(x => x.GetElement(It.IsAny<string>())).Returns(sessionMemberInfo);

        // Act
        _pathItemsService.ApplyRemovePathItemLocally(pathItem);

        // Assert
        pathItemRepository.Elements.Should().NotContain(pathItem);
    }
}