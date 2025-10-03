using System.Reactive.Concurrency;
using ByteSync.Business.Synchronizations;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Services.TimeTracking;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.TimeTracking;

[TestFixture]
public class SynchronizationDataTrackingStrategyTests
{
    private static (SynchronizationDataTrackingStrategy Sut, SynchronizationProcessData Data, IScheduler Scheduler) CreateSut(
        IScheduler? scheduler = null)
    {
        var data = new SynchronizationProcessData();
        
        var syncServiceMock = new Mock<ISynchronizationService>();
        syncServiceMock.SetupGet(x => x.SynchronizationProcessData).Returns(data);
        
        var usedScheduler = scheduler ?? new TestScheduler();
        var sut = new SynchronizationDataTrackingStrategy(syncServiceMock.Object, usedScheduler);
        
        return (sut, data, usedScheduler);
    }
    
    [Test]
    public void GetDataObservable_InitialEmission_IsZeroTuple()
    {
        // Arrange
        var (sut, data, _) = CreateSut();
        var evt = new AutoResetEvent(false);
        (long Identified, long Processed)? captured = null;
        Exception? error = null;
        using var sub = sut.GetDataObservable().Subscribe(
            v =>
            {
                captured = v;
                evt.Set();
            },
            ex =>
            {
                error = ex;
                evt.Set();
            });
        
        // Act: push after subscription
        data.SynchronizationProgress.OnNext(new SynchronizationProgress());
        data.SynchronizationMainStatus.OnNext(SynchronizationProcessStatuses.Pending);
        evt.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
        error.Should().BeNull();
        var first = captured!.Value;
        
        // Assert
        first.Should().Be((0L, 0L));
    }
    
    [Test]
    public void GetDataObservable_RunningWithZeroProgress_EmitsImmediately()
    {
        // Arrange
        var (sut, data, _) = CreateSut();
        var results = new List<(long Identified, long Processed)>();
        using var sub = sut.GetDataObservable().Subscribe(results.Add);
        
        // Act: push progress and status after subscription, in that order
        data.SynchronizationProgress.OnNext(new SynchronizationProgress
        {
            SynchronizedVolume = 0,
            TotalVolumeToProcess = 1234
        });
        data.SynchronizationMainStatus.OnNext(SynchronizationProcessStatuses.Running);
        
        // Assert: should include the emitted data (non-skippable path)
        results.Should().Contain((1234L, 0L));
    }
    
    [Test]
    public void GetDataObservable_RunningWithNonZeroProgress_IsSampled()
    {
        // Arrange
        var (sut, data, scheduler) = CreateSut();
        
        var results = new List<(long Identified, long Processed)>();
        using var subscription = sut.GetDataObservable().Subscribe(x => results.Add(x));
        
        // Act: Push a few non-zero updates in quick succession (< 0.5s)
        data.SynchronizationMainStatus.OnNext(SynchronizationProcessStatuses.Running);
        data.SynchronizationProgress.OnNext(new SynchronizationProgress { TotalVolumeToProcess = 1000, SynchronizedVolume = 10 });
        (scheduler as TestScheduler)!.AdvanceBy(TimeSpan.FromMilliseconds(100).Ticks);
        data.SynchronizationProgress.OnNext(new SynchronizationProgress { TotalVolumeToProcess = 1000, SynchronizedVolume = 20 });
        (scheduler as TestScheduler)!.AdvanceBy(TimeSpan.FromMilliseconds(100).Ticks);
        data.SynchronizationProgress.OnNext(new SynchronizationProgress { TotalVolumeToProcess = 1000, SynchronizedVolume = 30 });
        
        // Advance virtual time beyond 0.5s to trigger Sample(0.5s)
        (scheduler as TestScheduler)!.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);
        
        // Assert: we should have at least one sampled emission with the latest values
        results.Should().Contain((1000L, 30L));
    }
}