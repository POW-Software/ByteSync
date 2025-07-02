using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using FluentAssertions;

namespace ByteSync.ServerCommon.Tests.Business;

[TestFixture]
public class CloudSessionDataTests
{
    private CloudSessionData _cloudSessionData;
    private EncryptedSessionSettings _sessionSettings;
    private Client _creator;

    [SetUp]
    public void Setup()
    {
        _sessionSettings = new EncryptedSessionSettings();
        _creator = new Client { ClientInstanceId = "creator-123" };
        _cloudSessionData = new CloudSessionData();
    }

    [Test]
    public void DefaultConstructor_ShouldInitializeCollections()
    {
        // Assert
        _cloudSessionData.SessionMembers.Should().NotBeNull();
        _cloudSessionData.SessionMembers.Should().BeEmpty();
        _cloudSessionData.PreSessionMembers.Should().NotBeNull();
        _cloudSessionData.PreSessionMembers.Should().BeEmpty();
    }

    [Test]
    public void ParameterizedConstructor_ShouldInitializeProperties()
    {
        // Arrange
        string lobbyId = "lobby-123";

        // Act
        var sessionData = new CloudSessionData(lobbyId, _sessionSettings, _creator);

        // Assert
        sessionData.LobbyId.Should().Be(lobbyId);
        sessionData.SessionSettings.Should().BeSameAs(_sessionSettings);
        sessionData.CreatorInstanceId.Should().Be(_creator.ClientInstanceId);
        sessionData.SessionMembers.Should().NotBeNull().And.BeEmpty();
        sessionData.PreSessionMembers.Should().NotBeNull().And.BeEmpty();
    }

    // [Test]
    // public void IsSessionOnError_WhenCloudSessionFatalErrorIsNull_ShouldReturnFalse()
    // {
    //     // Act & Assert
    //     _cloudSessionData.IsSessionOnError.Should().BeFalse();
    // }

    // [Test]
    // public void IsSessionOnError_WhenCloudSessionFatalErrorIsNotNull_ShouldReturnTrue()
    // {
    //     // Arrange
    //     _cloudSessionData.SetSessionOnFatalError(CloudSessionFatalErrors.MemberQuittedAfterActivation);
    //
    //     // Act & Assert
    //     _cloudSessionData.IsSessionOnError.Should().BeTrue();
    // }

