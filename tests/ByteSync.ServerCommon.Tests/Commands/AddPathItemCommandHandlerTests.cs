using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Interfaces.Hub;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.Inventories;
using ByteSync.ServerCommon.Interfaces.Hubs;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NUnit.Framework.Legacy;

namespace ByteSync.ServerCommon.Tests.Commands;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class AddPathItemCommandHandlerTests
{
    private readonly IInventoryMemberService _mockInventoryMemberService;
    private readonly ICloudSessionsRepository _mockCloudSessionsRepository;
    private readonly IInventoryRepository _mockInventoryRepository;
    private readonly IByteSyncClientCaller _mockByteSyncClientCaller;
    private readonly ILogger<AddPathItemCommandHandler> _mockLogger;
    private readonly IHubByteSyncPush _mockByteSyncPush = A.Fake<IHubByteSyncPush>(x => x.Strict());
    
    private readonly AddPathItemCommandHandler _addPathItemCommandHandler;

    public AddPathItemCommandHandlerTests()
    {
        _mockInventoryMemberService = A.Fake<IInventoryMemberService>();
        _mockInventoryRepository = A.Fake<IInventoryRepository>();
        _mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        _mockByteSyncClientCaller = A.Fake<IByteSyncClientCaller>();
        _mockLogger = A.Fake<ILogger<AddPathItemCommandHandler>>();
        
        _addPathItemCommandHandler = new AddPathItemCommandHandler(_mockInventoryMemberService, _mockInventoryRepository, _mockCloudSessionsRepository, 
            _mockByteSyncClientCaller, _mockLogger);
    }
    
    [Test]
    public async Task AddPathItem_InventoryNotStarted_AddsPathItemCorrectly()
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
            .Returns(new UpdateEntityResult<InventoryData>(inventoryData, UpdateEntityStatus.Saved));

        A.CallTo(() => _mockByteSyncClientCaller.SessionGroupExcept(sessionId, client)).Returns(_mockByteSyncPush);

        A.CallTo(() => _mockByteSyncPush.PathItemAdded(A<PathItemDTO>.Ignored)).Returns(Task.CompletedTask);

        var request = new AddPathItemRequest(sessionId, client, encryptedPathItem);
        
        // Act
        await _addPathItemCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        ClassicAssert.IsTrue(inventoryData.InventoryMembers.Any(imd =>
            imd.ClientInstanceId == client.ClientInstanceId && imd.SharedPathItems.Contains(encryptedPathItem)));

        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(sessionId, A<Func<InventoryData?, InventoryData?>>.Ignored)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockByteSyncClientCaller.SessionGroupExcept(A<string>.Ignored, A<Client>.Ignored)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockByteSyncPush.PathItemAdded(A<PathItemDTO>.Ignored)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task AddPathItem_InventoryStarted_AddsPathItemCorrectly()
    {
        // Arrange
        var sessionId = "testSession";
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        var encryptedPathItem = new EncryptedPathItem { Code = "pathItem1" };
        var inventoryData = new InventoryData(sessionId);
        inventoryData.IsInventoryStarted = true;

        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId))
            .Returns(new CloudSessionData(null, new EncryptedSessionSettings(), client));

        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(A<string>.Ignored, A<Func<InventoryData?, InventoryData?>>.Ignored))
            .Invokes((string _, Func<InventoryData, InventoryData> func) => func(inventoryData));

        A.CallTo(() => _mockByteSyncClientCaller.SessionGroupExcept(sessionId, client)).Returns(_mockByteSyncPush);

        A.CallTo(() => _mockByteSyncPush.PathItemAdded(A<PathItemDTO>.Ignored)).Returns(Task.CompletedTask);

        var request = new AddPathItemRequest(sessionId, client, encryptedPathItem);
        
        // Act
        await _addPathItemCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        inventoryData.InventoryMembers.Count.Should().Be(0);
        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(sessionId, A<Func<InventoryData?, InventoryData?>>.Ignored)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task AddPathItem_InventoryNotStartedAndPathItemAlreadyExists_DoesNotAddPathItem()
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
            .Invokes((string _, Func<InventoryData, InventoryData> func) => func(inventoryData));

        var request = new AddPathItemRequest(sessionId, client, encryptedPathItem);
        
        // Act
        await _addPathItemCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        inventoryData.InventoryMembers.Count.Should().Be(1);
        inventoryData.InventoryMembers[0].SharedPathItems.Count.Should().Be(1);
        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(sessionId, A<Func<InventoryData?, InventoryData?>>.Ignored)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task AddPathItem_InventoryNotStartedAndPathItemAlreadyExistsForAnotherClient_AddsPathItemCorrectly()
    {
        // Arrange
        var sessionId = "testSession";
        var client1 = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        var encryptedPathItem1 = new EncryptedPathItem { Code = "pathItem1" };
        var encryptedPathItem2 = new EncryptedPathItem { Code = "pathItem2" };
        var inventoryData = new InventoryData(sessionId);
        inventoryData.InventoryMembers.Add(new InventoryMemberData
            { ClientInstanceId = "client2_CID2", SharedPathItems = new List<EncryptedPathItem> { encryptedPathItem2 } });

        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId))
            .Returns(new CloudSessionData(null, new EncryptedSessionSettings(), client1));

        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(A<string>.Ignored, A<Func<InventoryData?, InventoryData?>>.Ignored))
            .Invokes((string _, Func<InventoryData, InventoryData> func) => func(inventoryData));

        var request = new AddPathItemRequest(sessionId, client1, encryptedPathItem1);
        
        // Act
        await _addPathItemCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        inventoryData.InventoryMembers.Count.Should().Be(2);
        inventoryData.InventoryMembers[0].SharedPathItems.Count.Should().Be(1);
        inventoryData.InventoryMembers[0].SharedPathItems[0].Code.Should().Be(encryptedPathItem2.Code);
        inventoryData.InventoryMembers[1].SharedPathItems.Count.Should().Be(1);
        inventoryData.InventoryMembers[1].SharedPathItems[0].Code.Should().Be(encryptedPathItem1.Code);
        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(sessionId, A<Func<InventoryData?, InventoryData?>>.Ignored)).MustHaveHappenedOnceExactly();
    }
}