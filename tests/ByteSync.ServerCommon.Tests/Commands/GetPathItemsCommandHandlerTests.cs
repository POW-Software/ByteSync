using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.Inventories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Tests.Commands;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class GetPathItemsCommandHandlerTests
{
    private readonly ICloudSessionsRepository _mockCloudSessionsRepository;
    private readonly IInventoryRepository _mockInventoryRepository;
    private readonly ILogger<GetPathItemsCommandHandler> _mockLogger;
    
    private readonly GetPathItemsCommandHandler _getPathItemsCommandHandler;

    public GetPathItemsCommandHandlerTests()
    {
        _mockInventoryRepository = A.Fake<IInventoryRepository>();
        _mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        _mockLogger = A.Fake<ILogger<GetPathItemsCommandHandler>>();
        
        _getPathItemsCommandHandler = new GetPathItemsCommandHandler(_mockInventoryRepository, _mockLogger);
    }
    
    [Test]
    public async Task GetPathItems_InventoryNotStarted_ReturnsNull()
    {
        // Arrange
        var sessionId = "testSession";
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        var encryptedPathItem = new EncryptedPathItem { Code = "pathItem1" };
        var inventoryData = new InventoryData(sessionId);
        inventoryData.InventoryMembers.Add(new InventoryMemberData
            { ClientInstanceId = client.ClientInstanceId, SharedPathItems = new List<EncryptedPathItem> { encryptedPathItem } });

        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId))
            .Returns(new CloudSessionData(null, new EncryptedSessionSettings(), client));

        A.CallTo(() => _mockInventoryRepository.Get(sessionId))
            .Returns(inventoryData);

        var request = new GetPathItemsRequest(sessionId, client.ClientInstanceId);
        
        // Act
        var pathItems = await _getPathItemsCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        pathItems!.Count.Should().Be(1);
        A.CallTo(() => _mockInventoryRepository.Get(sessionId)).MustHaveHappenedOnceExactly();
    }
    
    [Test]
    public async Task GetPathItems_NoPathItems_ReturnsEmptyList()
    {
        // Arrange
        var sessionId = "testSession";
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        var inventoryData = new InventoryData(sessionId);

        A.CallTo(() => _mockInventoryRepository.Get(sessionId))
            .Returns(inventoryData);

        var request = new GetPathItemsRequest(sessionId, client.ClientInstanceId);
        
        // Act
        var pathItems = await _getPathItemsCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        pathItems.Should().BeEmpty();
        A.CallTo(() => _mockInventoryRepository.Get(sessionId)).MustHaveHappenedOnceExactly();
    }
    
    [Test]
    public async Task GetPathItems_OnePathItem_ReturnsPathItem()
    {
        // Arrange
        var sessionId = "testSession";
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        var pathItem = new EncryptedPathItem { Code = "pathItem1" };
        var inventoryData = new InventoryData(sessionId);
        inventoryData.InventoryMembers.Add(new InventoryMemberData { ClientInstanceId = client.ClientInstanceId, SharedPathItems = new List<EncryptedPathItem> { pathItem } });

        A.CallTo(() => _mockInventoryRepository.Get(sessionId))
            .Returns(inventoryData);
        
        var request = new GetPathItemsRequest(sessionId, client.ClientInstanceId);
        
        // Act
        var pathItems = await _getPathItemsCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        pathItems.Should().NotBeNull();
        pathItems!.Count.Should().Be(1);
        A.CallTo(() => _mockInventoryRepository.Get(sessionId)).MustHaveHappenedOnceExactly();
    }
}