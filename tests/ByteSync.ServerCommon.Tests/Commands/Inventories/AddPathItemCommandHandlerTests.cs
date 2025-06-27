using ByteSync.Common.Business.Inventories;
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
public class AddPathItemCommandHandlerTests
{
    private readonly IInventoryMemberService _mockInventoryMemberService;
    private readonly ICloudSessionsRepository _mockCloudSessionsRepository;
    private readonly IInventoryRepository _mockInventoryRepository;
    private readonly IInvokeClientsService _mockInvokeClientsService;
    private readonly ILogger<AddPathItemCommandHandler> _mockLogger;
    private readonly IHubByteSyncPush _mockByteSyncPush = A.Fake<IHubByteSyncPush>(x => x.Strict());
    
    private readonly AddPathItemCommandHandler _addPathItemCommandHandler;

    public AddPathItemCommandHandlerTests()
    {
        _mockInventoryMemberService = A.Fake<IInventoryMemberService>();
        _mockInventoryRepository = A.Fake<IInventoryRepository>();
        _mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        _mockInvokeClientsService = A.Fake<IInvokeClientsService>();
        _mockLogger = A.Fake<ILogger<AddPathItemCommandHandler>>();
        
        _addPathItemCommandHandler = new AddPathItemCommandHandler(_mockInventoryMemberService, _mockInventoryRepository, _mockCloudSessionsRepository, 
            _mockInvokeClientsService, _mockLogger);
    }
    
    [Test]
    public async Task AddPathItem_InventoryNotStarted_AddsPathItemCorrectly()
    {
        // Arrange
        var sessionId = "testSession";
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        var encryptedPathItem = new EncryptedDataSource { Code = "pathItem1" };
        var inventoryData = new InventoryData(sessionId);

        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId))
            .Returns(new CloudSessionData(null, new EncryptedSessionSettings(), client));

        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(A<string>.Ignored, A<Func<InventoryData?, InventoryData?>>.Ignored))
            .Invokes((string _, Func<InventoryData, InventoryData> func) => func(inventoryData))
            .Returns(new UpdateEntityResult<InventoryData>(inventoryData, UpdateEntityStatus.Saved));
        
        A.CallTo(() => _mockInventoryMemberService.GetOrCreateInventoryMember(A<InventoryData>.Ignored, "testSession", client))
            .Returns(new InventoryMemberData { ClientInstanceId = client.ClientInstanceId });

        A.CallTo(() => _mockInvokeClientsService.SessionGroupExcept(sessionId, client)).Returns(_mockByteSyncPush);

        A.CallTo(() => _mockByteSyncPush.DataSourceAdded(A<DataSourceDTO>.Ignored)).Returns(Task.CompletedTask);

        var request = new AddPathItemRequest(sessionId, client, encryptedPathItem);
        
        // Act
        await _addPathItemCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(sessionId, A<Func<InventoryData?, InventoryData?>>.Ignored)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockInvokeClientsService.SessionGroupExcept(A<string>.Ignored, A<Client>.Ignored)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockByteSyncPush.DataSourceAdded(A<DataSourceDTO>.Ignored)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockByteSyncPush.DataSourceAdded(A<DataSourceDTO>.Ignored)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockInventoryMemberService.GetOrCreateInventoryMember(A<InventoryData>.Ignored, "testSession", client)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task AddPathItem_InventoryStarted_AddsPathItemCorrectly()
    {
        // Arrange
        var sessionId = "testSession";
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        var encryptedPathItem = new EncryptedDataSource { Code = "pathItem1" };
        var inventoryData = new InventoryData(sessionId);
        inventoryData.IsInventoryStarted = true;

        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId))
            .Returns(new CloudSessionData(null, new EncryptedSessionSettings(), client));

        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(A<string>.Ignored, A<Func<InventoryData?, InventoryData?>>.Ignored))
            .Invokes((string _, Func<InventoryData, InventoryData> func) => func(inventoryData));

        A.CallTo(() => _mockInvokeClientsService.SessionGroupExcept(sessionId, client)).Returns(_mockByteSyncPush);

        A.CallTo(() => _mockByteSyncPush.DataSourceAdded(A<DataSourceDTO>.Ignored)).Returns(Task.CompletedTask);

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
        var encryptedPathItem = new EncryptedDataSource { Code = "pathItem1" };
        var inventoryData = new InventoryData(sessionId);
        inventoryData.InventoryMembers.Add(new InventoryMemberData
            { ClientInstanceId = client.ClientInstanceId, SharedPathItems = new List<EncryptedDataSource> { encryptedPathItem } });

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
}