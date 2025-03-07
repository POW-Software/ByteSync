using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Interfaces.Hub;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.CloudSessions;
using ByteSync.ServerCommon.Interfaces.Mappers;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using ByteSync.ServerCommon.Tests.Helpers;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using RedLockNet;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Tests.Commands.CloudSessions;

public class FinalizeJoinCloudSessionCommandHandlerTests
{
    private ICloudSessionsRepository _mockCloudSessionsRepository;
    private ISessionMemberMapper _mockSessionMemberMapper;
    private IInvokeClientsService _mockInvokeClientsService;
    private IClientsGroupsService _mockClientsGroupsService;
    private ICacheService _mockCacheService;
    private ILogger<FinalizeJoinCloudSessionCommandHandler> _mockLogger;
    private ITransaction _mockTransaction;
    private FinalizeJoinCloudSessionCommandHandler _handler;
    private IHubByteSyncPush _byteSyncPush;

    [SetUp]
    public void Setup()
    {
        _mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        _mockSessionMemberMapper = A.Fake<ISessionMemberMapper>();
        _mockInvokeClientsService = A.Fake<IInvokeClientsService>();
        _mockClientsGroupsService = A.Fake<IClientsGroupsService>();
        _mockCacheService = A.Fake<ICacheService>();
        _mockLogger = A.Fake<ILogger<FinalizeJoinCloudSessionCommandHandler>>();
        _mockTransaction = A.Fake<ITransaction>();
        _byteSyncPush = A.Fake<IHubByteSyncPush>();

        A.CallTo(() => _mockCacheService.OpenTransaction()).Returns(_mockTransaction);
        A.CallTo(() => _mockInvokeClientsService.SessionGroup(A<string>.Ignored))
            .Returns(_byteSyncPush);

        _handler = new FinalizeJoinCloudSessionCommandHandler(
            _mockCloudSessionsRepository,
            _mockSessionMemberMapper,
            _mockInvokeClientsService,
            _mockClientsGroupsService,
            _mockCacheService,
            _mockLogger);
    }

