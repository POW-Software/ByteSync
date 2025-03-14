using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Interfaces.Hub;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.Trusts;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Tests.Commands.Trusts;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class StartTrustCheckCommandHandlerTests
{
    private readonly ICloudSessionsRepository _mockCloudSessionsRepository;
    private readonly IInvokeClientsService _mockInvokeClientsService;
    private readonly ILogger<StartTrustCheckCommandHandler> _mockLogger;
    private readonly IHubByteSyncPush _mockByteSyncPush;
    
    private readonly StartTrustCheckCommandHandler _startTrustCheckCommandHandler;

    public StartTrustCheckCommandHandlerTests()
    {
        _mockCloudSessionsRepository = A.Fake<ICloudSessionsRepository>();
        _mockInvokeClientsService = A.Fake<IInvokeClientsService>();
        _mockLogger = A.Fake<ILogger<StartTrustCheckCommandHandler>>();
        _mockByteSyncPush = A.Fake<IHubByteSyncPush>();
        
        _startTrustCheckCommandHandler = new StartTrustCheckCommandHandler(
            _mockCloudSessionsRepository, 
            _mockInvokeClientsService,
            _mockLogger);
    }
    
    [Test]
    public async Task Handle_SessionExists_WithMembers_ReturnsSuccessResult()
    {
        // Arrange
        var sessionId = "testSession";
        var joinerClient = new Client { ClientId = "joinerClient", ClientInstanceId = "joinerClientInstance" };
        var member1 = "memberInstance1";
        var member2 = "memberInstance2";
        var member3 = "nonExistentMemberInstance";
        
        var publicKeyInfo = new PublicKeyInfo();
        var parameters = new TrustCheckParameters
        {
            SessionId = sessionId,
            MembersInstanceIdsToCheck = new List<string> { member1, member2, member3 },
            PublicKeyInfo = publicKeyInfo
        };
        
        var cloudSession = new CloudSessionData(sessionId, new EncryptedSessionSettings(), new Client { ClientInstanceId = "member1" });
        cloudSession.SessionMembers.Add(new SessionMemberData { ClientInstanceId = member1 });
        cloudSession.SessionMembers.Add(new SessionMemberData { ClientInstanceId = member2 });
        
        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId))
            .Returns(cloudSession);
        
        A.CallTo(() => _mockInvokeClientsService.Client(member1))
            .Returns(_mockByteSyncPush);
        A.CallTo(() => _mockInvokeClientsService.Client(member2))
            .Returns(_mockByteSyncPush);
            
        A.CallTo(() => _mockByteSyncPush.AskPublicKeyCheckData(sessionId, joinerClient.ClientInstanceId, publicKeyInfo))
            .Returns(Task.CompletedTask);
        
        var request = new StartTrustCheckRequest(parameters, joinerClient);
        
        // Act
        var result = await _startTrustCheckCommandHandler.Handle(request, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        result.IsOK.Should().BeTrue();
        result.MembersInstanceIds.Should().HaveCount(2);
        result.MembersInstanceIds.Should().Contain(member1);
        result.MembersInstanceIds.Should().Contain(member2);
        result.MembersInstanceIds.Should().NotContain(member3);
        
        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockInvokeClientsService.Client(member1)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockInvokeClientsService.Client(member2)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockByteSyncPush.AskPublicKeyCheckData(sessionId, joinerClient.ClientInstanceId, publicKeyInfo))
            .MustHaveHappened(2, Times.Exactly);
    }
    
    [Test]
    public async Task Handle_SessionDoesNotExist_ReturnsFailureResult()
    {
        // Arrange
        var sessionId = "nonExistentSession";
        var joinerClient = new Client { ClientId = "joinerClient", ClientInstanceId = "joinerClientInstance" };
        var member1 = "memberInstance1";
        
        var parameters = new TrustCheckParameters
        {
            SessionId = sessionId,
            MembersInstanceIdsToCheck = new List<string> { member1 },
            PublicKeyInfo = new PublicKeyInfo()
        };
        
        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId))
            .Returns(Task.FromResult<CloudSessionData?>(null));
        
        var request = new StartTrustCheckRequest(parameters, joinerClient);
        
        // Act
        var result = await _startTrustCheckCommandHandler.Handle(request, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        result.IsOK.Should().BeFalse();
        result.MembersInstanceIds.Should().BeEmpty();
        
        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockInvokeClientsService.Client(A<string>.Ignored)).MustNotHaveHappened();
    }
    
    [Test]
    public async Task Handle_SessionExistsButNoValidMembers_ReturnsEmptySuccessResult()
    {
        // Arrange
        var sessionId = "testSession";
        var joinerClient = new Client { ClientId = "joinerClient", ClientInstanceId = "joinerClientInstance" };
        var nonExistentMember = "nonExistentMember";
        
        var parameters = new TrustCheckParameters
        {
            SessionId = sessionId,
            MembersInstanceIdsToCheck = new List<string> { nonExistentMember },
            PublicKeyInfo = new PublicKeyInfo()
        };
        
        var cloudSession = new CloudSessionData(sessionId, new EncryptedSessionSettings(), new Client { ClientInstanceId = "creator" });
        cloudSession.SessionMembers.Add(new SessionMemberData { ClientInstanceId = "otherMember" });
        
        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId))
            .Returns(cloudSession);
        
        var request = new StartTrustCheckRequest(parameters, joinerClient);
        
        // Act
        var result = await _startTrustCheckCommandHandler.Handle(request, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        result.IsOK.Should().BeTrue();
        result.MembersInstanceIds.Should().BeEmpty();
        
        A.CallTo(() => _mockCloudSessionsRepository.Get(sessionId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockInvokeClientsService.Client(A<string>.Ignored)).MustNotHaveHappened();
    }
}