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
            TargetClientInstanceAndNodeIds = new HashSet<string> { "target1_node1", "target2_node2" },
            SuccessTargetClientInstanceAndNodeIds = new HashSet<string> { "target1_node1" },
            ErrorTargetClientInstanceAndNodeIds = new HashSet<string> { "target2_node2" }
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
            TargetClientInstanceAndNodeIds = new HashSet<string> { "target1_node1", "target2_node2", "target3_node3" },
            SuccessTargetClientInstanceAndNodeIds = new HashSet<string> { "target1_node1" },
            ErrorTargetClientInstanceAndNodeIds = new HashSet<string> { "target2_node2" }
        };

        // Act
        var result = entity.IsFinished;

        // Assert
        Assert.That(result, Is.False);
    }
}