    [Test]
    public async Task Handle_SuccessfulFinalization_ReturnsSuccess()
    {
        // Arrange
        var sessionId = "123ABC456";
        var joinerInstanceId = "joinerInstance1";
        var validatorInstanceId = "validatorInstance1";
        var finalizationPassword = "password123";
        var client = new Client { ClientInstanceId = joinerInstanceId };
        var privateData = new EncryptedSessionMemberPrivateData();
        var sessionMemberInfo = new SessionMemberInfoDTO();

        var request = new FinalizeJoinCloudSessionRequest(
            new FinalizeJoinCloudSessionParameters
            {
                SessionId = sessionId,
                JoinerInstanceId = joinerInstanceId,
                ValidatorInstanceId = validatorInstanceId,
                FinalizationPassword = finalizationPassword,
                EncryptedSessionMemberPrivateData = privateData
            },
            client);

        var cloudSession = new CloudSessionData { SessionId = sessionId };
        var joiner = new SessionMemberData
        {
            ClientInstanceId = joinerInstanceId,
            ValidatorInstanceId = validatorInstanceId,
            FinalizationPassword = finalizationPassword,
            CloudSessionData = cloudSession
        };

        cloudSession.PreSessionMembers.Add(joiner);

        // A.CallTo(() => _mockUpdateResult.IsWaitingForTransaction).Returns(true);

        bool funcResult = false;
        bool isTransaction = false;
        A.CallTo(() => _mockCloudSessionsRepository.Update(
                A<string>.That.IsEqualTo(sessionId),
                A<Func<CloudSessionData, bool>>.Ignored,
                A<ITransaction>.That.IsEqualTo(_mockTransaction), A<IRedLock>.Ignored))
            .Invokes((string id, Func<CloudSessionData, bool> updateAction, ITransaction? transaction, IRedLock _) =>
            {
                funcResult = updateAction(cloudSession);
                isTransaction = transaction != null;
            })
            .ReturnsLazily(() => UpdateResultBuilder.BuildUpdateResult(funcResult, cloudSession, isTransaction));

        A.CallTo(() => _mockSessionMemberMapper.Convert(A<SessionMemberData>.That.IsNotNull()))
            .Returns(Task.FromResult(sessionMemberInfo));

        A.CallTo(() => _mockTransaction.ExecuteAsync(A<CommandFlags>.Ignored)).Returns(Task.FromResult(true));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Status.Should().Be(FinalizeJoinSessionStatuses.FinalizeJoinSessionSucess);

        // Verify repository update
        A.CallTo(() => _mockCloudSessionsRepository.Update(
            A<string>.That.IsEqualTo(sessionId),
            A<Func<CloudSessionData, bool>>.Ignored,
            A<ITransaction>.That.IsEqualTo(_mockTransaction), A<IRedLock>.Ignored)).MustHaveHappenedOnceExactly();

        // Verify session member moved from PreSessionMembers to SessionMembers
        cloudSession.PreSessionMembers.Should().NotContain(joiner);
        cloudSession.SessionMembers.Should().Contain(joiner);
        joiner.EncryptedPrivateData.Should().Be(privateData);

        // Verify client added to session group
        A.CallTo(() => _mockClientsGroupsService.AddSessionSubscription(
            A<Client>.That.IsEqualTo(client),
            A<string>.That.IsEqualTo(sessionId),
            A<ITransaction>.That.IsEqualTo(_mockTransaction))).MustHaveHappenedOnceExactly();

        A.CallTo(() => _mockClientsGroupsService.AddToSessionGroup(
            A<Client>.That.IsEqualTo(client),
            A<string>.That.IsEqualTo(sessionId))).MustHaveHappenedOnceExactly();

        // Verify transaction execute
        A.CallTo(() => _mockTransaction.ExecuteAsync(A<CommandFlags>.Ignored)).MustHaveHappenedOnceExactly();

        // Verify client notification
        A.CallTo(() => _mockInvokeClientsService.SessionGroup(A<string>.That.IsEqualTo(sessionId)))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _byteSyncPush.MemberJoinedSession(A<SessionMemberInfoDTO>.That.IsEqualTo(sessionMemberInfo)))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_SessionNotFound_ReturnsSessionNotFoundStatus()
    {
        // Arrange
        var sessionId = "123ABC456";
        var request = CreateBasicRequest(sessionId);

        bool funcResult = false;
        bool isTransaction = false;
        var cloudSessionData = new CloudSessionData { SessionId = sessionId, IsSessionRemoved = true };
        A.CallTo(() => _mockCloudSessionsRepository.Update(
                A<string>.That.IsEqualTo(sessionId),
                A<Func<CloudSessionData, bool>>.Ignored,
                A<ITransaction>.That.IsEqualTo(_mockTransaction), A<IRedLock>.Ignored))
            .Invokes((string id, Func<CloudSessionData, bool> updateAction, ITransaction? transaction, IRedLock _) =>
            {
                funcResult = updateAction(cloudSessionData);
                isTransaction = transaction != null;
            })
            .ReturnsLazily(() => UpdateResultBuilder.BuildUpdateResult(funcResult, cloudSessionData, isTransaction));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Status.Should().Be(FinalizeJoinSessionStatuses.SessionNotFound);
        VerifyNoSessionModifications();
    }

    [Test]
    public async Task Handle_SessionAlreadyActivated_ReturnsSessionAlreadyActivatedStatus()
    {
        // Arrange
        var sessionId = "123ABC456";
        var request = CreateBasicRequest(sessionId);

        bool funcResult = false;
        bool isTransaction = false;
        var cloudSessionData = new CloudSessionData{ SessionId = sessionId, IsSessionActivated = true };
        A.CallTo(() => _mockCloudSessionsRepository.Update(
                A<string>.That.IsEqualTo(sessionId),
                A<Func<CloudSessionData, bool>>.Ignored,
                A<ITransaction>.That.IsEqualTo(_mockTransaction), A<IRedLock>.Ignored))
            .Invokes((string id, Func<CloudSessionData, bool> updateAction, ITransaction? transaction, IRedLock _) =>
            {
                funcResult = updateAction(cloudSessionData);
                isTransaction = transaction != null;
            })
            .ReturnsLazily(() => UpdateResultBuilder.BuildUpdateResult(funcResult, cloudSessionData, isTransaction));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Status.Should().Be(FinalizeJoinSessionStatuses.SessionAlreadyActivated);
        VerifyNoSessionModifications();
    }

