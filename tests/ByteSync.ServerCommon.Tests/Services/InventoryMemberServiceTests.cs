using ByteSync.Common.Business.Sessions;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Services;
using FluentAssertions;

namespace ByteSync.ServerCommon.Tests.Services;

[TestFixture]
public class InventoryMemberServiceTests
{
    private IInventoryMemberService _inventoryMemberService;
    private InventoryData _inventoryData;
    private Client _client;
    private string _sessionId;

    [SetUp]
    public void Setup()
    {
        _inventoryMemberService = new InventoryMemberService();
        _inventoryData = new InventoryData
        {
            InventoryMembers = new List<InventoryMemberData>()
        };
        _client = new Client();
        _client.ClientInstanceId = "client123";
        _sessionId = "testSessionId";
    }

    [Test]
    public void GetOrCreateInventoryMember_WhenInventoryMemberDoesNotExist_ShouldCreateNewInventoryMember()
    {
        // Act
        var result = _inventoryMemberService.GetOrCreateInventoryMember(_inventoryData, _sessionId, _client);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be(_sessionId);
        result.ClientInstanceId.Should().Be(_client.ClientInstanceId);
        result.SessionMemberGeneralStatus.Should().Be(SessionMemberGeneralStatus.InventoryWaitingForStart);
        _inventoryData.InventoryMembers.Should().ContainSingle();
        _inventoryData.InventoryMembers.First().Should().BeEquivalentTo(result);
    }

    [Test]
    public void GetOrCreateInventoryMember_WhenInventoryMemberExists_ShouldReturnExistingInventoryMember()
    {
        // Arrange
        var existingInventoryMember = new InventoryMemberData
        {
            ClientInstanceId = _client.ClientInstanceId,
            SessionId = _sessionId,
            SessionMemberGeneralStatus = SessionMemberGeneralStatus.InventoryWaitingForStart
        };

        _inventoryData.InventoryMembers.Add(existingInventoryMember);

        // Act
        var result = _inventoryMemberService.GetOrCreateInventoryMember(_inventoryData, _sessionId, _client);

        // Assert
        result.Should().Be(existingInventoryMember);
    }
}