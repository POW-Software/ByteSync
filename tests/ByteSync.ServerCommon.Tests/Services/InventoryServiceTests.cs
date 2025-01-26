using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Interfaces.Hub;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Interfaces.Hubs;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Services;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using RedLockNet;
using StackExchange.Redis;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace ByteSync.ServerCommon.Tests.Services;

public class InventoryServiceTests
{
    [Test]
    public async Task AddPathItem_InventoryNotStarted_AddsPathItemCorrectly()
    {
        // Arrange
        var mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>(x => x.Strict());
        var mockInventoryRepository = A.Fake<IInventoryRepository>(x => x.Strict());
        var mockSharedFilesService = A.Fake<ISharedFilesService>(x => x.Strict());
        var mockByteSyncClientCaller = A.Fake<IByteSyncClientCaller>(x => x.Strict());
        var mockByteSyncPush = A.Fake<IHubByteSyncPush>(x => x.Strict());
        var mockCacheService = A.Fake<ICacheService>();
        var mockLogger = A.Fake<ILogger<InventoryService>>();

        var sessionId = "testSession";
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        var encryptedPathItem = new EncryptedPathItem { Code = "pathItem1" };
        var inventoryData = new InventoryData(sessionId);

        A.CallTo(() => mockCloudSessionsRepository.Get(sessionId))
            .Returns(new CloudSessionData(null, new EncryptedSessionSettings(), client));

        A.CallTo(() => mockInventoryRepository.AddOrUpdate(A<string>.Ignored, A<Func<InventoryData?, InventoryData?>>.Ignored))
            .Invokes((string _, Func<InventoryData, InventoryData> func) => func(inventoryData))
            .Returns(new UpdateEntityResult<InventoryData>(inventoryData, UpdateEntityStatus.Saved));

        A.CallTo(() => mockByteSyncClientCaller.SessionGroupExcept(sessionId, client)).Returns(mockByteSyncPush);

        A.CallTo(() => mockByteSyncPush.PathItemAdded(A<PathItemDTO>.Ignored)).Returns(Task.CompletedTask);

        var service = new InventoryService(mockCloudSessionsRepository, mockInventoryRepository, mockSharedFilesService, mockByteSyncClientCaller,
            mockCacheService, mockLogger);

        // Act
        await service.AddPathItem(sessionId, client, encryptedPathItem);

        // Assert
        Assert.IsTrue(inventoryData.InventoryMembers.Any(imd =>
            imd.ClientInstanceId == client.ClientInstanceId && imd.SharedPathItems.Contains(encryptedPathItem)));

        A.CallTo(() => mockCloudSessionsRepository.Get(sessionId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => mockInventoryRepository.AddOrUpdate(sessionId, A<Func<InventoryData?, InventoryData?>>.Ignored)).MustHaveHappenedOnceExactly();
        A.CallTo(() => mockByteSyncClientCaller.SessionGroupExcept(A<string>.Ignored, A<Client>.Ignored)).MustHaveHappenedOnceExactly();
        A.CallTo(() => mockByteSyncPush.PathItemAdded(A<PathItemDTO>.Ignored)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task AddPathItem_InventoryStarted_AddsPathItemCorrectly()
    {
        // Arrange
        var mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>(x => x.Strict());
        var mockInventoryRepository = A.Fake<IInventoryRepository>(x => x.Strict());
        var mockSharedFilesService = A.Fake<ISharedFilesService>(x => x.Strict());
        var mockByteSyncClientCaller = A.Fake<IByteSyncClientCaller>(x => x.Strict());
        var mockByteSyncPush = A.Fake<IHubByteSyncPush>(x => x.Strict());
        var mockCacheService = A.Fake<ICacheService>();
        var mockLogger = A.Fake<ILogger<InventoryService>>();

        var sessionId = "testSession";
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        var encryptedPathItem = new EncryptedPathItem { Code = "pathItem1" };
        var inventoryData = new InventoryData(sessionId);
        inventoryData.IsInventoryStarted = true;

        A.CallTo(() => mockCloudSessionsRepository.Get(sessionId))
            .Returns(new CloudSessionData(null, new EncryptedSessionSettings(), client));

        A.CallTo(() => mockInventoryRepository.AddOrUpdate(A<string>.Ignored, A<Func<InventoryData?, InventoryData?>>.Ignored))
            .Invokes((string _, Func<InventoryData, InventoryData> func) => func(inventoryData));

        A.CallTo(() => mockByteSyncClientCaller.SessionGroupExcept(sessionId, client)).Returns(mockByteSyncPush);

        A.CallTo(() => mockByteSyncPush.PathItemAdded(A<PathItemDTO>.Ignored)).Returns(Task.CompletedTask);

        var service = new InventoryService(mockCloudSessionsRepository, mockInventoryRepository, mockSharedFilesService, mockByteSyncClientCaller,
            mockCacheService, mockLogger);

        // Act
        await service.AddPathItem(sessionId, client, encryptedPathItem);

        // Assert
        inventoryData.InventoryMembers.Count.Should().Be(0);
        A.CallTo(() => mockInventoryRepository.AddOrUpdate(sessionId, A<Func<InventoryData?, InventoryData?>>.Ignored)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task AddPathItem_InventoryNotStartedAndPathItemAlreadyExists_DoesNotAddPathItem()
    {
        // Arrange
        var mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        var mockInventoryRepository = A.Fake<IInventoryRepository>();
        var mockSharedFilesService = A.Fake<ISharedFilesService>();
        var mockByteSyncClientCaller = A.Fake<IByteSyncClientCaller>();
        var mockCacheService = A.Fake<ICacheService>();
        var mockLogger = A.Fake<ILogger<InventoryService>>();

        var sessionId = "testSession";
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        var encryptedPathItem = new EncryptedPathItem { Code = "pathItem1" };
        var inventoryData = new InventoryData(sessionId);
        inventoryData.InventoryMembers.Add(new InventoryMemberData
            { ClientInstanceId = client.ClientInstanceId, SharedPathItems = new List<EncryptedPathItem> { encryptedPathItem } });

        A.CallTo(() => mockCloudSessionsRepository.Get(sessionId))
            .Returns(new CloudSessionData(null, new EncryptedSessionSettings(), client));

        A.CallTo(() => mockInventoryRepository.AddOrUpdate(A<string>.Ignored, A<Func<InventoryData?, InventoryData?>>.Ignored))
            .Invokes((string _, Func<InventoryData, InventoryData> func) => func(inventoryData));

        var service = new InventoryService(mockCloudSessionsRepository, mockInventoryRepository, mockSharedFilesService, mockByteSyncClientCaller,
            mockCacheService, mockLogger);

        // Act
        await service.AddPathItem(sessionId, client, encryptedPathItem);

        // Assert
        inventoryData.InventoryMembers.Count.Should().Be(1);
        inventoryData.InventoryMembers[0].SharedPathItems.Count.Should().Be(1);
        A.CallTo(() => mockInventoryRepository.AddOrUpdate(sessionId, A<Func<InventoryData?, InventoryData?>>.Ignored)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task AddPathItem_InventoryNotStartedAndPathItemAlreadyExistsForAnotherClient_AddsPathItemCorrectly()
    {
        // Arrange
        var mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        var mockInventoryRepository = A.Fake<IInventoryRepository>();
        var mockSharedFilesService = A.Fake<ISharedFilesService>();
        var mockByteSyncClientCaller = A.Fake<IByteSyncClientCaller>();
        var mockCacheService = A.Fake<ICacheService>();
        var mockLogger = A.Fake<ILogger<InventoryService>>();

        var sessionId = "testSession";
        var client1 = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        var encryptedPathItem1 = new EncryptedPathItem { Code = "pathItem1" };
        var encryptedPathItem2 = new EncryptedPathItem { Code = "pathItem2" };
        var inventoryData = new InventoryData(sessionId);
        inventoryData.InventoryMembers.Add(new InventoryMemberData
            { ClientInstanceId = "client2_CID2", SharedPathItems = new List<EncryptedPathItem> { encryptedPathItem2 } });

        A.CallTo(() => mockCloudSessionsRepository.Get(sessionId))
            .Returns(new CloudSessionData(null, new EncryptedSessionSettings(), client1));

        A.CallTo(() => mockInventoryRepository.AddOrUpdate(A<string>.Ignored, A<Func<InventoryData?, InventoryData?>>.Ignored))
            .Invokes((string _, Func<InventoryData, InventoryData> func) => func(inventoryData));

        var service = new InventoryService(mockCloudSessionsRepository, mockInventoryRepository, mockSharedFilesService, mockByteSyncClientCaller,
            mockCacheService, mockLogger);

        // Act
        await service.AddPathItem(sessionId, client1, encryptedPathItem1);

        // Assert
        inventoryData.InventoryMembers.Count.Should().Be(2);
        inventoryData.InventoryMembers[0].SharedPathItems.Count.Should().Be(1);
        inventoryData.InventoryMembers[0].SharedPathItems[0].Code.Should().Be(encryptedPathItem2.Code);
        inventoryData.InventoryMembers[1].SharedPathItems.Count.Should().Be(1);
        inventoryData.InventoryMembers[1].SharedPathItems[0].Code.Should().Be(encryptedPathItem1.Code);
        A.CallTo(() => mockInventoryRepository.AddOrUpdate(sessionId, A<Func<InventoryData?, InventoryData?>>.Ignored)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task RemovePathItem_InventoryNotStarted_DoesNothingWhenNoSharedPathItem()
    {
        // Arrange
        var mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        var mockInventoryRepository = A.Fake<IInventoryRepository>();
        var mockSharedFilesService = A.Fake<ISharedFilesService>();
        var mockByteSyncClientCaller = A.Fake<IByteSyncClientCaller>();
        var mockCacheService = A.Fake<ICacheService>();
        var mockLogger = A.Fake<ILogger<InventoryService>>();

        var sessionId = "testSession";
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        var encryptedPathItem = new EncryptedPathItem { Code = "pathItem1" };
        var inventoryData = new InventoryData(sessionId);

        A.CallTo(() => mockCloudSessionsRepository.Get(sessionId))
            .Returns(new CloudSessionData(null, new EncryptedSessionSettings(), client));

        A.CallTo(() => mockInventoryRepository.AddOrUpdate(A<string>.Ignored, A<Func<InventoryData?, InventoryData?>>.Ignored))
            .Invokes((string _, Func<InventoryData, InventoryData> func) => func(inventoryData))
            .Returns(new UpdateEntityResult<InventoryData>(inventoryData, UpdateEntityStatus.WaitingForTransaction));

        var service = new InventoryService(mockCloudSessionsRepository, mockInventoryRepository, mockSharedFilesService, mockByteSyncClientCaller,
            mockCacheService, mockLogger);

        // Act
        await service.RemovePathItem(sessionId, client, encryptedPathItem);

        // Assert
        Assert.IsTrue(inventoryData.InventoryMembers.Any(imd => imd.ClientInstanceId == client.ClientInstanceId && imd.SharedPathItems.Count == 0));
        A.CallTo(() => mockInventoryRepository.AddOrUpdate(sessionId, A<Func<InventoryData?, InventoryData?>>.Ignored)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task RemovePathItem_InventoryNotStarted_RemovesPathItemCorrectly()
    {
        // Arrange
        var mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        var mockInventoryRepository = A.Fake<IInventoryRepository>();
        var mockSharedFilesService = A.Fake<ISharedFilesService>();
        var mockByteSyncClientCaller = A.Fake<IByteSyncClientCaller>();
        var mockCacheService = A.Fake<ICacheService>();
        var mockLogger = A.Fake<ILogger<InventoryService>>();

        var sessionId = "testSession";
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        var encryptedPathItem = new EncryptedPathItem { Code = "pathItem1" };
        var inventoryData = new InventoryData(sessionId);
        inventoryData.InventoryMembers.Add(new InventoryMemberData
            { ClientInstanceId = client.ClientInstanceId, SharedPathItems = new List<EncryptedPathItem> { encryptedPathItem } });

        A.CallTo(() => mockCloudSessionsRepository.Get(sessionId))
            .Returns(new CloudSessionData(null, new EncryptedSessionSettings(), client));

        A.CallTo(() => mockInventoryRepository.AddOrUpdate(A<string>.Ignored, A<Func<InventoryData?, InventoryData?>>.Ignored))
            .Invokes((string _, Func<InventoryData, InventoryData> func) => func(inventoryData))
            .Returns(new UpdateEntityResult<InventoryData>(inventoryData, UpdateEntityStatus.WaitingForTransaction));

        var service = new InventoryService(mockCloudSessionsRepository, mockInventoryRepository, mockSharedFilesService, mockByteSyncClientCaller,
            mockCacheService, mockLogger);

        // Act
        await service.RemovePathItem(sessionId, client, encryptedPathItem);

        // Assert
        Assert.IsTrue(inventoryData.InventoryMembers.Any(imd => imd.ClientInstanceId == client.ClientInstanceId && imd.SharedPathItems.Count == 0));
        A.CallTo(() => mockInventoryRepository.AddOrUpdate(sessionId, A<Func<InventoryData?, InventoryData?>>.Ignored)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task GetPathItems_InventoryNotStarted_ReturnsNull()
    {
        // Arrange
        var mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        var mockInventoryRepository = A.Fake<IInventoryRepository>();
        var mockSharedFilesService = A.Fake<ISharedFilesService>();
        var mockByteSyncClientCaller = A.Fake<IByteSyncClientCaller>();
        var mockCacheService = A.Fake<ICacheService>();
        var mockLogger = A.Fake<ILogger<InventoryService>>();

        var sessionId = "testSession";
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        var encryptedPathItem = new EncryptedPathItem { Code = "pathItem1" };
        var inventoryData = new InventoryData(sessionId);
        inventoryData.InventoryMembers.Add(new InventoryMemberData
            { ClientInstanceId = client.ClientInstanceId, SharedPathItems = new List<EncryptedPathItem> { encryptedPathItem } });

        A.CallTo(() => mockCloudSessionsRepository.Get(sessionId))
            .Returns(new CloudSessionData(null, new EncryptedSessionSettings(), client));

        A.CallTo(() => mockInventoryRepository.Get(sessionId))
            .Returns(inventoryData);

        var service = new InventoryService(mockCloudSessionsRepository, mockInventoryRepository, mockSharedFilesService, mockByteSyncClientCaller,
            mockCacheService, mockLogger);

        // Act
        var pathItems = await service.GetPathItems(sessionId, client.ClientInstanceId);

        // Assert
        pathItems!.Count.Should().Be(1);
        A.CallTo(() => mockInventoryRepository.Get(sessionId)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task SetLocalInventoryStatus_UpdatesStatus_WhenUtcChangeDateIsLater()
    {
        // Arrange
        var mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        var mockInventoryRepository = A.Fake<IInventoryRepository>();
        var mockSharedFilesService = A.Fake<ISharedFilesService>();
        var mockByteSyncClientCaller = A.Fake<IByteSyncClientCaller>();
        var mockCacheService = A.Fake<ICacheService>();
        var mockLogger = A.Fake<ILogger<InventoryService>>();

        var sessionId = "testSession";
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        var parameters = new UpdateSessionMemberGeneralStatusParameters
        {
            SessionId = "testSession",
            UtcChangeDate = DateTime.UtcNow,
            SessionMemberGeneralStatus = SessionMemberGeneralStatus.InventoryRunningAnalysis
        };

        var inventoryData = new InventoryData(sessionId);
        inventoryData.InventoryMembers.Add(new InventoryMemberData { ClientInstanceId = client.ClientInstanceId });

        A.CallTo(() => mockInventoryRepository.Update(A<string>.Ignored, A<Func<InventoryData, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<InventoryData, bool> func, ITransaction _, IRedLock _) => func(inventoryData))
            .Returns(new UpdateEntityResult<InventoryData>(inventoryData, UpdateEntityStatus.Saved));

        var service = new InventoryService(mockCloudSessionsRepository, mockInventoryRepository, mockSharedFilesService, mockByteSyncClientCaller,
            mockCacheService, mockLogger);

        // Act
        var result = await service.SetLocalInventoryStatus(client, parameters);

        // Assert
        result.Should().Be(true);
        inventoryData.InventoryMembers.Single().SessionMemberGeneralStatus.Should().Be(parameters.SessionMemberGeneralStatus);
        inventoryData.InventoryMembers.Single().LastLocalInventoryStatusUpdate.Should().Be(parameters.UtcChangeDate);
        A.CallTo(() => mockInventoryRepository.Update(sessionId, A<Func<InventoryData, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task GetPathItems_NoPathItems_ReturnsEmptyList()
    {
        // Arrange
        var mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        var mockInventoryRepository = A.Fake<IInventoryRepository>();
        var mockSharedFilesService = A.Fake<ISharedFilesService>();
        var mockByteSyncClientCaller = A.Fake<IByteSyncClientCaller>();
        var mockCacheService = A.Fake<ICacheService>();
        var mockLogger = A.Fake<ILogger<InventoryService>>();

        var sessionId = "testSession";
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        var inventoryData = new InventoryData(sessionId);

        A.CallTo(() => mockInventoryRepository.Get(sessionId))
            .Returns(inventoryData);

        var service = new InventoryService(mockCloudSessionsRepository, mockInventoryRepository, mockSharedFilesService, mockByteSyncClientCaller,
            mockCacheService, mockLogger);

        // Act
        var pathItems = await service.GetPathItems(sessionId, client.ClientInstanceId);

        // Assert
        pathItems.Should().BeEmpty();
        A.CallTo(() => mockInventoryRepository.Get(sessionId)).MustHaveHappenedOnceExactly();
    }
    
    [Test]
    public async Task GetPathItems_OnePathItem_ReturnsPathItem()
    {
        // Arrange
        var mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        var mockInventoryRepository = A.Fake<IInventoryRepository>();
        var mockSharedFilesService = A.Fake<ISharedFilesService>();
        var mockByteSyncClientCaller = A.Fake<IByteSyncClientCaller>();
        var mockCacheService = A.Fake<ICacheService>();
        var mockLogger = A.Fake<ILogger<InventoryService>>();

        var sessionId = "testSession";
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        var pathItem = new EncryptedPathItem { Code = "pathItem1" };
        var inventoryData = new InventoryData(sessionId);
        inventoryData.InventoryMembers.Add(new InventoryMemberData { ClientInstanceId = client.ClientInstanceId, SharedPathItems = new List<EncryptedPathItem> { pathItem } });

        A.CallTo(() => mockInventoryRepository.Get(sessionId))
            .Returns(inventoryData);
        
        var service = new InventoryService(mockCloudSessionsRepository, mockInventoryRepository, mockSharedFilesService, mockByteSyncClientCaller,
            mockCacheService, mockLogger);

        // Act
        var pathItems = await service.GetPathItems(sessionId, client.ClientInstanceId);

        // Assert
        pathItems.Should().NotBeNull();
        pathItems!.Count.Should().Be(1);
        A.CallTo(() => mockInventoryRepository.Get(sessionId)).MustHaveHappenedOnceExactly();
    }
}