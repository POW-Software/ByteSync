using ByteSync.Common.Business.Sessions.Cloud;
using NUnit.Framework;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace TestingCommon.Business.Sessions.Cloud;

[TestFixture]
public class CloudSessionTests
{
    [Test]
    public void Constructor_WithNoParameters_SetsVersionNumberToZero()
    {
        // Arrange
        var cloudSession = new CloudSession();

        // Act

        // Assert
        Assert.AreEqual(0, cloudSession.VersionNumber);
    }

    [Test]
    public void Constructor_WithParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        var sessionId = "testSessionId";
        var creatorInstanceId = "creatorInstanceId";

        // Act
        var cloudSession = new CloudSession(sessionId, creatorInstanceId);

        // Assert
        Assert.AreEqual(sessionId, cloudSession.SessionId);
        Assert.AreEqual(creatorInstanceId, cloudSession.CreatorInstanceId);
    }

    [Test]
    public void Equals_WithEqualObjects_ReturnsTrue()
    {
        // Arrange
        var sessionId = "testSessionId";
        var creatorInstanceId = "creatorInstanceId";
        var cloudSession1 = new CloudSession(sessionId, creatorInstanceId);
        var cloudSession2 = new CloudSession(sessionId, creatorInstanceId);

        // Act
        var result = cloudSession1.Equals(cloudSession2);

        // Assert
        Assert.IsTrue(result);
    }

    [Test]
    public void Equals_WithDifferentObjects_ReturnsFalse()
    {
        // Arrange
        var sessionId1 = "testSessionId1";
        var creatorInstanceId1 = "creatorInstanceId1";
        var cloudSession1 = new CloudSession(sessionId1, creatorInstanceId1);

        var sessionId2 = "testSessionId2";
        var creatorInstanceId2 = "creatorInstanceId2";
        var cloudSession2 = new CloudSession(sessionId2, creatorInstanceId2);

        // Act
        var result = cloudSession1.Equals(cloudSession2);

        // Assert
        Assert.IsFalse(result);
    }

    [Test]
    public void IncrementVersionNumber_WhenCalled_IncreasesVersionNumberByOne()
    {
        // Arrange
        var cloudSession = new CloudSession();
        var initialVersionNumber = cloudSession.VersionNumber;

        // Act
        cloudSession.IncrementVersionNumber();

        // Assert
        Assert.AreEqual(initialVersionNumber + 1, cloudSession.VersionNumber);
    }
}