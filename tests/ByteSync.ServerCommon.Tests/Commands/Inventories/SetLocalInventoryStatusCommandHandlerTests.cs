using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.Inventories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using RedLockNet;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Tests.Commands.Inventories;

public class SetLocalInventoryStatusCommandHandlerTests
{
    private readonly IInventoryRepository _mockInventoryRepository;
    private readonly IClientsGroupsInvoker _mockGroupsInvoker;
    private readonly ILogger<SetLocalInventoryStatusCommandHandler> _mockLogger;
    
    private readonly SetLocalInventoryStatusCommandHandler _setLocalInventoryStatusCommandHandler;

    public SetLocalInventoryStatusCommandHandlerTests()
    {
        _mockInventoryRepository = A.Fake<IInventoryRepository>();
        _mockGroupsInvoker = A.Fake<IClientsGroupsInvoker>();
        _mockLogger = A.Fake<ILogger<SetLocalInventoryStatusCommandHandler>>();
        
        _setLocalInventoryStatusCommandHandler = new SetLocalInventoryStatusCommandHandler(_mockInventoryRepository, _mockGroupsInvoker, _mockLogger);
    }
    
    [Test]
    public async Task SetLocalInventoryStatus_UpdatesStatus_WhenUtcChangeDateIsLater()
    {
        // Arrange
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

        A.CallTo(() => _mockInventoryRepository.Update(A<string>.Ignored, A<Func<InventoryData, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .Invokes((string _, Func<InventoryData, bool> func, ITransaction _, IRedLock _) => func(inventoryData))
            .Returns(new UpdateEntityResult<InventoryData>(inventoryData, UpdateEntityStatus.Saved));

        var request = new SetLocalInventoryStatusRequest(client, parameters);

        // Act
        var result  = await _setLocalInventoryStatusCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().Be(true);
        inventoryData.InventoryMembers.Single().SessionMemberGeneralStatus.Should().Be(parameters.SessionMemberGeneralStatus);
        inventoryData.InventoryMembers.Single().LastLocalInventoryStatusUpdate.Should().Be(parameters.UtcChangeDate);
        A.CallTo(() => _mockInventoryRepository.Update(sessionId, A<Func<InventoryData, bool>>.Ignored, A<ITransaction>.Ignored, A<IRedLock>.Ignored))
            .MustHaveHappenedOnceExactly();
    }
}