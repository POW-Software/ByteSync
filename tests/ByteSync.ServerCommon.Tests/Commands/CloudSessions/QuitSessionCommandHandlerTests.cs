using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Interfaces.Hub;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.CloudSessions;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Hubs;
using ByteSync.ServerCommon.Interfaces.Mappers;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using FakeItEasy;
using FluentAssertions;
using RedLockNet;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Tests.Commands.CloudSessions;

[TestFixture]
public class QuitSessionCommandHandlerTests
{
    private ICloudSessionsRepository _mockCloudSessionsRepository;
    private IInventoryRepository _mockInventoryRepository;
    private ISynchronizationRepository _mockSynchronizationRepository;
    private ICacheService _mockCacheService;
    private ISessionMemberMapper _mockSessionMemberMapper;
    private IByteSyncClientCaller _mockByteSyncClientCaller;
    private ITransaction _mockTransaction;
    private QuitSessionCommandHandler _quitSessionCommandHandler;

    [SetUp]
    public void Setup()
    {
        _mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        _mockInventoryRepository = A.Fake<IInventoryRepository>();
        _mockSynchronizationRepository = A.Fake<ISynchronizationRepository>();
        _mockCacheService = A.Fake<ICacheService>();
        _mockSessionMemberMapper = A.Fake<ISessionMemberMapper>();
        _mockByteSyncClientCaller = A.Fake<IByteSyncClientCaller>();

        _mockTransaction = A.Fake<ITransaction>();
        A.CallTo(() => _mockCacheService.OpenTransaction()).Returns(_mockTransaction);

        _quitSessionCommandHandler = new QuitSessionCommandHandler(_mockCloudSessionsRepository, _mockInventoryRepository,
            _mockSynchronizationRepository, _mockCacheService, _mockSessionMemberMapper, _mockByteSyncClientCaller);
    }

