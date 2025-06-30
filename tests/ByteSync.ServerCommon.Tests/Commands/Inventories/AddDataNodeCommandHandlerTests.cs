using System;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Interfaces.Hub;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.Inventories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Tests.Commands.Inventories;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class AddDataNodeCommandHandlerTests
{
    private readonly IInventoryMemberService _mockInventoryMemberService;
    private readonly ICloudSessionsRepository _mockCloudSessionsRepository;
    private readonly IInventoryRepository _mockInventoryRepository;
    private readonly IInvokeClientsService _mockInvokeClientsService;
    private readonly ILogger<AddDataNodeCommandHandler> _mockLogger;
    private readonly IHubByteSyncPush _mockByteSyncPush = A.Fake<IHubByteSyncPush>(x => x.Strict());

    private readonly AddDataNodeCommandHandler _addDataNodeCommandHandler;

    public AddDataNodeCommandHandlerTests()
    {
        _mockInventoryMemberService = A.Fake<IInventoryMemberService>();
        _mockInventoryRepository = A.Fake<IInventoryRepository>();
        _mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        _mockInvokeClientsService = A.Fake<IInvokeClientsService>();
        _mockLogger = A.Fake<ILogger<AddDataNodeCommandHandler>>();

        _addDataNodeCommandHandler = new AddDataNodeCommandHandler(_mockInventoryMemberService, _mockInventoryRepository,
            _mockCloudSessionsRepository, _mockInvokeClientsService, _mockLogger);
    }

    [Test]
    public async Task AddDataNode_InventoryNotStarted_AddsDataNodeCorrectly()
    {
        // Arrange
        var sessionId = "testSession";
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        var encryptedDataNode = new EncryptedDataNode { Data = new byte[] { 1 }, IV = new byte[] { 2 } };
        var inventoryData = new InventoryData(sessionId);

        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId))
            .Returns(new CloudSessionData(null, new EncryptedSessionSettings(), client));

        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(A<string>.Ignored, A<Func<InventoryData?, InventoryData?>>.Ignored))
            .Invokes((string _, Func<InventoryData, InventoryData> func) => func(inventoryData))
            .Returns(new UpdateEntityResult<InventoryData>(inventoryData, UpdateEntityStatus.Saved));

        A.CallTo(() => _mockInventoryMemberService.GetOrCreateInventoryMember(A<InventoryData>.Ignored, sessionId, client))
            .Returns(new InventoryMemberData { ClientInstanceId = client.ClientInstanceId });

        A.CallTo(() => _mockInvokeClientsService.SessionGroupExcept(sessionId, client)).Returns(_mockByteSyncPush);
        A.CallTo(() => _mockByteSyncPush.DataNodeAdded(A<DataNodeDTO>.Ignored)).Returns(Task.CompletedTask);

        var request = new AddDataNodeRequest(sessionId, client, encryptedDataNode);

        // Act
        await _addDataNodeCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        inventoryData.InventoryMembers.Should().ContainSingle();
        inventoryData.InventoryMembers[0].DataNodes.Should().Contain(encryptedDataNode);
        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(sessionId, A<Func<InventoryData?, InventoryData?>>.Ignored)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockInvokeClientsService.SessionGroupExcept(A<string>.Ignored, A<Client>.Ignored)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockByteSyncPush.DataNodeAdded(A<DataNodeDTO>.Ignored)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockInventoryMemberService.GetOrCreateInventoryMember(A<InventoryData>.Ignored, sessionId, client)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task AddDataNode_InventoryStarted_AddsDataNodeCorrectly()
    {
        // Arrange
        var sessionId = "testSession";
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        var encryptedDataNode = new EncryptedDataNode { Data = Array.Empty<byte>(), IV = Array.Empty<byte>() };
        var inventoryData = new InventoryData(sessionId);
        inventoryData.IsInventoryStarted = true;

        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId))
            .Returns(new CloudSessionData(null, new EncryptedSessionSettings(), client));

        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(A<string>.Ignored, A<Func<InventoryData?, InventoryData?>>.Ignored))
            .Invokes((string _, Func<InventoryData, InventoryData> func) => func(inventoryData));

        A.CallTo(() => _mockInvokeClientsService.SessionGroupExcept(sessionId, client)).Returns(_mockByteSyncPush);
        A.CallTo(() => _mockByteSyncPush.DataNodeAdded(A<DataNodeDTO>.Ignored)).Returns(Task.CompletedTask);

        var request = new AddDataNodeRequest(sessionId, client, encryptedDataNode);

        // Act
        await _addDataNodeCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        inventoryData.InventoryMembers.Count.Should().Be(0);
        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(sessionId, A<Func<InventoryData?, InventoryData?>>.Ignored)).MustHaveHappenedOnceExactly();
    }
}
