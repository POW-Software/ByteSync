using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.Inventories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using ByteSync.ServerCommon.Tests.Helpers;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Tests.Commands.Inventories;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class SetLocalInventoryStatusCommandHandlerTests
{
    private readonly IInventoryRepository _mockInventoryRepository;
    private readonly IInvokeClientsService _mockService;
    private readonly ILogger<SetLocalInventoryStatusCommandHandler> _mockLogger;
    
    private readonly SetLocalInventoryStatusCommandHandler _setLocalInventoryStatusCommandHandler;

    public SetLocalInventoryStatusCommandHandlerTests()
    {
        _mockInventoryRepository = A.Fake<IInventoryRepository>();
        _mockService = A.Fake<IInvokeClientsService>();
        _mockLogger = A.Fake<ILogger<SetLocalInventoryStatusCommandHandler>>();
        
        _setLocalInventoryStatusCommandHandler = new SetLocalInventoryStatusCommandHandler(_mockInventoryRepository, _mockService, _mockLogger);
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

        InventoryData? funcResult = null;
        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(A<string>.Ignored, A<Func<InventoryData?, InventoryData?>>.Ignored))
            .Invokes((string _, Func<InventoryData?, InventoryData?> func) =>
            {
                funcResult = func(inventoryData);
            })
            .ReturnsLazily(() => UpdateResultBuilder.BuildAddOrUpdateResult(funcResult, false));

        var request = new SetLocalInventoryStatusRequest(client, parameters);

        // Act
        var result  = await _setLocalInventoryStatusCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().Be(true);
        inventoryData.InventoryMembers.Single().SessionMemberGeneralStatus.Should().Be(parameters.SessionMemberGeneralStatus);
        inventoryData.InventoryMembers.Single().LastLocalInventoryStatusUpdate.Should().Be(parameters.UtcChangeDate);
        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(sessionId, A<Func<InventoryData?, InventoryData?>>.Ignored))
            .MustHaveHappenedOnceExactly();
    }
    
    [Test]
    public async Task SetLocalInventoryStatus_DoesNotUpdateStatus_WhenUtcChangeDateIsOlder()
    {
        // Arrange
        var sessionId = "testSession";
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        var olderDate = DateTime.UtcNow.AddMinutes(-10);
        var newerDate = DateTime.UtcNow;
        var parameters = new UpdateSessionMemberGeneralStatusParameters
        {
            SessionId = "testSession",
            UtcChangeDate = olderDate,
            SessionMemberGeneralStatus = SessionMemberGeneralStatus.InventoryRunningAnalysis
        };

        var inventoryData = new InventoryData(sessionId);
        var inventoryMember = new InventoryMemberData 
        { 
            ClientInstanceId = client.ClientInstanceId,
            SessionMemberGeneralStatus = SessionMemberGeneralStatus.InventoryFinished,
            LastLocalInventoryStatusUpdate = newerDate
        };
        inventoryData.InventoryMembers.Add(inventoryMember);

        InventoryData? funcResult = null;
        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(A<string>.Ignored, A<Func<InventoryData?, InventoryData?>>.Ignored))
            .Invokes((string _, Func<InventoryData?, InventoryData?> func) =>
            {
                funcResult = func(inventoryData);
            })
            .ReturnsLazily(() => UpdateResultBuilder.BuildAddOrUpdateResult(funcResult, false));

        var request = new SetLocalInventoryStatusRequest(client, parameters);

        // Act
        var result = await _setLocalInventoryStatusCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().Be(false);
        inventoryData.InventoryMembers.Single().SessionMemberGeneralStatus.Should().Be(SessionMemberGeneralStatus.InventoryFinished);
        inventoryData.InventoryMembers.Single().LastLocalInventoryStatusUpdate.Should().Be(newerDate);
        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(sessionId, A<Func<InventoryData?, InventoryData?>>.Ignored))
            .MustHaveHappenedOnceExactly();
    }
    
    [Test]
    public async Task SetLocalInventoryStatus_CreatesNewInventoryData_WhenInventoryDataIsNull()
    {
        // Arrange
        var sessionId = "testSession";
        var client = new Client { ClientId = "client1", ClientInstanceId = "clientInstanceId1" };
        var currentDate = DateTime.UtcNow;
        var parameters = new UpdateSessionMemberGeneralStatusParameters
        {
            SessionId = sessionId,
            UtcChangeDate = currentDate,
            SessionMemberGeneralStatus = SessionMemberGeneralStatus.InventoryRunningAnalysis
        };

        InventoryData? capturedInventoryData = null;
        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(A<string>.Ignored, A<Func<InventoryData?, InventoryData?>>.Ignored))
            .Invokes((string _, Func<InventoryData?, InventoryData?> func) =>
            {
                // Pass null to simulate non-existing inventory data
                capturedInventoryData = func(null);
            })
            .ReturnsLazily(() => UpdateResultBuilder.BuildAddOrUpdateResult(capturedInventoryData, true));

        var request = new SetLocalInventoryStatusRequest(client, parameters);

        // Act
        var result = await _setLocalInventoryStatusCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().Be(false);
        capturedInventoryData.Should().BeNull();

        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(sessionId, A<Func<InventoryData?, InventoryData?>>.Ignored))
            .MustHaveHappenedOnceExactly();
    }
}