    [Test]
    public async Task QuitSession_SessionMemberExists_PerformsUpdateAndNotifies()
    {
        // Arrange
        var sessionId = "testSession";
        var client = new Client { ClientInstanceId = "clientInstance1" };
        // Create a session member that matches the client.
        var sessionMember = new SessionMemberData { ClientInstanceId = "clientInstance1" };
        var cloudSessionData = new CloudSessionData();
        cloudSessionData.SessionMembers.Add(sessionMember);
        // Mark session activated if you want to trigger synchronization update.
        cloudSessionData.IsSessionActivated = true;
        var inventoryData = new InventoryData(sessionId);
        var synchronizationEntity = new SynchronizationEntity
        {
            SessionId = sessionId,
            EndedOn = DateTimeOffset.Now
        };

        // // Simulate repository update that removes the member and returns waiting-for-transaction.
        // A.CallTo(() => _mockCloudSessionsRepository.Update(sessionId, A<Func<CloudSessionData, bool>>.Ignored, _mockTransaction, null))
        //     .Invokes((string id, Func<CloudSessionData, bool> updateFunc, ITransaction tx) =>
        //     {
        //         // Invoke function on the provided cloud session data.
        //         var memberRemoved = updateFunc(cloudSessionData);
        //         return new UpdateEntityResult<CloudSessionData>(cloudSessionData, 
        //             memberRemoved ? UpdateEntityStatus.Saved : UpdateEntityStatus.NotFound);
        //     })
        //     .ReturnsLazily(() => new UpdateEntityResult<CloudSessionData>(cloudSessionData, UpdateEntityStatus.Saved));
        
        A.CallTo(() => _mockCloudSessionsRepository.Update(A<string>.Ignored, A<Func<CloudSessionData, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<CloudSessionData, bool> func, ITransaction tx, IRedLock redisLock) => func(cloudSessionData))
            .Returns(new UpdateEntityResult<CloudSessionData>(cloudSessionData, UpdateEntityStatus.WaitingForTransaction));

        // // Simulate inventory update.
        // A.CallTo(() => _mockInventoryRepository.UpdateIfExists(sessionId, A<Func<InventoryData, bool>>.Ignored, _mockTransaction, null))
        //     .Returns(Task.CompletedTask);
        
        A.CallTo(() => _mockInventoryRepository.UpdateIfExists(A<string>.Ignored, A<Func<InventoryData, bool>>.Ignored, _mockTransaction, null))
            .Invokes((string _, Func<InventoryData, bool> func, ITransaction tx, IRedLock redisLock) => func(inventoryData))
            .Returns(new UpdateEntityResult<InventoryData>(inventoryData, UpdateEntityStatus.Saved));

        // Simulate synchronization update.
        A.CallTo(() => _mockSynchronizationRepository.UpdateIfExists(sessionId, A<Func<SynchronizationEntity, bool>>.Ignored, _mockTransaction, null))
            .Invokes((string _, Func<SynchronizationEntity, bool> func, ITransaction tx, IRedLock redisLock) => func(synchronizationEntity))
            .Returns(new UpdateEntityResult<SynchronizationEntity>(synchronizationEntity, UpdateEntityStatus.Saved));

        // Simulate transaction execution.
        A.CallTo(() => _mockTransaction.ExecuteAsync(CommandFlags.None)).Returns(true);

        // Set up the client caller group.
        var mockGroup = A.Fake<IHubByteSyncPush>();
        A.CallTo(() => _mockByteSyncClientCaller.SessionGroup(sessionId)).Returns(mockGroup);

        // Set up session member mapper.
        var sessionMemberInfo = new SessionMemberInfoDTO();
        A.CallTo(() => _mockSessionMemberMapper.Convert(sessionMember))
            .Returns(Task.FromResult(sessionMemberInfo));

        var request = new QuitSessionRequest(sessionId, client);

        // Act
        await _quitSessionCommandHandler.Handle(request, CancellationToken.None);

        // Assert repository update is called.
        cloudSessionData.SessionMembers.Should().BeEmpty();
        
        A.CallTo(() => _mockCloudSessionsRepository.Update(sessionId, A<Func<CloudSessionData, bool>>.Ignored, _mockTransaction, null))
            .MustHaveHappenedOnceExactly();
        // Assert inventory and synchronization updates occurred.
        A.CallTo(() => _mockInventoryRepository.UpdateIfExists(sessionId, A<Func<InventoryData, bool>>.Ignored, _mockTransaction, null))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockSynchronizationRepository.UpdateIfExists(sessionId, A<Func<SynchronizationEntity, bool>>.Ignored, _mockTransaction, null))
            .MustHaveHappenedOnceExactly();
        // Assert transaction was executed.
        A.CallTo(() => _mockTransaction.ExecuteAsync(CommandFlags.None)).MustHaveHappenedOnceExactly();
        // Assert client was removed from group.
        A.CallTo(() => _mockByteSyncClientCaller.RemoveFromGroup(client, sessionId))
            .MustHaveHappenedOnceExactly();
        // Assert session member mapper and notify call.
        A.CallTo(() => _mockSessionMemberMapper.Convert(sessionMember))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => mockGroup.MemberQuittedSession(sessionMemberInfo))
            .MustHaveHappenedOnceExactly();
    }

    /*
    [Test]
    public async Task QuitSession_SessionMemberDoesNotExist_NoFurtherActions()
    {
        // Arrange
        var sessionId = "testSession";
        var client = new Client { ClientInstanceId = "clientInstance1" };
        // Cloud session data with no matching member.
        var cloudSessionData = new CloudSessionData();

        // Simulate repository update that does nothing and returns waiting-for-transaction as false.
        A.CallTo(() => _mockCloudSessionsRepository.Update(sessionId, A<Func<CloudSessionData, CloudSessionData>>.Ignored, _mockTransaction))
            .Invokes((string id, Func<CloudSessionData, CloudSessionData> updateFunc, ITransaction tx) =>
            {
                updateFunc(cloudSessionData);
                return new UpdateEntityResult<CloudSessionData>(cloudSessionData, UpdateEntityStatus.NotFound, false);
            })
            .ReturnsLazily(() => new UpdateEntityResult<CloudSessionData>(cloudSessionData, UpdateEntityStatus.NotFound, false));

        var request = new QuitSessionRequest(sessionId, client, client.ClientInstanceId);

        // Act
        await _quitSessionCommandHandler.Handle(request, CancellationToken.None);

        // Assert that no inventory, synchronization, or client caller methods are invoked.
        A.CallTo(() => _mockInventoryRepository.UpdateIfExists(sessionId, A<Func<InventoryData, InventoryData>>.Ignored, _mockTransaction))
            .MustNotHaveHappened();
        A.CallTo(() => _mockSynchronizationRepository.UpdateIfExists(sessionId, A<Func<SynchronizationData, bool>>.Ignored, _mockTransaction))
            .MustNotHaveHappened();
        A.CallTo(() => _mockTransaction.ExecuteAsync()).MustNotHaveHappened();
        A.CallTo(() => _mockByteSyncClientCaller.RemoveFromGroup(A<Client>.Ignored, A<string>.Ignored))
            .MustNotHaveHappened();
    }
    */
}