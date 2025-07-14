using System.Linq;
using NUnit.Framework;
using ByteSync.Services.Communications.Transfers;

namespace ByteSync.Client.Tests.Services.Communications.Transfers;

public class DownloadPartsCoordinatorTests
{
    [Test]
    public void Constructor_InitializesProperties()
    {
        var coordinator = new DownloadPartsCoordinator();
        Assert.That(coordinator.DownloadPartsInfo, Is.Not.Null);
        Assert.That(coordinator.DownloadQueue, Is.Not.Null);
        Assert.That(coordinator.MergeChannel, Is.Not.Null);
        Assert.That(coordinator.AllPartsQueued, Is.False);
    }

    [Test]
    public void AddAvailablePart_QueuesPartsAndCompletesWhenAllKnown()
    {
        var coordinator = new DownloadPartsCoordinator();
        coordinator.SetAllPartsKnown(2);
        coordinator.AddAvailablePart(1);
        coordinator.AddAvailablePart(2);
        Assert.That(coordinator.DownloadPartsInfo.AvailableParts.Contains(1), Is.True);
        Assert.That(coordinator.DownloadPartsInfo.AvailableParts.Contains(2), Is.True);
        Assert.That(coordinator.AllPartsQueued, Is.True);
    }

    [Test]
    public void SetAllPartsKnown_CompletesQueueWhenAllPartsAdded()
    {
        var coordinator = new DownloadPartsCoordinator();
        coordinator.AddAvailablePart(1);
        coordinator.AddAvailablePart(2);
        coordinator.SetAllPartsKnown(2);
        Assert.That(coordinator.AllPartsQueued, Is.True);
    }
} 