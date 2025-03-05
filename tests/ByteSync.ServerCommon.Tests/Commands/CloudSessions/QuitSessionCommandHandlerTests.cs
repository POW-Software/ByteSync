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
using ByteSync.ServerCommon.Tests.Helpers;
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
    private IClientsGroupsManager _mockClientsGroupsManager;
    private IClientsGroupsInvoker _mockClientsGroupsInvoker;
    
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
        _mockClientsGroupsManager = A.Fake<IClientsGroupsManager>();
        _mockClientsGroupsInvoker = A.Fake<IClientsGroupsInvoker>();

        _mockTransaction = A.Fake<ITransaction>();
        A.CallTo(() => _mockCacheService.OpenTransaction()).Returns(_mockTransaction);

        _quitSessionCommandHandler = new QuitSessionCommandHandler(_mockCloudSessionsRepository, _mockInventoryRepository,
            _mockSynchronizationRepository, _mockCacheService, _mockSessionMemberMapper, _mockClientsGroupsManager, _mockClientsGroupsInvoker);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task QuitSession_SessionMemberExists_PerformsUpdateAndNotifies(bool isQuitterAnInventoryMember)
    {
        // Arrange
        var sessionId = "testSession";
        var client = new Client { ClientInstanceId = "clientInstance1" };
        
        var sessionMember = new SessionMemberData { ClientInstanceId = "clientInstance1" };
        var cloudSessionData = new CloudSessionData();
        cloudSessionData.SessionMembers.Add(sessionMember);
        cloudSessionData.IsSessionActivated = true;
        

        var inventoryData = new InventoryData(sessionId);
        if (isQuitterAnInventoryMember)
        {
            var inventoryMember = new InventoryMemberData { ClientInstanceId = "clientInstance1" };
            inventoryData.InventoryMembers.Add(inventoryMember);
        }
        
        var synchronizationEntity = new SynchronizationEntity
        {
            SessionId = sessionId,
            EndedOn = DateTimeOffset.Now
        };

        bool funcResult = false;
        bool isTransaction = false;
        A.CallTo(() => _mockCloudSessionsRepository.Update(A<string>.Ignored, A<Func<CloudSessionData, bool>>.Ignored, 
                A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<CloudSessionData, bool> func, ITransaction? transaction, IRedLock? _) =>
            {
                funcResult = func(cloudSessionData);
                isTransaction = transaction != null;
            })
            .ReturnsLazily(() => UpdateResultBuilder.BuildUpdateResult(funcResult, cloudSessionData, isTransaction));
        
        A.CallTo(() => _mockInventoryRepository.UpdateIfExists(A<string>.Ignored, A<Func<InventoryData, bool>>.Ignored, _mockTransaction, null))
            .Invokes((string _, Func<InventoryData, bool> func, ITransaction? transaction, IRedLock? _) =>
            {
                funcResult = func(inventoryData);
                isTransaction = transaction != null;
            })
            .ReturnsLazily(() => UpdateResultBuilder.BuildUpdateResult(funcResult, inventoryData, isTransaction));
        
        A.CallTo(() => _mockSynchronizationRepository.UpdateIfExists(sessionId, A<Func<SynchronizationEntity, bool>>.Ignored, _mockTransaction, null))
            .Invokes((string _, Func<SynchronizationEntity, bool> func, ITransaction? transaction, IRedLock? _) =>
            {
                funcResult = func(synchronizationEntity);
                isTransaction = transaction != null;
            })
            .ReturnsLazily(() => UpdateResultBuilder.BuildUpdateResult(funcResult, synchronizationEntity, isTransaction));
        
        A.CallTo(() => _mockTransaction.ExecuteAsync(CommandFlags.None)).Returns(true);
        
        var mockGroup = A.Fake<IHubByteSyncPush>();
        A.CallTo(() => _mockClientsGroupsInvoker.SessionGroup(sessionId)).Returns(mockGroup);
        
        var sessionMemberInfo = new SessionMemberInfoDTO();
        A.CallTo(() => _mockSessionMemberMapper.Convert(sessionMember))
            .Returns(Task.FromResult(sessionMemberInfo));
        
        // Act
        var request = new QuitSessionRequest(sessionId, client);
        await _quitSessionCommandHandler.Handle(request, CancellationToken.None);
        
        
        // Assert
        cloudSessionData.SessionMembers.Should().BeEmpty();
        cloudSessionData.IsSessionRemoved.Should().BeTrue();
        cloudSessionData.IsSessionOnError.Should().BeFalse();
        inventoryData.InventoryMembers.Should().BeEmpty();
        synchronizationEntity.IsFatalError.Should().BeFalse();
        
        A.CallTo(() => _mockCloudSessionsRepository.Update(sessionId, A<Func<CloudSessionData, bool>>.Ignored, _mockTransaction, null))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _mockInventoryRepository.UpdateIfExists(sessionId, A<Func<InventoryData, bool>>.Ignored, _mockTransaction, null))
            .MustHaveHappenedOnceExactly();
        
        A.CallTo(() => _mockSynchronizationRepository.UpdateIfExists(sessionId, A<Func<SynchronizationEntity, bool>>.Ignored, _mockTransaction, null))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _mockTransaction.ExecuteAsync(CommandFlags.None)).MustHaveHappenedOnceExactly();

        A.CallTo(() => _mockClientsGroupsManager.RemoveFromGroup(client, sessionId))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _mockSessionMemberMapper.Convert(sessionMember))
            .MustHaveHappenedOnceExactly();
        
        A.CallTo(() => mockGroup.MemberQuittedSession(sessionMemberInfo))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task QuitSession_SessionMemberNotExists_()
    {
        // Arrange
        var sessionId = "testSession";
        var client = new Client { ClientInstanceId = "clientInstance1" };
        
        var cloudSessionData = new CloudSessionData();
        cloudSessionData.IsSessionActivated = false;
        
        var inventoryData = new InventoryData(sessionId);
        var inventoryMember = new InventoryMemberData { ClientInstanceId = "clientInstance1" };
        inventoryData.InventoryMembers.Add(inventoryMember);
        
        var synchronizationEntity = new SynchronizationEntity
        {
            SessionId = sessionId,
            EndedOn = DateTimeOffset.Now
        };

        bool funcResult = false;
        bool isTransaction = false;
        A.CallTo(() => _mockCloudSessionsRepository.Update(A<string>.Ignored, A<Func<CloudSessionData, bool>>.Ignored, 
                A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<CloudSessionData, bool> func, ITransaction? transaction, IRedLock? _) =>
            {
                funcResult = func(cloudSessionData);
                isTransaction = transaction != null;
            })
            .ReturnsLazily(() => UpdateResultBuilder.BuildUpdateResult(funcResult, cloudSessionData, isTransaction));
        
        A.CallTo(() => _mockInventoryRepository.UpdateIfExists(A<string>.Ignored, A<Func<InventoryData, bool>>.Ignored, _mockTransaction, null))
            .Invokes((string _, Func<InventoryData, bool> func, ITransaction? transaction, IRedLock? _) =>
            {
                funcResult = func(inventoryData);
                isTransaction = transaction != null;
            })
            .ReturnsLazily(() => UpdateResultBuilder.BuildUpdateResult(funcResult, inventoryData, isTransaction));
        
        A.CallTo(() => _mockSynchronizationRepository.UpdateIfExists(sessionId, A<Func<SynchronizationEntity, bool>>.Ignored, _mockTransaction, null))
            .Invokes((string _, Func<SynchronizationEntity, bool> func, ITransaction? transaction, IRedLock? _) =>
            {
                funcResult = func(synchronizationEntity);
                isTransaction = transaction != null;
            })
            .ReturnsLazily(() => UpdateResultBuilder.BuildUpdateResult(funcResult, synchronizationEntity, isTransaction));
        
        A.CallTo(() => _mockTransaction.ExecuteAsync(CommandFlags.None)).Returns(true);
        
        var mockGroup = A.Fake<IHubByteSyncPush>();
        A.CallTo(() => _mockClientsGroupsInvoker.SessionGroup(sessionId)).Returns(mockGroup);
        
        // Act
        var request = new QuitSessionRequest(sessionId, client);
        await _quitSessionCommandHandler.Handle(request, CancellationToken.None);
        
        // Assert
        cloudSessionData.SessionMembers.Should().BeEmpty();
        cloudSessionData.IsSessionRemoved.Should().BeFalse();
        cloudSessionData.IsSessionOnError.Should().BeFalse();
        inventoryData.InventoryMembers.Should().NotBeEmpty();
        synchronizationEntity.IsFatalError.Should().BeFalse();
        
        A.CallTo(() => _mockCloudSessionsRepository.Update(sessionId, A<Func<CloudSessionData, bool>>.Ignored, _mockTransaction, null))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _mockInventoryRepository.UpdateIfExists(sessionId, A<Func<InventoryData, bool>>.Ignored, _mockTransaction, null))
            .MustNotHaveHappened();
        
        A.CallTo(() => _mockSynchronizationRepository.UpdateIfExists(sessionId, A<Func<SynchronizationEntity, bool>>.Ignored, _mockTransaction, null))
            .MustNotHaveHappened();

        A.CallTo(() => _mockTransaction.ExecuteAsync(CommandFlags.None)).MustNotHaveHappened();

        A.CallTo(() => _mockClientsGroupsManager.RemoveFromGroup(client, sessionId))
            .MustNotHaveHappened();

        A.CallTo(() => _mockSessionMemberMapper.Convert(A<SessionMemberData>.Ignored))
            .MustNotHaveHappened();
    }
}