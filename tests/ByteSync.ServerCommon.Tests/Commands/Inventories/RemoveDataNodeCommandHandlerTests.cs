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
public class RemoveDataNodeCommandHandlerTests
{
    private readonly IInventoryMemberService _mockInventoryMemberService;
    private readonly ICloudSessionsRepository _mockCloudSessionsRepository;
    private readonly IInventoryRepository _mockInventoryRepository;
    private readonly IInvokeClientsService _mockInvokeClientsService;
    private readonly ILogger<RemoveDataNodeCommandHandler> _mockLogger;

    private readonly RemoveDataNodeCommandHandler _removeDataNodeCommandHandler;

    public RemoveDataNodeCommandHandlerTests()
    {
        _mockInventoryMemberService = A.Fake<IInventoryMemberService>();
        _mockInventoryRepository = A.Fake<IInventoryRepository>();
        _mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        _mockInvokeClientsService = A.Fake<IInvokeClientsService>();
        _mockLogger = A.Fake<ILogger<RemoveDataNodeCommandHandler>>();

        _removeDataNodeCommandHandler = new RemoveDataNodeCommandHandler(_mockInventoryMemberService, _mockInventoryRepository,
            _mockCloudSessionsRepository, _mockInvokeClientsService, _mockLogger);
    }

    [Test]
    public async Task RemoveDataNode_InventoryNotStarted_DoesNothingWhenNoDataNode()
    {
        // Arrange
        var sessionId = "testSession";
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        var encryptedDataNode = new EncryptedDataNode{ Id = "dataNode1" };
        var inventoryData = new InventoryEntity(sessionId);

        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId))
            .Returns(new CloudSessionData(null, new EncryptedSessionSettings(), client));

        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(A<string>.Ignored, A<Func<InventoryEntity?, InventoryEntity?>>.Ignored))
            .Invokes((string _, Func<InventoryEntity, InventoryEntity> func) => func(inventoryData))
            .Returns(new UpdateEntityResult<InventoryEntity>(inventoryData, UpdateEntityStatus.WaitingForTransaction));

        A.CallTo(() => _mockInventoryMemberService.GetOrCreateInventoryMember(A<InventoryEntity>.Ignored, sessionId, client))
            .Returns(new InventoryMemberEntity { ClientInstanceId = client.ClientInstanceId });

        var request = new RemoveDataNodeRequest(sessionId, client, client.ClientInstanceId, encryptedDataNode);

        // Act
        await _removeDataNodeCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(sessionId, A<Func<InventoryEntity?, InventoryEntity?>>.Ignored)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockInventoryMemberService.GetOrCreateInventoryMember(A<InventoryEntity>.Ignored, sessionId, client)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task RemoveDataNode_InventoryNotStarted_RemovesDataNodeCorrectly()
    {
        // Arrange
        var sessionId = "testSession";
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        var encryptedDataNode = new EncryptedDataNode{ Id = "dataNode1" };
        var inventoryData = new InventoryEntity(sessionId);
        inventoryData.InventoryMembers.Add(new InventoryMemberEntity { ClientInstanceId = client.ClientInstanceId, DataNodes = [ encryptedDataNode ], DataSources = [] });

        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId))
            .Returns(new CloudSessionData(null, new EncryptedSessionSettings(), client));

        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(A<string>.Ignored, A<Func<InventoryEntity?, InventoryEntity?>>.Ignored))
            .Invokes((string _, Func<InventoryEntity, InventoryEntity> func) => func(inventoryData))
            .Returns(new UpdateEntityResult<InventoryEntity>(inventoryData, UpdateEntityStatus.WaitingForTransaction));

        A.CallTo(() => _mockInventoryMemberService.GetOrCreateInventoryMember(A<InventoryEntity>.Ignored, sessionId, client))
            .Returns(new InventoryMemberEntity { ClientInstanceId = client.ClientInstanceId });

        var request = new RemoveDataNodeRequest(sessionId, client, client.ClientInstanceId, encryptedDataNode);

        // Act
        await _removeDataNodeCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(sessionId, A<Func<InventoryEntity?, InventoryEntity?>>.Ignored)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockInventoryMemberService.GetOrCreateInventoryMember(A<InventoryEntity>.Ignored, sessionId, client)).MustHaveHappenedOnceExactly();
    }
}
