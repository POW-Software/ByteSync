using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.Inventories;
using ByteSync.ServerCommon.Entities.Inventories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using FakeItEasy;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Tests.Commands.Inventories;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class RemoveDataSourceCommandHandlerTests
{
    private readonly IInventoryMemberService _mockInventoryMemberService;
    private readonly ICloudSessionsRepository _mockCloudSessionsRepository;
    private readonly IInventoryRepository _mockInventoryRepository;
    private readonly IInvokeClientsService _mockInvokeClientsService;
    private readonly ILogger<RemoveDataSourceCommandHandler> _mockLogger;
    
    private readonly RemoveDataSourceCommandHandler _removeDataSourceCommandHandler;

    public RemoveDataSourceCommandHandlerTests()
    {
        _mockInventoryMemberService = A.Fake<IInventoryMemberService>();
        _mockInventoryRepository = A.Fake<IInventoryRepository>();
        _mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        _mockInvokeClientsService = A.Fake<IInvokeClientsService>();
        _mockLogger = A.Fake<ILogger<RemoveDataSourceCommandHandler>>();
        
        _removeDataSourceCommandHandler = new RemoveDataSourceCommandHandler(_mockInventoryMemberService, _mockInventoryRepository, _mockCloudSessionsRepository, 
            _mockInvokeClientsService, _mockLogger);
    }
    
    [Test]
    public async Task RemoveDataSource_InventoryNotStarted_DoesNothingWhenNoSharedDataSource()
    {
        // Arrange
        var sessionId = "testSession";
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        var encryptedDataSource = new EncryptedDataSource { Id = "dataSource1" };
        var inventoryData = new InventoryEntity(sessionId);

        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId))
            .Returns(new CloudSessionData(null, new EncryptedSessionSettings(), client));

        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(A<string>.Ignored, A<Func<InventoryEntity?, InventoryEntity?>>.Ignored))
            .Invokes((string _, Func<InventoryEntity, InventoryEntity> func) => func(inventoryData))
            .Returns(new UpdateEntityResult<InventoryEntity>(inventoryData, UpdateEntityStatus.WaitingForTransaction));
        
        A.CallTo(() => _mockInventoryMemberService.GetOrCreateInventoryMember(A<InventoryEntity>.Ignored, "testSession", client))
            .Returns(new InventoryMemberEntity { ClientInstanceId = client.ClientInstanceId });

        var request = new RemoveDataSourceRequest(sessionId, client, client.ClientInstanceId, "dataNodeId", encryptedDataSource);

        // Act
        await _removeDataSourceCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(sessionId, A<Func<InventoryEntity?, InventoryEntity?>>.Ignored)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockInventoryMemberService.GetOrCreateInventoryMember(A<InventoryEntity>.Ignored, "testSession", client)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task RemoveDataSource_InventoryNotStarted_RemovesDataSourceCorrectly()
    {
        // Arrange
        var sessionId = "testSession";
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        var encryptedDataSource = new EncryptedDataSource { Id = "dataSource1" };
        var inventoryData = new InventoryEntity(sessionId);
        var inventoryMember = new InventoryMemberEntity { ClientInstanceId = client.ClientInstanceId };
        var dataNode = new InventoryDataNodeEntity { Id = "dataNodeId" };
        dataNode.DataSources.Add(new InventoryDataSourceEntity(encryptedDataSource));
        inventoryMember.DataNodes.Add(dataNode);
        inventoryData.InventoryMembers.Add(inventoryMember);

        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId))
            .Returns(new CloudSessionData(null, new EncryptedSessionSettings(), client));

        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(A<string>.Ignored, A<Func<InventoryEntity?, InventoryEntity?>>.Ignored))
            .Invokes((string _, Func<InventoryEntity, InventoryEntity> func) => func(inventoryData))
            .Returns(new UpdateEntityResult<InventoryEntity>(inventoryData, UpdateEntityStatus.WaitingForTransaction));
        
        A.CallTo(() => _mockInventoryMemberService.GetOrCreateInventoryMember(A<InventoryEntity>.Ignored, "testSession", client))
            .Returns(new InventoryMemberEntity { ClientInstanceId = client.ClientInstanceId });

        var request = new RemoveDataSourceRequest(sessionId, client, client.ClientInstanceId, "dataNodeId", encryptedDataSource);

        // Act
        await _removeDataSourceCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(sessionId, A<Func<InventoryEntity?, InventoryEntity?>>.Ignored)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockInventoryMemberService.GetOrCreateInventoryMember(A<InventoryEntity>.Ignored, "testSession", client)).MustHaveHappenedOnceExactly();
    }
}