    [Test]
    public void Contains_WithExistingEndpoint_ShouldReturnTrue()
    {
        // Arrange
        string clientInstanceId = "client-123";
        _cloudSessionData.SessionMembers.Add(new SessionMemberData { ClientInstanceId = clientInstanceId });
        var endpoint = new ByteSyncEndpoint { ClientInstanceId = clientInstanceId };

        // Act
        bool result = _cloudSessionData.Contains(endpoint);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void Contains_WithNonExistingEndpoint_ShouldReturnFalse()
    {
        // Arrange
        var endpoint = new ByteSyncEndpoint { ClientInstanceId = "non-existing" };

        // Act
        bool result = _cloudSessionData.Contains(endpoint);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void SetSessionActivated_ShouldUpdateSessionSettingsAndActivateSession()
    {
        // Arrange
        var newSessionSettings = new EncryptedSessionSettings();

        // Act
        _cloudSessionData.SetSessionActivated(newSessionSettings);

        // Assert
        _cloudSessionData.SessionSettings.Should().BeSameAs(newSessionSettings);
        _cloudSessionData.IsSessionActivated.Should().BeTrue();
    }

    [Test]
    public void UpdateSessionSettings_ShouldReplaceSessionSettings()
    {
        // Arrange
        var newSessionSettings = new EncryptedSessionSettings();

        // Act
        _cloudSessionData.UpdateSessionSettings(newSessionSettings);

        // Assert
        _cloudSessionData.SessionSettings.Should().BeSameAs(newSessionSettings);
    }

    /*[Test]
    public void SetSessionOnFatalError_ShouldSetErrorAndReturnIt()
    {
        // Arrange
        _cloudSessionData.SessionId = "session-123";
        var fatalError = CloudSessionFatalErrors.MemberQuittedAfterActivation;

        // Act
        var result = _cloudSessionData.SetSessionOnFatalError(fatalError);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be(_cloudSessionData.SessionId);
        result.SessionFatalError.Should().Be(fatalError);
        result.HappenedOn.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        _cloudSessionData.CloudSessionFatalError.Should().BeSameAs(result);
    }*/

    /*[Test]
    public void ResetSession_ShouldClearActivatedStatusAndError()
    {
        // Arrange
        _cloudSessionData.IsSessionActivated = true;
        _cloudSessionData.SetSessionOnFatalError(CloudSessionFatalErrors.MemberQuittedAfterActivation);

        // Act
        _cloudSessionData.ResetSession();

        // Assert
        _cloudSessionData.IsSessionActivated.Should().BeFalse();
        _cloudSessionData.CloudSessionFatalError.Should().BeNull();
    }*/

    [Test]
    public void GetCloudSession_ShouldReturnCloudSessionWithCorrectProperties()
    {
        // Arrange
        _cloudSessionData.SessionId = "session-123";
        _cloudSessionData.CreatorInstanceId = "creator-123";
        _cloudSessionData.LobbyId = "lobby-123";

        // Act
        var result = _cloudSessionData.GetCloudSession();

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be(_cloudSessionData.SessionId);
        result.CreatorInstanceId.Should().Be(_cloudSessionData.CreatorInstanceId);
        result.LobbyId.Should().Be(_cloudSessionData.LobbyId);
    }

    [Test]
    public void FindMember_WithExistingMember_ShouldReturnMember()
    {
        // Arrange
        var member = new SessionMemberData { ClientInstanceId = "client-123" };
        _cloudSessionData.SessionMembers.Add(member);

        // Act
        var result = _cloudSessionData.FindMember(member.ClientInstanceId);

        // Assert
        result.Should().BeSameAs(member);
    }

    [Test]
    public void FindMember_WithNonExistingMember_ShouldReturnNull()
    {
        // Act
        var result = _cloudSessionData.FindMember("non-existing");

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void FindMemberOrPreMember_WithRegularMember_ShouldReturnMember()
    {
        // Arrange
        var member = new SessionMemberData { ClientInstanceId = "client-123" };
        _cloudSessionData.SessionMembers.Add(member);

        // Act
        var result = _cloudSessionData.FindMemberOrPreMember(member.ClientInstanceId);

        // Assert
        result.Should().BeSameAs(member);
    }

    [Test]
    public void FindMemberOrPreMember_WithPreMember_ShouldReturnMember()
    {
        // Arrange
        var preMember = new SessionMemberData { ClientInstanceId = "pre-client-123" };
        _cloudSessionData.PreSessionMembers.Add(preMember);

        // Act
        var result = _cloudSessionData.FindMemberOrPreMember(preMember.ClientInstanceId);

        // Assert
        result.Should().BeSameAs(preMember);
    }

    [Test]
    public void FindMemberOrPreMember_WithNonExistingMember_ShouldReturnNull()
    {
        // Act
        var result = _cloudSessionData.FindMemberOrPreMember("non-existing");

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void FindMemberOrPreMember_WithDuplicateInBothLists_ShouldReturnRegularMember()
    {
        // Arrange
        string duplicateId = "duplicate-id";
        var regularMember = new SessionMemberData { ClientInstanceId = duplicateId };
        var preMember = new SessionMemberData { ClientInstanceId = duplicateId };
        
        _cloudSessionData.SessionMembers.Add(regularMember);
        _cloudSessionData.PreSessionMembers.Add(preMember);

        // Act
        var result = _cloudSessionData.FindMemberOrPreMember(duplicateId);

        // Assert
        result.Should().BeSameAs(regularMember);
    }

    [Test]
    public void FindMemberOrPreMember_WithEmptyLists_ShouldReturnNull()
    {
        // Act
        var result = _cloudSessionData.FindMemberOrPreMember("any-id");

        // Assert
        result.Should().BeNull();
    }
}