﻿using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.Inventories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using FakeItEasy;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Tests.Commands.Inventories;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class RemovePathItemCommandHandlerTests
{
    private readonly IInventoryMemberService _mockInventoryMemberService;
    private readonly ICloudSessionsRepository _mockCloudSessionsRepository;
    private readonly IInventoryRepository _mockInventoryRepository;
    private readonly IInvokeClientsService _mockInvokeClientsService;
    private readonly ILogger<RemovePathItemCommandHandler> _mockLogger;
    
    private readonly RemovePathItemCommandHandler _removePathItemCommandHandler;

    public RemovePathItemCommandHandlerTests()
    {
        _mockInventoryMemberService = A.Fake<IInventoryMemberService>();
        _mockInventoryRepository = A.Fake<IInventoryRepository>();
        _mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        _mockInvokeClientsService = A.Fake<IInvokeClientsService>();
        _mockLogger = A.Fake<ILogger<RemovePathItemCommandHandler>>();
        
        _removePathItemCommandHandler = new RemovePathItemCommandHandler(_mockInventoryMemberService, _mockInventoryRepository, _mockCloudSessionsRepository, 
            _mockInvokeClientsService, _mockLogger);
    }
    
    [Test]
    public async Task RemovePathItem_InventoryNotStarted_DoesNothingWhenNoSharedPathItem()
    {
        // Arrange
        var sessionId = "testSession";
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        var encryptedPathItem = new EncryptedPathItem { Code = "pathItem1" };
        var inventoryData = new InventoryData(sessionId);

        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId))
            .Returns(new CloudSessionData(null, new EncryptedSessionSettings(), client));

        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(A<string>.Ignored, A<Func<InventoryData?, InventoryData?>>.Ignored))
            .Invokes((string _, Func<InventoryData, InventoryData> func) => func(inventoryData))
            .Returns(new UpdateEntityResult<InventoryData>(inventoryData, UpdateEntityStatus.WaitingForTransaction));
        
        A.CallTo(() => _mockInventoryMemberService.GetOrCreateInventoryMember(A<InventoryData>.Ignored, "testSession", client))
            .Returns(new InventoryMemberData { ClientInstanceId = client.ClientInstanceId });

        var request = new RemovePathItemRequest(sessionId, client, encryptedPathItem);

        // Act
        await _removePathItemCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(sessionId, A<Func<InventoryData?, InventoryData?>>.Ignored)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockInventoryMemberService.GetOrCreateInventoryMember(A<InventoryData>.Ignored, "testSession", client)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task RemovePathItem_InventoryNotStarted_RemovesPathItemCorrectly()
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

        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(A<string>.Ignored, A<Func<InventoryData?, InventoryData?>>.Ignored))
            .Invokes((string _, Func<InventoryData, InventoryData> func) => func(inventoryData))
            .Returns(new UpdateEntityResult<InventoryData>(inventoryData, UpdateEntityStatus.WaitingForTransaction));
        
        A.CallTo(() => _mockInventoryMemberService.GetOrCreateInventoryMember(A<InventoryData>.Ignored, "testSession", client))
            .Returns(new InventoryMemberData { ClientInstanceId = client.ClientInstanceId });

        var request = new RemovePathItemRequest(sessionId, client, encryptedPathItem);

        // Act
        await _removePathItemCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(sessionId, A<Func<InventoryData?, InventoryData?>>.Ignored)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockInventoryMemberService.GetOrCreateInventoryMember(A<InventoryData>.Ignored, "testSession", client)).MustHaveHappenedOnceExactly();
    }
}