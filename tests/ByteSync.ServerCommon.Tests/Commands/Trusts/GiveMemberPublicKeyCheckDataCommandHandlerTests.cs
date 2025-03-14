using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Trust.Connections;
using ByteSync.Common.Interfaces.Hub;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Commands.Trusts;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Tests.Commands.Trusts;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class GiveMemberPublicKeyCheckDataCommandHandlerTests
{
    private readonly IInvokeClientsService _mockInvokeClientsService;
    private readonly ILogger<GiveMemberPublicKeyCheckDataCommandHandler> _mockLogger;
    private readonly IHubByteSyncPush _mockHubByteSyncPush;
    private readonly GiveMemberPublicKeyCheckDataCommandHandler _handler;

    public GiveMemberPublicKeyCheckDataCommandHandlerTests()
    {
        _mockInvokeClientsService = A.Fake<IInvokeClientsService>();
        _mockLogger = A.Fake<ILogger<GiveMemberPublicKeyCheckDataCommandHandler>>();
        _mockHubByteSyncPush = A.Fake<IHubByteSyncPush>();
        
        _handler = new GiveMemberPublicKeyCheckDataCommandHandler(
            _mockInvokeClientsService,
            _mockLogger);
    }

    [Test]
    public async Task Handle_ShouldInvokeClient_WithCorrectParameters()
    {
        // Arrange
        var sessionId = "testSession";
        var clientInstanceId = "recipientInstance";
        var senderClient = new Client { ClientId = "senderClient", ClientInstanceId = "senderInstance" };
        var publicKeyCheckData = new PublicKeyCheckData();
        
        var parameters = new GiveMemberPublicKeyCheckDataParameters
        {
            SessionId = sessionId,
            ClientInstanceId = clientInstanceId,
            PublicKeyCheckData = publicKeyCheckData
        };

        A.CallTo(() => _mockInvokeClientsService.Client(clientInstanceId))
            .Returns(_mockHubByteSyncPush);
        
        A.CallTo(() => _mockHubByteSyncPush.GiveMemberPublicKeyCheckData(sessionId, publicKeyCheckData))
            .Returns(Task.CompletedTask);

        var request = new GiveMemberPublicKeyCheckDataRequest(parameters, senderClient);

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockInvokeClientsService.Client(clientInstanceId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockHubByteSyncPush.GiveMemberPublicKeyCheckData(sessionId, publicKeyCheckData))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_WithMultipleRequests_ShouldInvokeCorrectClients()
    {
        // Arrange
        var sessionId = "testSession";
        var clientInstanceId1 = "recipientInstance1";
        var clientInstanceId2 = "recipientInstance2";
        var senderClient = new Client { ClientId = "senderClient", ClientInstanceId = "senderInstance" };
        
        var publicKeyCheckData1 = new PublicKeyCheckData();
        var publicKeyCheckData2 = new PublicKeyCheckData();
        
        var mockByteSyncClient1 = A.Fake<IHubByteSyncPush>();
        var mockByteSyncClient2 = A.Fake<IHubByteSyncPush>();

        var parameters1 = new GiveMemberPublicKeyCheckDataParameters
        {
            SessionId = sessionId,
            ClientInstanceId = clientInstanceId1,
            PublicKeyCheckData = publicKeyCheckData1
        };

        var parameters2 = new GiveMemberPublicKeyCheckDataParameters
        {
            SessionId = sessionId,
            ClientInstanceId = clientInstanceId2,
            PublicKeyCheckData = publicKeyCheckData2
        };

        A.CallTo(() => _mockInvokeClientsService.Client(clientInstanceId1))
            .Returns(mockByteSyncClient1);
        A.CallTo(() => _mockInvokeClientsService.Client(clientInstanceId2))
            .Returns(mockByteSyncClient2);
        
        A.CallTo(() => mockByteSyncClient1.GiveMemberPublicKeyCheckData(sessionId, publicKeyCheckData1))
            .Returns(Task.CompletedTask);
        A.CallTo(() => mockByteSyncClient2.GiveMemberPublicKeyCheckData(sessionId, publicKeyCheckData2))
            .Returns(Task.CompletedTask);

        var request1 = new GiveMemberPublicKeyCheckDataRequest(parameters1, senderClient);
        var request2 = new GiveMemberPublicKeyCheckDataRequest(parameters2, senderClient);

        // Act
        await _handler.Handle(request1, CancellationToken.None);
        await _handler.Handle(request2, CancellationToken.None);

        // Assert
        A.CallTo(() => _mockInvokeClientsService.Client(clientInstanceId1)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockInvokeClientsService.Client(clientInstanceId2)).MustHaveHappenedOnceExactly();
        A.CallTo(() => mockByteSyncClient1.GiveMemberPublicKeyCheckData(sessionId, publicKeyCheckData1))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => mockByteSyncClient2.GiveMemberPublicKeyCheckData(sessionId, publicKeyCheckData2))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_WhenClientFails_ShouldPropagateException()
    {
        // Arrange
        var sessionId = "testSession";
        var clientInstanceId = "recipientInstance";
        var senderClient = new Client { ClientId = "senderClient", ClientInstanceId = "senderInstance" };
        var publicKeyCheckData = new PublicKeyCheckData();
        
        var parameters = new GiveMemberPublicKeyCheckDataParameters
        {
            SessionId = sessionId,
            ClientInstanceId = clientInstanceId,
            PublicKeyCheckData = publicKeyCheckData
        };

        var exception = new InvalidOperationException("Test exception");
        
        A.CallTo(() => _mockInvokeClientsService.Client(clientInstanceId))
            .Returns(_mockHubByteSyncPush);
        
        A.CallTo(() => _mockHubByteSyncPush.GiveMemberPublicKeyCheckData(sessionId, publicKeyCheckData))
            .ThrowsAsync(exception);

        var request = new GiveMemberPublicKeyCheckDataRequest(parameters, senderClient);

        // Act/Assert
        await FluentActions.Invoking(async () => 
            await _handler.Handle(request, CancellationToken.None)
        ).Should().ThrowAsync<InvalidOperationException>().WithMessage("Test exception");
        
        A.CallTo(() => _mockInvokeClientsService.Client(clientInstanceId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mockHubByteSyncPush.GiveMemberPublicKeyCheckData(sessionId, publicKeyCheckData))
            .MustHaveHappenedOnceExactly();
    }
}