    [Test]
    public async Task Handle_AuthNotChecked_ReturnsAuthNotCheckedStatus()
    {
        // Arrange
        var sessionId = "123ABC456";
        var joinerInstanceId = "joinerInstance1";
        var request = CreateBasicRequest(sessionId, joinerInstanceId);

        bool funcResult = false;
        bool isTransaction = false;
        var cloudSessionData = new CloudSessionData { SessionId = sessionId };
        cloudSessionData.SessionMembers.Add(new SessionMemberData { ClientInstanceId = "otherMember" });
        A.CallTo(() => _mockCloudSessionsRepository.Update(
                A<string>.That.IsEqualTo(sessionId),
                A<Func<CloudSessionData, bool>>.Ignored,
                A<ITransaction>.That.IsEqualTo(_mockTransaction), A<IRedLock>.Ignored))
            .Invokes((string id, Func<CloudSessionData, bool> updateAction, ITransaction? transaction, IRedLock _) =>
            {
                funcResult = updateAction(cloudSessionData);
                isTransaction = transaction != null;
            })
            .ReturnsLazily(() => UpdateResultBuilder.BuildUpdateResult(funcResult, cloudSessionData, isTransaction));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Status.Should().Be(FinalizeJoinSessionStatuses.AuthIsNotChecked);
        VerifyNoSessionModifications();
    }

    [Test]
    public async Task Handle_PrememberNotFound_ReturnsPrememberNotFoundStatus()
    {
        // Arrange
        var sessionId = "123ABC456";
        var joinerInstanceId = "joinerInstance1";
        var validatorInstanceId = "validatorInstance1";
        var finalizationPassword = "password123";
        var request = CreateBasicRequest(sessionId, joinerInstanceId, validatorInstanceId, finalizationPassword);
        
        bool funcResult = false;
        bool isTransaction = false;
        var cloudSessionData = new CloudSessionData { SessionId = sessionId };
        A.CallTo(() => _mockCloudSessionsRepository.Update(
                A<string>.That.IsEqualTo(sessionId),
                A<Func<CloudSessionData, bool>>.Ignored,
                A<ITransaction>.That.IsEqualTo(_mockTransaction), A<IRedLock>.Ignored))
            .Invokes((string id, Func<CloudSessionData, bool> updateAction, ITransaction? transaction, IRedLock _) =>
            {
                funcResult = updateAction(cloudSessionData);
                isTransaction = transaction != null;
            })
            .ReturnsLazily(() => UpdateResultBuilder.BuildUpdateResult(funcResult, cloudSessionData, isTransaction));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Status.Should().Be(FinalizeJoinSessionStatuses.PrememberNotFound);
        VerifyNoSessionModifications();
    }

    private FinalizeJoinCloudSessionRequest CreateBasicRequest(
        string sessionId,
        string joinerInstanceId = "joinerInstance1",
        string validatorInstanceId = "validatorInstance1",
        string finalizationPassword = "password123")
    {
        var client = new Client { ClientInstanceId = joinerInstanceId };
        var privateData = new EncryptedSessionMemberPrivateData();

        return new FinalizeJoinCloudSessionRequest(
            new FinalizeJoinCloudSessionParameters
            {
                SessionId = sessionId,
                JoinerInstanceId = joinerInstanceId,
                ValidatorInstanceId = validatorInstanceId,
                FinalizationPassword = finalizationPassword,
                EncryptedSessionMemberPrivateData = privateData
            },
            client);
    }

    private void VerifyNoSessionModifications()
    {
        A.CallTo(() => _mockClientsGroupsService.AddSessionSubscription(
            A<Client>.Ignored,
            A<string>.Ignored,
            A<ITransaction>.Ignored)).MustNotHaveHappened();

        A.CallTo(() => _mockClientsGroupsService.AddToSessionGroup(
            A<Client>.Ignored,
            A<string>.Ignored)).MustNotHaveHappened();

        A.CallTo(() => _byteSyncPush.MemberJoinedSession(A<SessionMemberInfoDTO>.Ignored))
            .MustNotHaveHappened();
    }
}