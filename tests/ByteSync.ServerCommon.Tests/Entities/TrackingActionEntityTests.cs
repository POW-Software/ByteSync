using ByteSync.Common.Business.Actions;
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
            TargetClientInstanceAndNodeIds =
            [
                new() { ClientInstanceId = "target1", NodeId = "node1" },
                new() { ClientInstanceId = "target2", NodeId = "node2" }
            ],
            SuccessTargetClientInstanceAndNodeIds = [new() { ClientInstanceId = "target1", NodeId = "node1" }],
            ErrorTargetClientInstanceAndNodeIds = [new() { ClientInstanceId = "target2", NodeId = "node2" }]
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
            TargetClientInstanceAndNodeIds =
            [
                new() { ClientInstanceId = "target1", NodeId = "node1" },
                new() { ClientInstanceId = "target2", NodeId = "node2" },
                new() { ClientInstanceId = "target3", NodeId = "node3" }
            ],
            SuccessTargetClientInstanceAndNodeIds = [new() { ClientInstanceId = "target1", NodeId = "node1" }],
            ErrorTargetClientInstanceAndNodeIds = [new() { ClientInstanceId = "target2", NodeId = "node2" }]
        };

        // Act
        var result = entity.IsFinished;

        // Assert
        Assert.That(result, Is.False);
    }
}