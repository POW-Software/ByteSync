using ByteSync.Services.Communications.Transfers.Downloading;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Communications.Transfers.Downloading;

public class DownloadPartsCoordinatorTests
{
    [Test]
    public void Constructor_InitializesProperties()
    {
        var coordinator = new DownloadPartsCoordinator();
        coordinator.DownloadPartsInfo.Should().NotBeNull();
        coordinator.DownloadQueue.Should().NotBeNull();
        coordinator.MergeChannel.Should().NotBeNull();
        coordinator.AllPartsQueued.Should().BeFalse();
    }
    
    [Test]
    public void AddAvailablePart_QueuesPartsAndCompletesWhenAllKnown()
    {
        var coordinator = new DownloadPartsCoordinator();
        coordinator.SetAllPartsKnown(2);
        coordinator.AddAvailablePart(1);
        coordinator.AddAvailablePart(2);
        coordinator.DownloadPartsInfo.AvailableParts.Should().Contain(1);
        coordinator.DownloadPartsInfo.AvailableParts.Should().Contain(2);
        coordinator.AllPartsQueued.Should().BeTrue();
    }
    
    [Test]
    public void SetAllPartsKnown_CompletesQueueWhenAllPartsAdded()
    {
        var coordinator = new DownloadPartsCoordinator();
        coordinator.AddAvailablePart(1);
        coordinator.AddAvailablePart(2);
        coordinator.SetAllPartsKnown(2);
        coordinator.AllPartsQueued.Should().BeTrue();
    }
    
    [Test]
    public async Task AddAvailablePartAsync_QueuesPartsAndCompletesWhenAllKnown()
    {
        var coordinator = new DownloadPartsCoordinator();
        await coordinator.SetAllPartsKnownAsync(2);
        await coordinator.AddAvailablePartAsync(1);
        await coordinator.AddAvailablePartAsync(2);
        coordinator.DownloadPartsInfo.AvailableParts.Should().Contain(1);
        coordinator.DownloadPartsInfo.AvailableParts.Should().Contain(2);
        coordinator.AllPartsQueued.Should().BeTrue();
    }
    
    [Test]
    public async Task SetAllPartsKnownAsync_CompletesQueueWhenAllPartsAdded()
    {
        var coordinator = new DownloadPartsCoordinator();
        await coordinator.AddAvailablePartAsync(1);
        await coordinator.AddAvailablePartAsync(2);
        await coordinator.SetAllPartsKnownAsync(2);
        coordinator.AllPartsQueued.Should().BeTrue();
    }
    
    [Test]
    public async Task AddAvailablePartAsync_DoesNotCauseInfiniteRecursion()
    {
        var coordinator = new DownloadPartsCoordinator();
        await coordinator.SetAllPartsKnownAsync(1);
        
        await coordinator.AddAvailablePartAsync(1);
        
        coordinator.DownloadPartsInfo.AvailableParts.Should().Contain(1);
        coordinator.AllPartsQueued.Should().BeTrue();
    }
}