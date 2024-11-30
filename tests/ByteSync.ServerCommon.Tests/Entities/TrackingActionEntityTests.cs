using ByteSync.ServerCommon.Entities;

namespace ByteSync.ServerCommon.Tests.Entities;

public class TrackingActionEntityTests
{
    [Test]
    public void IsFinished_SourceClientInstanceIdNotNullAndIsSourceSuccessFalse_ReturnsTrue()
    {
        // Arrange
        var entity = new TrackingActionEntity
        {
            SourceClientInstanceId = "source",
            IsSourceSuccess = false
        };

        // Act
        var result = entity.IsFinished;

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsFinished_SourceClientInstanceIdNotNullAndIsSourceSuccessNull_ReturnsFalse()
    {
        // Arrange
        var entity = new TrackingActionEntity
        {
            SourceClientInstanceId = "source",
            IsSourceSuccess = null
        };

        // Act
        var result = entity.IsFinished;

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsFinished_AllTargetsProcessed_ReturnsTrue()
    {
        // Arrange
        var entity = new TrackingActionEntity
        {
            TargetClientInstanceIds = new HashSet<string> { "target1", "target2" },
            SuccessTargetClientInstanceIds = new HashSet<string> { "target1" },
            ErrorTargetClientInstanceIds = new HashSet<string> { "target2" }
        };

        // Act
        var result = entity.IsFinished;

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsFinished_NotAllTargetsProcessed_ReturnsFalse()
    {
        // Arrange
        var entity = new TrackingActionEntity
        {
            TargetClientInstanceIds = new HashSet<string> { "target1", "target2", "target3" },
            SuccessTargetClientInstanceIds = new HashSet<string> { "target1" },
            ErrorTargetClientInstanceIds = new HashSet<string> { "target2" }
        };

        // Act
        var result = entity.IsFinished;

        // Assert
        Assert.That(result, Is.False);
    }
}