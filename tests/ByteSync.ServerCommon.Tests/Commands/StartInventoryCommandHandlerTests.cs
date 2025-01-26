using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.Inventories;
using ByteSync.ServerCommon.Interfaces.Hubs;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Services;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using RedLockNet;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Tests.Commands;

public class StartInventoryCommandHandlerTests
{
    private readonly ICloudSessionsRepository _mockCloudSessionsRepository;
    private readonly IInventoryRepository _mockInventoryRepository;
    private readonly ISharedFilesService _mockSharedFilesService;
    private readonly IByteSyncClientCaller _mockByteSyncClientCaller;
    private readonly ICacheService _mockCacheService;
    private readonly ILogger<StartInventoryCommandHandler> _mockLogger;
    
    private readonly StartInventoryCommandHandler _startInventoryCommandHandler;

    public StartInventoryCommandHandlerTests()
    {
        _mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        _mockInventoryRepository = A.Fake<IInventoryRepository>();
        _mockSharedFilesService = A.Fake<ISharedFilesService>();
        _mockByteSyncClientCaller = A.Fake<IByteSyncClientCaller>();
        _mockCacheService = A.Fake<ICacheService>();
        _mockLogger = A.Fake<ILogger<StartInventoryCommandHandler>>();
        
        _startInventoryCommandHandler = new StartInventoryCommandHandler(_mockInventoryRepository, _mockCloudSessionsRepository, _mockSharedFilesService, 
            _mockByteSyncClientCaller, _mockCacheService, _mockLogger);
    }
    
    [Test]
    public async Task StartInventory_SessionNotFound_ReturnsSessionNotFoundResult()
    {
        // Arrange
        // var mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        // var mockInventoryRepository = A.Fake<IInventoryRepository>();
        // var mockSharedFilesService = A.Fake<ISharedFilesService>();
        // var mockByteSyncClientCaller = A.Fake<IByteSyncClientCaller>();
        // var mockCacheService = A.Fake<ICacheService>();
        // var mockLogger = A.Fake<ILogger<InventoryService>>();

        var sessionId = "testSession";

        A.CallTo(() => _mockCloudSessionsRepository.UpdateIfExists(A<string>.Ignored, A<Func<CloudSessionData, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Returns(new UpdateEntityResult<CloudSessionData>(null!, UpdateEntityStatus.NotFound));

        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        
        var request = new StartInventoryRequest(sessionId, client);

        // Act
        var result = await _startInventoryCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StartInventoryStatuses.SessionNotFound);
    }
    
    // [Test]
    // public async Task StartInventory_SessionNotFound_ReturnsSessionNotFoundResult()
    // {
    //     // // Arrange
    //     // var mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
    //     // var mockInventoryRepository = A.Fake<IInventoryRepository>();
    //     // var mockSharedFilesService = A.Fake<ISharedFilesService>();
    //     // var mockByteSyncClientCaller = A.Fake<IByteSyncClientCaller>();
    //     // var mockCacheService = A.Fake<ICacheService>();
    //     // var mockLogger = A.Fake<ILogger<InventoryService>>();
    //
    //     var sessionId = "testSession";
    //
    //     A.CallTo(() => _mockCloudSessionsRepository.UpdateIfExists(A<string>.Ignored, A<Func<CloudSessionData, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
    //         .Returns(new UpdateEntityResult<CloudSessionData>(null!, UpdateEntityStatus.NotFound));
    //
    //     // var service = new InventoryService(mockCloudSessionsRepository, mockInventoryRepository, mockSharedFilesService,
    //     //     mockByteSyncClientCaller, mockCacheService, mockLogger);
    //
    //     
    //     
    //     var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
    //     
    //     var request = new StartInventoryRequest(sessionId, client);
    //
    //     // Act
    //     var result = await _startInventoryCommandHandler.Handle(request, CancellationToken.None);
    //
    //     // Assert
    //     result.Status.Should().Be(StartInventoryStatuses.SessionNotFound);
    // }
    
    [Test]
    public async Task StartInventory_LessThan2Members_ReturnsLessThan2MembersResult()
    {
        // Arrange
        var sessionId = "testSession";
        var inventoryData = new InventoryData(sessionId);
        var cloudSessionData = new CloudSessionData(null, new EncryptedSessionSettings(),
            new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" });
        cloudSessionData.SessionMembers.Add(new SessionMemberData("client1", "client1", new PublicKeyInfo(), null, cloudSessionData));
        
        A.CallTo(() => _mockCloudSessionsRepository.UpdateIfExists(A<string>.Ignored, A<Func<CloudSessionData, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<CloudSessionData, bool> func, ITransaction _, IRedLock _) => func(cloudSessionData))
            .Returns(new UpdateEntityResult<CloudSessionData>(cloudSessionData, UpdateEntityStatus.WaitingForTransaction));
        
        A.CallTo(() => _mockInventoryRepository.Update(A<string>.Ignored, A<Func<InventoryData, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<InventoryData, bool> func, ITransaction _, IRedLock _) => func(inventoryData))
            .Returns(new UpdateEntityResult<InventoryData>(inventoryData, UpdateEntityStatus.WaitingForTransaction));

        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };

        var request = new StartInventoryRequest(sessionId, client);

        // Act
        var result = await _startInventoryCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StartInventoryStatuses.LessThan2Members);
    }
    
