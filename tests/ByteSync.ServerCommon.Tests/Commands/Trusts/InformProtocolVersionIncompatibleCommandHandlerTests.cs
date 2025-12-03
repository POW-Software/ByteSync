using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Common.Business.Versions;
using ByteSync.Common.Interfaces.Hub;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Commands.Trusts;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Tests.Commands.Trusts;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class InformProtocolVersionIncompatibleCommandHandlerTests
{
    private readonly IInvokeClientsService _mockInvokeClientsService;
    private readonly ILogger<InformProtocolVersionIncompatibleCommandHandler> _mockLogger;
    private readonly IHubByteSyncPush _mockHubByteSyncPush;
    private readonly InformProtocolVersionIncompatibleCommandHandler _handler;
    
    public InformProtocolVersionIncompatibleCommandHandlerTests()
    {
        _mockInvokeClientsService = A.Fake<IInvokeClientsService>();
        _mockLogger = A.Fake<ILogger<InformProtocolVersionIncompatibleCommandHandler>>();
        _mockHubByteSyncPush = A.Fake<IHubByteSyncPush>();
        
        _handler = new InformProtocolVersionIncompatibleCommandHandler(
            _mockInvokeClientsService,
            _mockLogger);
    }
    
    [Test]
    public async Task Handle_ShouldInvokeClient_WithCorrectParameters()
    {
        var sessionId = "testSession";
        var memberClientInstanceId = "memberInstance";
        var joinerClientInstanceId = "joinerInstance";
        var memberProtocolVersion = ProtocolVersion.CURRENT;
        var joinerProtocolVersion = 0;
        
        var senderClient = new Client { ClientId = "senderClient", ClientInstanceId = "senderInstance" };
        
        var parameters = new InformProtocolVersionIncompatibleParameters
        {
            SessionId = sessionId,
            MemberClientInstanceId = memberClientInstanceId,
            JoinerClientInstanceId = joinerClientInstanceId,
            MemberProtocolVersion = memberProtocolVersion,
            JoinerProtocolVersion = joinerProtocolVersion
        };
        
        A.CallTo(() => _mockInvokeClientsService.Client(joinerClientInstanceId))
            .Returns(_mockHubByteSyncPush);
        
        A.CallTo(() => _mockHubByteSyncPush.InformProtocolVersionIncompatible(parameters))
            .Returns(Task.CompletedTask);
        
        var request = new InformProtocolVersionIncompatibleRequest(parameters, senderClient);
        
        await _handler.Handle(request, CancellationToken.None);
        
        A.CallTo(() => _mockInvokeClientsService.Client(joinerClientInstanceId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockHubByteSyncPush.InformProtocolVersionIncompatible(parameters))
            .MustHaveHappenedOnceExactly();
    }
    
    [Test]
    public async Task Handle_WhenClientFails_ShouldPropagateException()
    {
        var sessionId = "testSession";
        var memberClientInstanceId = "memberInstance";
        var joinerClientInstanceId = "joinerInstance";
        var memberProtocolVersion = ProtocolVersion.CURRENT;
        var joinerProtocolVersion = 0;
        
        var senderClient = new Client { ClientId = "senderClient", ClientInstanceId = "senderInstance" };
        
        var parameters = new InformProtocolVersionIncompatibleParameters
        {
            SessionId = sessionId,
            MemberClientInstanceId = memberClientInstanceId,
            JoinerClientInstanceId = joinerClientInstanceId,
            MemberProtocolVersion = memberProtocolVersion,
            JoinerProtocolVersion = joinerProtocolVersion
        };
        
        var exception = new InvalidOperationException("Test exception");
        
        A.CallTo(() => _mockInvokeClientsService.Client(joinerClientInstanceId))
            .Returns(_mockHubByteSyncPush);
        
        A.CallTo(() => _mockHubByteSyncPush.InformProtocolVersionIncompatible(parameters))
            .ThrowsAsync(exception);
        
        var request = new InformProtocolVersionIncompatibleRequest(parameters, senderClient);
        
        await FluentActions.Invoking(async () =>
            await _handler.Handle(request, CancellationToken.None)
        ).Should().ThrowAsync<InvalidOperationException>().WithMessage("Test exception");
        
        A.CallTo(() => _mockInvokeClientsService.Client(joinerClientInstanceId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockHubByteSyncPush.InformProtocolVersionIncompatible(parameters))
            .MustHaveHappenedOnceExactly();
    }
}