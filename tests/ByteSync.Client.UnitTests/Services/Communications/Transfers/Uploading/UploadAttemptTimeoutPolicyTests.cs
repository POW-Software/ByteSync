using ByteSync.Services.Communications.Transfers.Uploading;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Communications.Transfers.Uploading;

[TestFixture]
public class UploadAttemptTimeoutPolicyTests
{
    [Test]
    public void ComputeTimeoutSeconds_FirstAttempt_ShouldUseFloorForSmallSlices()
    {
        // Act
        var timeout = UploadAttemptTimeoutPolicy.ComputeTimeoutSeconds(
            500 * 1024,
            attempt: 1,
            currentChunkSizeBytes: 500 * 1024);
        
        // Assert
        timeout.Should().Be(60);
    }
    
    [Test]
    public void ComputeTimeoutSeconds_FirstAttemptForOversizedSlice_ShouldIncreaseBudgetImmediately()
    {
        // Act
        var timeout = UploadAttemptTimeoutPolicy.ComputeTimeoutSeconds(
            625 * 1024,
            attempt: 1,
            currentChunkSizeBytes: 64 * 1024);
        
        // Assert
        timeout.Should().Be(180);
    }
    
    [Test]
    public void ComputeTimeoutSeconds_RetryForCurrentChunkSizedSlice_ShouldGrowGradually()
    {
        // Act
        var timeout = UploadAttemptTimeoutPolicy.ComputeTimeoutSeconds(
            64 * 1024,
            attempt: 2,
            currentChunkSizeBytes: 64 * 1024);
        
        // Assert
        timeout.Should().Be(75);
    }

    [Test]
    public void ComputeTimeoutSeconds_RetryForOversizedSlice_ShouldKeepRetryGrowthBounded()
    {
        // Act
        var timeout = UploadAttemptTimeoutPolicy.ComputeTimeoutSeconds(
            2 * 1024 * 1024,
            attempt: 2,
            currentChunkSizeBytes: 500 * 1024);

        // Assert
        timeout.Should().Be(165);
    }
    
    [Test]
    public void ComputeTimeoutSeconds_ShouldNotExceedCeiling()
    {
        // Act
        var timeout = UploadAttemptTimeoutPolicy.ComputeTimeoutSeconds(
            16 * 1024 * 1024,
            attempt: 10,
            currentChunkSizeBytes: 64 * 1024);
        
        // Assert
        timeout.Should().Be(180);
    }

    [Test]
    public void ComputeTimeoutSeconds_ForLargeStaleSliceAtLowBandwidth_ShouldAllowMoreThanTwoMinutes()
    {
        // Act
        var timeout = UploadAttemptTimeoutPolicy.ComputeTimeoutSeconds(
            1969 * 1024,
            attempt: 1,
            currentChunkSizeBytes: 500 * 1024);

        // Assert
        timeout.Should().Be(120);
    }

    [Test]
    public void ComputeTimeoutSeconds_WithHugeSlice_ShouldNotOverflow()
    {
        // Act
        var timeout = UploadAttemptTimeoutPolicy.ComputeTimeoutSeconds(
            long.MaxValue,
            attempt: 1,
            currentChunkSizeBytes: 64 * 1024);

        // Assert
        timeout.Should().Be(180);
    }

    [Test]
    public void ComputeTimeoutSeconds_WithHugeStaleRatio_ShouldNotOverflow()
    {
        // Act
        var timeout = UploadAttemptTimeoutPolicy.ComputeTimeoutSeconds(
            long.MaxValue,
            attempt: 2,
            currentChunkSizeBytes: 1);

        // Assert
        timeout.Should().Be(180);
    }
}
