using Autofac;
using ByteSync.Business.DataNodes;
using ByteSync.Common.Business.Sessions;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Controls.Encryptions;
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
        RegisterType<DataNodeService>();
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
        apiClient.Setup(a => a.AddDataNode(_sessionId, encrypted)).ReturnsAsync(true);
        var encrypter = Container.Resolve<Mock<IDataEncrypter>>();
        encrypter.Setup(e => e.EncryptDataNode(It.Is<DataNode>(n => n.NodeId == "NODE"))).Returns(encrypted);
        var node = new DataNode { NodeId = "NODE", ClientInstanceId = _currentEndPoint.ClientInstanceId };

        var result = await _service.TryAddDataNode(node);

        result.Should().BeTrue();
        repository.Elements.Should().Contain(node);
        apiClient.Verify(a => a.AddDataNode(_sessionId, encrypted), Times.Once);
    }

    [Test]
    public async Task TryAddDataNode_ShouldSkipApi_WhenClientDiffers()
    {
        var repository = Container.Resolve<IDataNodeRepository>();
        var apiClient = Container.Resolve<Mock<IInventoryApiClient>>();
        apiClient.Setup(a => a.AddDataNode(It.IsAny<string>(), It.IsAny<EncryptedDataNode>())).ReturnsAsync(true);
        var encrypter = Container.Resolve<Mock<IDataEncrypter>>();
        encrypter.Setup(e => e.EncryptDataNode(It.IsAny<DataNode>())).Returns(new EncryptedDataNode());
        var node = new DataNode { NodeId = "NODE", ClientInstanceId = "OTHER" };

        var result = await _service.TryAddDataNode(node);

        result.Should().BeTrue();
        repository.Elements.Should().Contain(node);
        apiClient.Verify(a => a.AddDataNode(It.IsAny<string>(), It.IsAny<EncryptedDataNode>()), Times.Never);
    }

    [Test]
    public async Task TryAddDataNode_ShouldNotAdd_WhenApiFails()
    {
        var repository = Container.Resolve<IDataNodeRepository>();
        var apiClient = Container.Resolve<Mock<IInventoryApiClient>>();
        var encrypted = new EncryptedDataNode();
        apiClient.Setup(a => a.AddDataNode(_sessionId, encrypted)).ReturnsAsync(false);
        var encrypter = Container.Resolve<Mock<IDataEncrypter>>();
        encrypter.Setup(e => e.EncryptDataNode(It.Is<DataNode>(n => n.NodeId == "NODE"))).Returns(encrypted);
        var node = new DataNode { NodeId = "NODE", ClientInstanceId = _currentEndPoint.ClientInstanceId };

        var result = await _service.TryAddDataNode(node);

        result.Should().BeFalse();
        repository.Elements.Should().BeEmpty();
    }

    [Test]
    public async Task TryRemoveDataNode_ShouldRemoveFromRepository_WhenApiSucceeds()
    {
        var repository = Container.Resolve<IDataNodeRepository>();
        var apiClient = Container.Resolve<Mock<IInventoryApiClient>>();
        var node = new DataNode { NodeId = "NODE", ClientInstanceId = _currentEndPoint.ClientInstanceId };
        repository.AddOrUpdate(node);
        apiClient.Setup(a => a.RemoveDataNode(_sessionId, It.IsAny<EncryptedDataNode>())).ReturnsAsync(true);

        var result = await _service.TryRemoveDataNode(node);

        result.Should().BeTrue();
        repository.Elements.Should().BeEmpty();
        apiClient.Verify(a => a.RemoveDataNode(_sessionId, It.IsAny<EncryptedDataNode>()), Times.Once);
    }

    [Test]
    public async Task CreateAndTryAddDataNode_ShouldAddDataNodeToRepository()
    {
        var repository = Container.Resolve<IDataNodeRepository>();
        var apiClient = Container.Resolve<Mock<IInventoryApiClient>>();
        var encrypted = new EncryptedDataNode();
        apiClient.Setup(a => a.AddDataNode(_sessionId, encrypted)).ReturnsAsync(true);
        var encrypter = Container.Resolve<Mock<IDataEncrypter>>();
        encrypter.Setup(e => e.EncryptDataNode(It.Is<DataNode>(n => n.NodeId == "NODE"))).Returns(encrypted);

        await _service.CreateAndTryAddDataNode("NODE");

        repository.Elements.Should().ContainSingle(n => n.NodeId == "NODE" && n.ClientInstanceId == _currentEndPoint.ClientInstanceId);
    }
}
