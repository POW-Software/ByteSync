using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.CloudSessions;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Tests.Commands.CloudSessions;

public class CreateSessionCommandHandlerTests
{
    private ICloudSessionsRepository _mockCloudSessionsRepository;
    private IClientsGroupsService _mockClientsGroupsService;
    private IClientsRepository _mockClientsRepository;
    private ICloudSessionsService _mockCloudSessionsService;
    private ICacheService _mockCacheService;
    private ILogger<CreateSessionCommandHandler> _mockLogger;
    private ITransaction _mockTransaction;
    private CreateSessionCommandHandler _createSessionCommandHandler;

    [SetUp]
    public void Setup()
    {
        _mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        _mockClientsGroupsService = A.Fake<IClientsGroupsService>();
        _mockClientsRepository = A.Fake<IClientsRepository>();
        _mockCloudSessionsService = A.Fake<ICloudSessionsService>();
        _mockCacheService = A.Fake<ICacheService>();
        _mockLogger = A.Fake<ILogger<CreateSessionCommandHandler>>();
        _mockTransaction = A.Fake<ITransaction>();

        A.CallTo(() => _mockCacheService.OpenTransaction()).Returns(_mockTransaction);

        _createSessionCommandHandler = new CreateSessionCommandHandler(
            _mockCloudSessionsRepository,
            _mockClientsGroupsService,
            _mockClientsRepository,
            _mockCloudSessionsService,
            _mockCacheService,
            _mockLogger);
    }

    [Test]
    public async Task Handle_ValidRequest_CreatesSession()
    {
        // Arrange
        var lobbyId = "lobbyId";
        var sessionSettings = new EncryptedSessionSettings();
        var client = new Client { ClientInstanceId = "clientInstance1" };
        var creatorPublicKeyInfo = new PublicKeyInfo();
        var creatorProfileClientId = "creatorProfile";
        var creatorPrivateData = new EncryptedSessionMemberPrivateData();
        var sessionId = "123ABC456";

        var request = new CreateSessionRequest(
            new CreateCloudSessionParameters
            {
                LobbyId = lobbyId,
                SessionSettings = sessionSettings,
                CreatorPublicKeyInfo = creatorPublicKeyInfo,
                CreatorProfileClientId = creatorProfileClientId,
                CreatorPrivateData = creatorPrivateData
            },
            client);

        CloudSessionData addedCloudSession = null!;
        SessionMemberData creatorMemberData = null!;

        A.CallTo(() => _mockCloudSessionsRepository.AddCloudSession(
                A<CloudSessionData>.Ignored,
                A<Func<string>>.Ignored,
                A<ITransaction>.Ignored))
            .Invokes((CloudSessionData session, Func<string> _, ITransaction _) =>
            {
                addedCloudSession = session;
                session.SessionId = sessionId;
                creatorMemberData = session.SessionMembers[0];
            })
            .Returns(Task.FromResult(new CloudSessionData { SessionId = sessionId }));

        var expectedResult = new CloudSessionResult();
        A.CallTo(() => _mockCloudSessionsService.BuildCloudSessionResult(
                A<CloudSessionData>.Ignored,
                A<SessionMemberData>.Ignored))
            .Returns(Task.FromResult(expectedResult));

        A.CallTo(() => _mockTransaction.ExecuteAsync(A<CommandFlags>.Ignored)).Returns(true);

        // Act
        var result = await _createSessionCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expectedResult);

        // Verify cloud session creation
        A.CallTo(() => _mockCloudSessionsRepository.AddCloudSession(
            A<CloudSessionData>.Ignored,
            A<Func<string>>.Ignored,
            A<ITransaction>.Ignored)).MustHaveHappenedOnceExactly();

        addedCloudSession.Should().NotBeNull();
        addedCloudSession.LobbyId.Should().Be(lobbyId);
        addedCloudSession.SessionSettings.Should().BeSameAs(sessionSettings);
        addedCloudSession.SessionMembers.Should().HaveCount(1);

        // Verify session member creation
        creatorMemberData.Should().NotBeNull();
        creatorMemberData.ClientInstanceId.Should().Be(client.ClientInstanceId);
        creatorMemberData.PublicKeyInfo.Should().BeEquivalentTo(creatorPublicKeyInfo);
        creatorMemberData.ProfileClientId.Should().Be(creatorProfileClientId);
        creatorMemberData.EncryptedPrivateData.Should().Be(creatorPrivateData);

        // Verify adding client to session
        A.CallTo(() => _mockClientsGroupsService.AddSessionSubscription(
            A<Client>.Ignored,
            A<string>.Ignored,
            A<ITransaction>.Ignored)).MustHaveHappenedOnceExactly();

        A.CallTo(() => _mockClientsGroupsService.AddToSessionGroup(
            A<Client>.Ignored,
            A<string>.Ignored)).MustHaveHappenedOnceExactly();

        // Verify transaction execute
        A.CallTo(() => _mockTransaction.ExecuteAsync(A<CommandFlags>.Ignored)).MustHaveHappenedOnceExactly();

        // Verify result building
        A.CallTo(() => _mockCloudSessionsService.BuildCloudSessionResult(
                A<CloudSessionData>.That.Matches(c => c.SessionId == sessionId),
                A<SessionMemberData>.That.Matches(m => m == creatorMemberData)))
            .MustHaveHappenedOnceExactly();
    }
}