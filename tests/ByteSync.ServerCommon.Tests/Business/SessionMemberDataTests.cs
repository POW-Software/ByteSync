using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using FluentAssertions;

namespace ByteSync.ServerCommon.Tests.Business;

[TestFixture]
public class SessionMemberDataTests
{
    private SessionMemberData _sessionMemberData;
    private CloudSessionData _cloudSessionData;
    private Client _client;
    private PublicKeyInfo _publicKeyInfo;
    private string _profileClientId;
    
    [SetUp]
    public void Setup()
    {
        _cloudSessionData = new CloudSessionData { SessionId = "session-123" };
        _client = new Client { ClientInstanceId = "client-123", ClientId = "id-123" };
        _publicKeyInfo = new PublicKeyInfo { ClientId = "public-key-data" };
        _profileClientId = "profile-123";
        _sessionMemberData = new SessionMemberData();
    }

    [Test]
    public void DefaultConstructor_ShouldInitializeProperties()
    {
        // Assert
        _sessionMemberData.JoinedSessionOn.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        _sessionMemberData.AuthCheckClientInstanceIds.Should().NotBeNull();
        _sessionMemberData.AuthCheckClientInstanceIds.Should().BeEmpty();
    }

    [Test]
    public void ParameterizedConstructor_WithClient_ShouldInitializeProperties()
    {
        // Act
        var memberData = new SessionMemberData(_client, _publicKeyInfo, _profileClientId, _cloudSessionData);

        // Assert
        memberData.ClientInstanceId.Should().Be(_client.ClientInstanceId);
        memberData.ClientId.Should().Be(_client.ClientId);
        memberData.PublicKeyInfo.Should().Be(_publicKeyInfo);
        memberData.ProfileClientId.Should().Be(_profileClientId);
        memberData.CloudSessionData.Should().Be(_cloudSessionData);
        memberData.JoinedSessionOn.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        memberData.AuthCheckClientInstanceIds.Should().NotBeNull();
        memberData.AuthCheckClientInstanceIds.Should().BeEmpty();
    }

    [Test]
    public void ParameterizedConstructor_WithStrings_ShouldInitializeProperties()
    {
        // Act
        var memberData = new SessionMemberData(
            "client-instance-456", 
            "client-id-456", 
            _publicKeyInfo, 
            _profileClientId, 
            _cloudSessionData);

        // Assert
        memberData.ClientInstanceId.Should().Be("client-instance-456");
        memberData.ClientId.Should().Be("client-id-456");
        memberData.PublicKeyInfo.Should().Be(_publicKeyInfo);
        memberData.ProfileClientId.Should().Be(_profileClientId);
        memberData.CloudSessionData.Should().Be(_cloudSessionData);
        memberData.JoinedSessionOn.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        memberData.AuthCheckClientInstanceIds.Should().NotBeNull();
        memberData.AuthCheckClientInstanceIds.Should().BeEmpty();
    }

    [Test]
    public void ParameterizedConstructor_WithEncryptedData_ShouldInitializeEncryptedData()
    {
        // Arrange
        var encryptedData = new EncryptedSessionMemberPrivateData();

        // Act
        var memberData = new SessionMemberData(
            _client, 
            _publicKeyInfo, 
            _profileClientId, 
            _cloudSessionData, 
            encryptedData);

        // Assert
        memberData.EncryptedPrivateData.Should().BeSameAs(encryptedData);
    }

    [Test]
    public void PositionInList_WhenMemberExists_ShouldReturnCorrectIndex()
    {
        // Arrange
        _sessionMemberData.ClientInstanceId = "client-456";
        _sessionMemberData.CloudSessionData = _cloudSessionData;
        
        var firstMember = new SessionMemberData { ClientInstanceId = "client-123" };
        var secondMember = _sessionMemberData;
        var thirdMember = new SessionMemberData { ClientInstanceId = "client-789" };
        
        _cloudSessionData.SessionMembers.Add(firstMember);
        _cloudSessionData.SessionMembers.Add(secondMember);
        _cloudSessionData.SessionMembers.Add(thirdMember);

        // Act
        var position = _sessionMemberData.PositionInList;

        // Assert
        position.Should().Be(1);
    }

    [Test]
    public void PositionInList_WhenMemberNotInList_ShouldReturnMinusOne()
    {
        // Arrange
        _sessionMemberData.ClientInstanceId = "not-in-list";
        _sessionMemberData.CloudSessionData = _cloudSessionData;
        
        _cloudSessionData.SessionMembers.Add(new SessionMemberData { ClientInstanceId = "client-123" });
        _cloudSessionData.SessionMembers.Add(new SessionMemberData { ClientInstanceId = "client-456" });

        // Act
        var position = _sessionMemberData.PositionInList;

        // Assert
        position.Should().Be(-1);
    }

    [Test]
    public void Equals_WithSameClientInstanceId_ShouldReturnTrue()
    {
        // Arrange
        var firstMember = new SessionMemberData { ClientInstanceId = "same-id" };
        var secondMember = new SessionMemberData { ClientInstanceId = "same-id" };

        // Act
        bool result = firstMember.Equals(secondMember);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void Equals_WithDifferentClientInstanceId_ShouldReturnFalse()
    {
        // Arrange
        var firstMember = new SessionMemberData { ClientInstanceId = "first-id" };
        var secondMember = new SessionMemberData { ClientInstanceId = "second-id" };

        // Act
        bool result = firstMember.Equals(secondMember);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void Equals_WithNonSessionMemberDataObject_ShouldReturnFalse()
    {
        // Arrange
        var memberData = new SessionMemberData { ClientInstanceId = "member-id" };
        var otherObject = new object();

        // Act
        bool result = memberData.Equals(otherObject);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void GetHashCode_WithSameClientInstanceId_ShouldReturnSameValue()
    {
        // Arrange
        var clientInstanceId = "same-id";
        var firstMember = new SessionMemberData { ClientInstanceId = clientInstanceId };
        var secondMember = new SessionMemberData { ClientInstanceId = clientInstanceId };

        // Act
        var firstHash = firstMember.GetHashCode();
        var secondHash = secondMember.GetHashCode();

        // Assert
        firstHash.Should().Be(secondHash);
        firstHash.Should().Be(clientInstanceId.GetHashCode());
    }

    [Test]
    public void IsAuthCheckedFor_WhenInstanceIdInAuthCheckList_ShouldReturnTrue()
    {
        // Arrange
        _sessionMemberData.AuthCheckClientInstanceIds = new List<string> { "auth-checked-id" };

        // Act
        bool result = _sessionMemberData.IsAuthCheckedFor("auth-checked-id");

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsAuthCheckedFor_WhenInstanceIdStartsWithClientId_ShouldReturnTrue()
    {
        // Arrange
        _sessionMemberData.ClientId = "client-prefix";

        // Act
        bool result = _sessionMemberData.IsAuthCheckedFor("client-prefix-suffix");

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsAuthCheckedFor_WhenInstanceIdNotChecked_ShouldReturnFalse()
    {
        // Arrange
        _sessionMemberData.ClientId = "client-id";
        _sessionMemberData.AuthCheckClientInstanceIds = new List<string> { "other-id" };

        // Act
        bool result = _sessionMemberData.IsAuthCheckedFor("unchecked-id");

        // Assert
        result.Should().BeFalse();
    }
}