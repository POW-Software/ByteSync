using ByteSync.Common.Business.EndPoints;
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
using FluentAssertions;
using Microsoft.Extensions.Logging;
using RedLockNet;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Tests.Commands.Inventories;

public class StartInventoryCommandHandlerTests
{
    private ICloudSessionsRepository _mockCloudSessionsRepository;
    private IInventoryRepository _mockInventoryRepository;
    private ISharedFilesService _mockSharedFilesService;
    private IInvokeClientsService _mockInvokeClientsService;
    private IRedisInfrastructureService _mockRedisInfrastructureService;
    private ILogger<StartInventoryCommandHandler> _mockLogger;
    
    private StartInventoryCommandHandler _startInventoryCommandHandler;

    public StartInventoryCommandHandlerTests()
    {

    }

    [SetUp]
    public void SetUp()
    {
        _mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        _mockInventoryRepository = A.Fake<IInventoryRepository>();
        _mockSharedFilesService = A.Fake<ISharedFilesService>(options => options.Strict());
        _mockInvokeClientsService = A.Fake<IInvokeClientsService>();
        _mockRedisInfrastructureService = A.Fake<IRedisInfrastructureService>();
        _mockLogger = A.Fake<ILogger<StartInventoryCommandHandler>>();
        
        _startInventoryCommandHandler = new StartInventoryCommandHandler(_mockInventoryRepository, _mockCloudSessionsRepository, _mockSharedFilesService, 
            _mockInvokeClientsService, _mockRedisInfrastructureService, _mockLogger);
    }
    
    [Test]
    public async Task StartInventory_SessionNotFound_ReturnsSessionNotFoundResult()
    {
        // Arrange
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
    
    [Test]
    public async Task StartInventory_LessThan2Members_ReturnsLessThan2MembersResult()
    {
        // Arrange
        var sessionId = "testSession";
        var inventoryData = new InventoryEntity(sessionId);
        var cloudSessionData = new CloudSessionData(null, new EncryptedSessionSettings(),
            new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" });
        cloudSessionData.SessionMembers.Add(new SessionMemberData("client1", "client1", new PublicKeyInfo(), null, cloudSessionData));
        
        A.CallTo(() => _mockCloudSessionsRepository.UpdateIfExists(A<string>.Ignored, A<Func<CloudSessionData, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<CloudSessionData, bool> func, ITransaction _, IRedLock _) => func(cloudSessionData))
            .Returns(new UpdateEntityResult<CloudSessionData>(cloudSessionData, UpdateEntityStatus.WaitingForTransaction));
        
        A.CallTo(() => _mockInventoryRepository.UpdateIfExists(A<string>.Ignored, A<Func<InventoryEntity, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<InventoryEntity, bool> func, ITransaction _, IRedLock _) => func(inventoryData))
            .Returns(new UpdateEntityResult<InventoryEntity>(inventoryData, UpdateEntityStatus.WaitingForTransaction));

        A.CallTo(() => _mockSharedFilesService.ClearSession(sessionId)).DoesNothing();
        
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };

        var request = new StartInventoryRequest(sessionId, client);

        // Act
        var result = await _startInventoryCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StartInventoryStatuses.LessThan2Members);
        A.CallTo(() =>  _mockSharedFilesService.ClearSession(sessionId)).MustNotHaveHappened();
    }
    
    [Test]
    public async Task StartInventory_MoreThan5Members_ReturnsMoreThan5MembersResult()
    {
        // Arrange
        var sessionId = "testSession";
        var inventoryData = new InventoryEntity(sessionId);
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

        A.CallTo(() => _mockInventoryRepository.UpdateIfExists(A<string>.Ignored, A<Func<InventoryEntity, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<InventoryEntity, bool> func, ITransaction _, IRedLock _) => func(inventoryData))
            .Returns(new UpdateEntityResult<InventoryEntity>(inventoryData, UpdateEntityStatus.WaitingForTransaction));
        
        A.CallTo(() => _mockSharedFilesService.ClearSession(sessionId)).DoesNothing();
        
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };

        var request = new StartInventoryRequest(sessionId, client);