    [Test]
    public async Task StartInventory_MoreThan5Members_ReturnsMoreThan5MembersResult()
    {
        // Arrange
        var sessionId = "testSession";
        var inventoryData = new InventoryData(sessionId);
        var cloudSessionData = new CloudSessionData(null, new EncryptedSessionSettings(),
            new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" });
        cloudSessionData.SessionMembers.Add(new SessionMemberData("client1", "client1", new PublicKeyInfo(), null, cloudSessionData));
        cloudSessionData.SessionMembers.Add(new SessionMemberData("client2", "client2", new PublicKeyInfo(), null, cloudSessionData));
        cloudSessionData.SessionMembers.Add(new SessionMemberData("client3", "client3", new PublicKeyInfo(), null, cloudSessionData));
        cloudSessionData.SessionMembers.Add(new SessionMemberData("client4", "client4", new PublicKeyInfo(), null, cloudSessionData));
        cloudSessionData.SessionMembers.Add(new SessionMemberData("client5", "client5", new PublicKeyInfo(), null, cloudSessionData));
        cloudSessionData.SessionMembers.Add(new SessionMemberData("client6", "client6", new PublicKeyInfo(), null, cloudSessionData));
        
        A.CallTo(() => _mockCloudSessionsRepository.UpdateIfExists(A<string>.Ignored, A<Func<CloudSessionData, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<CloudSessionData, bool> func, ITransaction _, IRedLock _) => func(cloudSessionData))
            .Returns(new UpdateEntityResult<CloudSessionData>(cloudSessionData, UpdateEntityStatus.WaitingForTransaction));

        A.CallTo(() => _mockInventoryRepository.Update(A<string>.Ignored, A<Func<InventoryData, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<InventoryData, bool> func, ITransaction _, IRedLock _) => func(inventoryData))
            .Returns(new UpdateEntityResult<InventoryData>(inventoryData, UpdateEntityStatus.WaitingForTransaction));
        
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };

        var request = new StartInventoryRequest(sessionId, client);

        // Act
        var result = await _startInventoryCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StartInventoryStatuses.MoreThan5Members);
    }
    
    [Test]
    public async Task StartInventory_UnknownError_ReturnsUnknownErrorResult()
    {
        // Arrange
        var sessionId = "testSession";
        var inventoryData = new InventoryData(sessionId);
        var cloudSessionData = new CloudSessionData(null, new EncryptedSessionSettings(),
            new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" });
        cloudSessionData.SessionMembers.Add(new SessionMemberData("client1", "client1", new PublicKeyInfo(), null, cloudSessionData));
        cloudSessionData.SessionMembers.Add(new SessionMemberData("client2", "client2", new PublicKeyInfo(), null, cloudSessionData));

        A.CallTo(() => _mockCloudSessionsRepository.UpdateIfExists(A<string>.Ignored, A<Func<CloudSessionData, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<CloudSessionData, bool> func, ITransaction _, IRedLock _) => func(cloudSessionData))
            .Returns(new UpdateEntityResult<CloudSessionData>(cloudSessionData, UpdateEntityStatus.WaitingForTransaction));
        
        A.CallTo(() => _mockInventoryRepository.Update(A<string>.Ignored, A<Func<InventoryData, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<InventoryData, bool> func, ITransaction _, IRedLock _) => func(inventoryData))
            .Returns(new UpdateEntityResult<InventoryData>(inventoryData, UpdateEntityStatus.WaitingForTransaction));
        
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };

        var request = new StartInventoryRequest(sessionId, client);

        // Act
        var result = await _startInventoryCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StartInventoryStatuses.UnknownError);
    }
    
    [Test]
    public async Task StartInventory_AtLeastOneMemberWithNoDataToSynchronize_ReturnsAtLeastOneMemberWithNoDataToSynchronizeResult()
    {
        // Arrange
        var sessionId = "testSession";
        var inventoryData = new InventoryData(sessionId);
        var encryptedPathItem = new EncryptedPathItem { Code = "pathItem1" };
        var cloudSessionData = new CloudSessionData(null, new EncryptedSessionSettings(),
            new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" });
        cloudSessionData.SessionMembers.Add(new SessionMemberData("client1", "client1", new PublicKeyInfo(), null, cloudSessionData));
        cloudSessionData.SessionMembers.Add(new SessionMemberData("client2", "client2", new PublicKeyInfo(), null, cloudSessionData));
        inventoryData.InventoryMembers.Add(new InventoryMemberData
            { ClientInstanceId = "client1", SharedPathItems = new List<EncryptedPathItem> { encryptedPathItem } });
        inventoryData.InventoryMembers.Add(new InventoryMemberData { ClientInstanceId = "client2", SharedPathItems = new List<EncryptedPathItem>() });
        
        A.CallTo(() => _mockCloudSessionsRepository.UpdateIfExists(A<string>.Ignored, A<Func<CloudSessionData, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<CloudSessionData, bool> func, ITransaction _, IRedLock _) => func(cloudSessionData))
            .Returns(new UpdateEntityResult<CloudSessionData>(cloudSessionData, UpdateEntityStatus.WaitingForTransaction));
        
        A.CallTo(() => _mockInventoryRepository.Update(A<string>.Ignored, A<Func<InventoryData, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<InventoryData, bool> func, ITransaction _, IRedLock _) => func(inventoryData))
            .Returns(new UpdateEntityResult<InventoryData>(inventoryData, UpdateEntityStatus.WaitingForTransaction));

        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };

        var request = new StartInventoryRequest(sessionId, client);

        // Act
        var result = await _startInventoryCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StartInventoryStatuses.AtLeastOneMemberWithNoDataToSynchronize);
    }
    
    [Test]
    public async Task StartInventory_AllMembersHaveDataToSynchronize_ReturnsSuccessResultAndCallsSharedFilesServiceCorrectly()
    {
        // Arrange
        var sessionId = "testSession";
        var inventoryData = new InventoryData(sessionId);
        var cloudSessionData = new CloudSessionData(null, new EncryptedSessionSettings(),
            new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" });
        cloudSessionData.SessionMembers.Add(new SessionMemberData("client1", "client1", new PublicKeyInfo(), null, cloudSessionData));
        cloudSessionData.SessionMembers.Add(new SessionMemberData("client2", "client2", new PublicKeyInfo(), null, cloudSessionData));
        inventoryData.InventoryMembers.Add(new InventoryMemberData
            { ClientInstanceId = "client1", SharedPathItems = new List<EncryptedPathItem> { new() { Code = "pathItem1" } } });
        inventoryData.InventoryMembers.Add(new InventoryMemberData
            { ClientInstanceId = "client2", SharedPathItems = new List<EncryptedPathItem> { new() { Code = "pathItem2" } } });

        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId))
            .Returns(cloudSessionData);

        A.CallTo(() => _mockCloudSessionsRepository.UpdateIfExists(A<string>.Ignored, A<Func<CloudSessionData, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<CloudSessionData, bool> func, ITransaction _, IRedLock _) => func(cloudSessionData))
            .Returns(new UpdateEntityResult<CloudSessionData>(cloudSessionData, UpdateEntityStatus.WaitingForTransaction));
        
        A.CallTo(() => _mockInventoryRepository.Update(A<string>.Ignored, A<Func<InventoryData, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<InventoryData, bool> func, ITransaction _, IRedLock _) => func(inventoryData))
            .Returns(new UpdateEntityResult<InventoryData>(inventoryData, UpdateEntityStatus.WaitingForTransaction));

        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };

        var request = new StartInventoryRequest(sessionId, client);

        // Act
        var result = await _startInventoryCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StartInventoryStatuses.InventoryStartedSucessfully);
        A.CallTo(() => _mockSharedFilesService.ClearSession(sessionId)).MustHaveHappenedOnceExactly();
    }
}