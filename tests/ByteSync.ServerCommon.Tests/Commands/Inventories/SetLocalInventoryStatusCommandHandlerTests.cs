using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Commands.Inventories;
using ByteSync.ServerCommon.Entities.Inventories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
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
    private readonly IInventoryMemberService _mockInventoryMemberService;
    private readonly IInvokeClientsService _mockInvokeClientsService;
    private readonly ILogger<SetLocalInventoryStatusCommandHandler> _mockLogger;
    
    private readonly SetLocalInventoryStatusCommandHandler _setLocalInventoryStatusCommandHandler;
    
    public SetLocalInventoryStatusCommandHandlerTests()
    {
        _mockInventoryRepository = A.Fake<IInventoryRepository>();
        _mockInventoryMemberService = A.Fake<IInventoryMemberService>();
        _mockInvokeClientsService = A.Fake<IInvokeClientsService>();
        _mockLogger = A.Fake<ILogger<SetLocalInventoryStatusCommandHandler>>();
        
        _setLocalInventoryStatusCommandHandler = new SetLocalInventoryStatusCommandHandler(_mockInventoryRepository, _mockInventoryMemberService, 
            _mockInvokeClientsService, _mockLogger);
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

        var inventoryData = new InventoryEntity(sessionId);
        var inventoryMemberData = new InventoryMemberEntity { ClientInstanceId = client.ClientInstanceId };
        
        A.CallTo(() => _mockInventoryMemberService.GetOrCreateInventoryMember(A<InventoryEntity>.Ignored, "testSession", client))
            .Invokes(() => inventoryData.InventoryMembers.Add(inventoryMemberData))
            .Returns(inventoryMemberData);

        InventoryEntity? funcResult = null;
        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(A<string>.Ignored, A<Func<InventoryEntity?, InventoryEntity?>>.Ignored))
            .Invokes((string _, Func<InventoryEntity?, InventoryEntity?> func) =>
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
        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(sessionId, A<Func<InventoryEntity?, InventoryEntity?>>.Ignored))
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

        var inventoryData = new InventoryEntity(sessionId);
        var inventoryMemberData = new InventoryMemberEntity 
        { 
            ClientInstanceId = client.ClientInstanceId,
            SessionMemberGeneralStatus = SessionMemberGeneralStatus.InventoryFinished,
            LastLocalInventoryStatusUpdate = newerDate
        };
        
        A.CallTo(() => _mockInventoryMemberService.GetOrCreateInventoryMember(A<InventoryEntity>.Ignored, "testSession", client))
            .Invokes(() => inventoryData.InventoryMembers.Add(inventoryMemberData))
            .Returns(inventoryMemberData);

        InventoryEntity? funcResult = null;
        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(A<string>.Ignored, A<Func<InventoryEntity?, InventoryEntity?>>.Ignored))
            .Invokes((string _, Func<InventoryEntity?, InventoryEntity?> func) =>
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
        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(sessionId, A<Func<InventoryEntity?, InventoryEntity?>>.Ignored))
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
        
        var inventoryMemberData = new InventoryMemberEntity 
        { 
            ClientInstanceId = client.ClientInstanceId,
            SessionMemberGeneralStatus = SessionMemberGeneralStatus.InventoryFinished,
            LastLocalInventoryStatusUpdate = null
        };
        
        A.CallTo(() => _mockInventoryMemberService.GetOrCreateInventoryMember(A<InventoryEntity>.Ignored, "testSession", client))
            .Invokes((InventoryEntity inventoryEntity, string _, Client _) => 
            {
                inventoryEntity.InventoryMembers.Add(inventoryMemberData);
            })
            .Returns(inventoryMemberData);

        InventoryEntity? capturedInventoryData = null;
        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(A<string>.Ignored, A<Func<InventoryEntity?, InventoryEntity?>>.Ignored))
            .Invokes((string _, Func<InventoryEntity?, InventoryEntity?> func) =>
            {
                // Pass null to simulate non-existing inventory data
                capturedInventoryData = func(null);
            })
            .ReturnsLazily(() => UpdateResultBuilder.BuildAddOrUpdateResult(capturedInventoryData, false));

        var request = new SetLocalInventoryStatusRequest(client, parameters);

        // Act
        var result = await _setLocalInventoryStatusCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().Be(true);
        capturedInventoryData.Should().NotBeNull();
        capturedInventoryData!.SessionId.Should().Be(sessionId);
        capturedInventoryData.InventoryMembers.Should().HaveCount(1);

        var inventoryMember = capturedInventoryData.InventoryMembers.Single();
        inventoryMember.ClientInstanceId.Should().Be(client.ClientInstanceId);
        inventoryMember.SessionMemberGeneralStatus.Should().Be(parameters.SessionMemberGeneralStatus);
        inventoryMember.LastLocalInventoryStatusUpdate.Should().Be(parameters.UtcChangeDate);

        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(sessionId, A<Func<InventoryEntity?, InventoryEntity?>>.Ignored))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task SetLocalInventoryStatus_HandlesRegardlessOfIsInventoryStarted(bool isInventoryStarted)
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

        var inventoryData = new InventoryEntity(sessionId) { IsInventoryStarted = isInventoryStarted };
        var inventoryMemberData = new InventoryMemberEntity
        {
            ClientInstanceId = client.ClientInstanceId,
            SessionMemberGeneralStatus = SessionMemberGeneralStatus.InventoryFinished,
            LastLocalInventoryStatusUpdate = null
        };

        A.CallTo(() => _mockInventoryMemberService.GetOrCreateInventoryMember(A<InventoryEntity>.Ignored, sessionId, client))
            .Invokes(() => inventoryData.InventoryMembers.Add(inventoryMemberData))
            .Returns(inventoryMemberData);

        InventoryEntity? funcResult = null;
        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(A<string>.Ignored, A<Func<InventoryEntity?, InventoryEntity?>>.Ignored))
            .Invokes((string _, Func<InventoryEntity?, InventoryEntity?> func) =>
            {
                funcResult = func(inventoryData);
            })
            .ReturnsLazily(() => UpdateResultBuilder.BuildAddOrUpdateResult(funcResult, false));

        var request = new SetLocalInventoryStatusRequest(client, parameters);

        // Act
        var result = await _setLocalInventoryStatusCommandHandler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().Be(true);
        inventoryData.InventoryMembers.Single().SessionMemberGeneralStatus.Should().Be(parameters.SessionMemberGeneralStatus);
        inventoryData.InventoryMembers.Single().LastLocalInventoryStatusUpdate.Should().Be(parameters.UtcChangeDate);
        A.CallTo(() => _mockInventoryRepository.AddOrUpdate(sessionId, A<Func<InventoryEntity?, InventoryEntity?>>.Ignored))
            .MustHaveHappenedOnceExactly();
    }
}