        // Act
        var result = await _startInventoryCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StartInventoryStatuses.MoreThan5Members);
        A.CallTo(() =>  _mockSharedFilesService.ClearSession(sessionId)).MustNotHaveHappened();
    }
    
    [Test]
    public async Task StartInventory_LessInventoryMembersThanSessionMembers_ReturnsAtLeastOneMemberWithNoDataToSynchronize()
    {
        // Arrange
        var sessionId = "testSession";
        var inventoryData = new InventoryEntity(sessionId);
        var cloudSessionData = new CloudSessionData(null, new EncryptedSessionSettings(),
            new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" });
        cloudSessionData.SessionMembers.Add(new SessionMemberData("client1", "client1", new PublicKeyInfo(), null, cloudSessionData));
        cloudSessionData.SessionMembers.Add(new SessionMemberData("client2", "client2", new PublicKeyInfo(), null, cloudSessionData));

        A.CallTo(() => _mockCloudSessionsRepository.UpdateIfExists(A<string>.Ignored, A<Func<CloudSessionData, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<CloudSessionData, bool> func, ITransaction _, IRedLock _) => func(cloudSessionData))
            .Returns(new UpdateEntityResult<CloudSessionData>(cloudSessionData, UpdateEntityStatus.WaitingForTransaction));
        
        A.CallTo(() => _mockInventoryRepository.UpdateIfExists(A<string>.Ignored, A<Func<InventoryEntity, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<InventoryEntity, bool> func, ITransaction _, IRedLock _) => func(inventoryData))
            .Returns(new UpdateEntityResult<InventoryEntity>(inventoryData, UpdateEntityStatus.WaitingForTransaction));
        
        A.CallTo(() => _mockSharedFilesService.ClearSession(sessionId)).DoesNothing();
        
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };

        var request = new StartInventoryRequest(sessionId, client);

        // Act
        var result = await _startInventoryCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StartInventoryStatuses.AtLeastOneMemberWithNoDataToSynchronize);
        A.CallTo(() =>  _mockSharedFilesService.ClearSession(sessionId)).MustHaveHappenedOnceExactly();
    }
    
        [Test]
    public async Task StartInventory_MoreInventoryMembersThanSessionMembers_ReturnsUnknownError()
    {
        // Arrange
        var sessionId = "testSession";
        var inventoryData = new InventoryEntity(sessionId);
        var cloudSessionData = new CloudSessionData(null, new EncryptedSessionSettings(),
            new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" });
        cloudSessionData.SessionMembers.Add(new SessionMemberData("client1", "client1", new PublicKeyInfo(), null, cloudSessionData));
        cloudSessionData.SessionMembers.Add(new SessionMemberData("client2", "client2", new PublicKeyInfo(), null, cloudSessionData));
        inventoryData.InventoryMembers.Add(new InventoryMemberEntity { ClientInstanceId = "client1" });
        inventoryData.InventoryMembers.Add(new InventoryMemberEntity { ClientInstanceId = "client2" });
        inventoryData.InventoryMembers.Add(new InventoryMemberEntity { ClientInstanceId = "client3" });

        A.CallTo(() => _mockCloudSessionsRepository.UpdateIfExists(A<string>.Ignored, A<Func<CloudSessionData, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<CloudSessionData, bool> func, ITransaction _, IRedLock _) => func(cloudSessionData))
            .Returns(new UpdateEntityResult<CloudSessionData>(cloudSessionData, UpdateEntityStatus.WaitingForTransaction));
        
        A.CallTo(() => _mockInventoryRepository.UpdateIfExists(A<string>.Ignored, A<Func<InventoryEntity, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<InventoryEntity, bool> func, ITransaction _, IRedLock _) => func(inventoryData))
            .Returns(new UpdateEntityResult<InventoryEntity>(inventoryData, UpdateEntityStatus.WaitingForTransaction));
        
        A.CallTo(() => _mockSharedFilesService.ClearSession(sessionId)).DoesNothing();
        
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };

        var request = new StartInventoryRequest(sessionId, client);

        // Act
        var result = await _startInventoryCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StartInventoryStatuses.UnknownError);
        A.CallTo(() =>  _mockSharedFilesService.ClearSession(sessionId)).MustHaveHappenedOnceExactly();
    }
    
    [Test]
    public async Task StartInventory_AtLeastOneMemberWithNoDataToSynchronize_ReturnsAtLeastOneMemberWithNoDataToSynchronizeResult()
    {
        // Arrange
        var sessionId = "testSession";
        var inventoryData = new InventoryEntity(sessionId);
        var encryptedDataSource = new EncryptedDataSource { Id = "dataSource1" };
        var cloudSessionData = new CloudSessionData(null, new EncryptedSessionSettings(),
            new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" });
        cloudSessionData.SessionMembers.Add(new SessionMemberData("client1", "client1", new PublicKeyInfo(), null, cloudSessionData));
        cloudSessionData.SessionMembers.Add(new SessionMemberData("client2", "client2", new PublicKeyInfo(), null, cloudSessionData));
        inventoryData.InventoryMembers.Add(new InventoryMemberEntity { ClientInstanceId = "client1", DataSources = [ encryptedDataSource ] });
        inventoryData.InventoryMembers.Add(new InventoryMemberEntity { ClientInstanceId = "client2" });
        
        A.CallTo(() => _mockCloudSessionsRepository.UpdateIfExists(A<string>.Ignored, A<Func<CloudSessionData, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<CloudSessionData, bool> func, ITransaction _, IRedLock _) => func(cloudSessionData))
            .Returns(new UpdateEntityResult<CloudSessionData>(cloudSessionData, UpdateEntityStatus.WaitingForTransaction));
        
        A.CallTo(() => _mockInventoryRepository.UpdateIfExists(A<string>.Ignored, A<Func<InventoryEntity, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<InventoryEntity, bool> func, ITransaction _, IRedLock _) => func(inventoryData))
            .Returns(new UpdateEntityResult<InventoryEntity>(inventoryData, UpdateEntityStatus.NoOperation));
        
        A.CallTo(() => _mockSharedFilesService.ClearSession(sessionId)).DoesNothing();

        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };

        var request = new StartInventoryRequest(sessionId, client);

        // Act
        var result = await _startInventoryCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StartInventoryStatuses.AtLeastOneMemberWithNoDataToSynchronize);
        A.CallTo(() =>  _mockSharedFilesService.ClearSession(sessionId)).MustNotHaveHappened();
    }
    
    [Test]
    public async Task StartInventory_AllMembersHaveDataToSynchronize_ReturnsSuccessResultAndCallsSharedFilesServiceCorrectly()
    {
        // Arrange
        var sessionId = "testSession";
        var inventoryData = new InventoryEntity(sessionId);
        var cloudSessionData = new CloudSessionData(null, new EncryptedSessionSettings(),
            new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" });
        cloudSessionData.SessionMembers.Add(new SessionMemberData("client1", "client1", new PublicKeyInfo(), null, cloudSessionData));
        cloudSessionData.SessionMembers.Add(new SessionMemberData("client2", "client2", new PublicKeyInfo(), null, cloudSessionData));
        inventoryData.InventoryMembers.Add(new InventoryMemberEntity
            { ClientInstanceId = "client1", DataSources = [ new() { Id = "dataSource1" } ] });
        inventoryData.InventoryMembers.Add(new InventoryMemberEntity
            { ClientInstanceId = "client2", DataSources = [ new() { Id = "dataSource2" } ] });

        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId))
            .Returns(cloudSessionData);

        A.CallTo(() => _mockCloudSessionsRepository.UpdateIfExists(A<string>.Ignored, A<Func<CloudSessionData, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<CloudSessionData, bool> func, ITransaction _, IRedLock _) => func(cloudSessionData))
            .Returns(new UpdateEntityResult<CloudSessionData>(cloudSessionData, UpdateEntityStatus.WaitingForTransaction));
        
        A.CallTo(() => _mockInventoryRepository.UpdateIfExists(A<string>.Ignored, A<Func<InventoryEntity, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<InventoryEntity, bool> func, ITransaction _, IRedLock _) => func(inventoryData))
            .Returns(new UpdateEntityResult<InventoryEntity>(inventoryData, UpdateEntityStatus.WaitingForTransaction));
        
        A.CallTo(() => _mockSharedFilesService.ClearSession(sessionId)).DoesNothing();

        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };

        var request = new StartInventoryRequest(sessionId, client);

        // Act
        var result = await _startInventoryCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StartInventoryStatuses.InventoryStartedSucessfully);
        A.CallTo(() => _mockSharedFilesService.ClearSession(sessionId)).MustHaveHappenedOnceExactly();
        A.CallTo(() =>  _mockSharedFilesService.ClearSession(sessionId)).MustHaveHappenedOnceExactly();
    }
    
    [Test]
    public async Task StartInventory_SessionAlreadyActivated_ReturnsOk()
    {
        // Arrange
        var sessionId = "testSession";
        var cloudSessionData = new CloudSessionData(
            null, 
            new EncryptedSessionSettings(),
            new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" }
        );
        A.CallTo(() => _mockCloudSessionsRepository.UpdateIfExists(
                A<string>.Ignored, 
                A<Func<CloudSessionData, bool>>.Ignored, 
                A<ITransaction>.Ignored, 
                A<IRedLock>.Ignored
            ))
            .Returns(new UpdateEntityResult<CloudSessionData>(cloudSessionData, UpdateEntityStatus.NoOperation));

        A.CallTo(() => _mockSharedFilesService.ClearSession(sessionId)).DoesNothing();
        
        var request = new StartInventoryRequest(sessionId, new Client 
        { 
            ClientId = "client1", 
            ClientInstanceId = "clientInstanceId1" 
        });

        // Act
        var result = await _startInventoryCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StartInventoryStatuses.InventoryStartedSucessfully);
        A.CallTo(() =>  _mockSharedFilesService.ClearSession(sessionId)).MustNotHaveHappened();
    }
    
    [Test]
    public async Task StartInventory_OneMember_NoInventoryDataSet_ReturnsLessThan2Members()
    {
        // Arrange
        var sessionId = "testSession";
        var inventoryData = new InventoryEntity(sessionId);
        var cloudSessionData = new CloudSessionData(null, new EncryptedSessionSettings(),
            new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" });
        cloudSessionData.SessionMembers.Add(new SessionMemberData("client1", "client1", new PublicKeyInfo(), null, cloudSessionData));
        inventoryData.InventoryMembers.Add(new InventoryMemberEntity
            { ClientInstanceId = "client1", DataSources = [ new() { Id = "dataSource1" } ] });

        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId))
            .Returns(cloudSessionData);

        A.CallTo(() => _mockCloudSessionsRepository.UpdateIfExists(A<string>.Ignored, A<Func<CloudSessionData, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<CloudSessionData, bool> func, ITransaction _, IRedLock _) => func(cloudSessionData))
            .Returns(new UpdateEntityResult<CloudSessionData>(cloudSessionData, UpdateEntityStatus.WaitingForTransaction));
        
        A.CallTo(() => _mockInventoryRepository.UpdateIfExists(A<string>.Ignored, A<Func<InventoryEntity, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Returns(new UpdateEntityResult<InventoryEntity>(inventoryData, UpdateEntityStatus.NotFound));
        
        A.CallTo(() => _mockSharedFilesService.ClearSession(sessionId)).DoesNothing();

        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };

        var request = new StartInventoryRequest(sessionId, client);

        // Act
        var result = await _startInventoryCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StartInventoryStatuses.LessThan2Members);
        A.CallTo(() =>  _mockSharedFilesService.ClearSession(sessionId)).MustNotHaveHappened();
    }
    
    [Test]
    public async Task StartInventory_TwoMembers_NoInventoryDataSet_ReturnsAtLeastOneMemberWithNoDataToSynchronize()
    {
        // Arrange
        var sessionId = "testSession";
        var inventoryData = new InventoryEntity(sessionId);
        var cloudSessionData = new CloudSessionData(null, new EncryptedSessionSettings(),
            new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" });
        cloudSessionData.SessionMembers.Add(new SessionMemberData("client1", "client1", new PublicKeyInfo(), null, cloudSessionData));
        cloudSessionData.SessionMembers.Add(new SessionMemberData("client2", "client2", new PublicKeyInfo(), null, cloudSessionData));
        inventoryData.InventoryMembers.Add(new InventoryMemberEntity
            { ClientInstanceId = "client1", DataSources = [ new() { Id = "dataSource1" } ] });
        inventoryData.InventoryMembers.Add(new InventoryMemberEntity
            { ClientInstanceId = "client2", DataSources = [ new() { Id = "dataSource2" } ] });

        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId))
            .Returns(cloudSessionData);

        A.CallTo(() => _mockCloudSessionsRepository.UpdateIfExists(A<string>.Ignored, A<Func<CloudSessionData, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<CloudSessionData, bool> func, ITransaction _, IRedLock _) => func(cloudSessionData))
            .Returns(new UpdateEntityResult<CloudSessionData>(cloudSessionData, UpdateEntityStatus.WaitingForTransaction));
        
        A.CallTo(() => _mockInventoryRepository.UpdateIfExists(A<string>.Ignored, A<Func<InventoryEntity, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Returns(new UpdateEntityResult<InventoryEntity>(inventoryData, UpdateEntityStatus.NotFound));
        
        A.CallTo(() => _mockSharedFilesService.ClearSession(sessionId)).DoesNothing();

        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };

        var request = new StartInventoryRequest(sessionId, client);

        // Act
        var result = await _startInventoryCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StartInventoryStatuses.AtLeastOneMemberWithNoDataToSynchronize);
        A.CallTo(() =>  _mockSharedFilesService.ClearSession(sessionId)).MustNotHaveHappened();
    }
}