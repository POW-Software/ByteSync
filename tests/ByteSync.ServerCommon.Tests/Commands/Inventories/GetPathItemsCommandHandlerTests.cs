using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.Inventories;
using ByteSync.ServerCommon.Entities.Inventories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Tests.Commands.Inventories;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class GetDataSourcesCommandHandlerTests
{
    private readonly ICloudSessionsRepository _mockCloudSessionsRepository;
    private readonly IInventoryRepository _mockInventoryRepository;
    private readonly ILogger<GetDataSourcesCommandHandler> _mockLogger;
    
    private readonly GetDataSourcesCommandHandler _getDataSourcesCommandHandler;

    public GetDataSourcesCommandHandlerTests()
    {
        _mockInventoryRepository = A.Fake<IInventoryRepository>();
        _mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        _mockLogger = A.Fake<ILogger<GetDataSourcesCommandHandler>>();
        
        _getDataSourcesCommandHandler = new GetDataSourcesCommandHandler(_mockInventoryRepository, _mockLogger);
    }
    
    [Test]
    public async Task GetDataSources_InventoryNotStarted_ReturnsNull()
    {
        // Arrange
        var sessionId = "testSession";
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        var encryptedDataSource = new EncryptedDataSource { Id = "dataSource1" };
        var inventoryData = new InventoryEntity(sessionId);
        var inventoryMember = new InventoryMemberEntity { ClientInstanceId = client.ClientInstanceId };
        var dataNode = new InventoryDataNodeEntity { Id = "nodeId" };
        dataNode.DataSources.Add(new InventoryDataSourceEntity(encryptedDataSource));
        inventoryMember.DataNodes.Add(dataNode);
        inventoryData.InventoryMembers.Add(inventoryMember);

        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId))
            .Returns(new CloudSessionData(null, new EncryptedSessionSettings(), client));

        A.CallTo(() => _mockInventoryRepository.Get(sessionId))
            .Returns(inventoryData);

        var request = new GetDataSourcesRequest(sessionId, client.ClientInstanceId, "nodeId");
        
        // Act
        var dataSources = await _getDataSourcesCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        dataSources!.Count.Should().Be(1);
        A.CallTo(() => _mockInventoryRepository.Get(sessionId)).MustHaveHappenedOnceExactly();
    }
    
    [Test]
    public async Task GetDataSources_NoDataSources_ReturnsEmptyList()
    {
        // Arrange
        var sessionId = "testSession";
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        var inventoryData = new InventoryEntity(sessionId);

        A.CallTo(() => _mockInventoryRepository.Get(sessionId))
            .Returns(inventoryData);

        var request = new GetDataSourcesRequest(sessionId, client.ClientInstanceId, "nodeId");
        
        // Act
        var dataSources = await _getDataSourcesCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        dataSources.Should().BeEmpty();
        A.CallTo(() => _mockInventoryRepository.Get(sessionId)).MustHaveHappenedOnceExactly();
    }
    
    [Test]
    public async Task GetDataSources_OneDataSource_ReturnsDataSource()
    {
        // Arrange
        var sessionId = "testSession";
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        var dataSource = new EncryptedDataSource { Id = "dataSource1" };
        var inventoryData = new InventoryEntity(sessionId);
        var inventoryMember = new InventoryMemberEntity { ClientInstanceId = client.ClientInstanceId };
        var dataNode = new InventoryDataNodeEntity { Id = "nodeId" };
        dataNode.DataSources.Add(new InventoryDataSourceEntity(dataSource));
        inventoryMember.DataNodes.Add(dataNode);
        inventoryData.InventoryMembers.Add(inventoryMember);

        A.CallTo(() => _mockInventoryRepository.Get(sessionId))
            .Returns(inventoryData);
        
        var request = new GetDataSourcesRequest(sessionId, client.ClientInstanceId, "nodeId");
        
        // Act
        var dataSources = await _getDataSourcesCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        dataSources.Should().NotBeNull();
        dataSources!.Count.Should().Be(1);
        A.CallTo(() => _mockInventoryRepository.Get(sessionId)).MustHaveHappenedOnceExactly();
    }
}