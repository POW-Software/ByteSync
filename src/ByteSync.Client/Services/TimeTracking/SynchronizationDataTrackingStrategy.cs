using System.Reactive.Concurrency;
using System.Reactive.Linq;
using ByteSync.Business.Synchronizations;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Controls.TimeTracking;

namespace ByteSync.Services.TimeTracking;

public class SynchronizationDataTrackingStrategy : IDataTrackingStrategy
{
    private readonly ISynchronizationService _synchronizationService;
    private readonly IScheduler _scheduler;

    public SynchronizationDataTrackingStrategy(ISynchronizationService synchronizationService)
    {
        _synchronizationService = synchronizationService;
        _scheduler = Scheduler.Default;
    }

    public SynchronizationDataTrackingStrategy(ISynchronizationService synchronizationService, IScheduler scheduler)
    {
        _synchronizationService = synchronizationService;
        _scheduler = scheduler;
    }

    public IObservable<(long IdentifiedSize, long ProcessedSize)> GetDataObservable()
    {
        var synchronizationProcessData = _synchronizationService.SynchronizationProcessData;

        var source = synchronizationProcessData.SynchronizationProgress.CombineLatest(synchronizationProcessData.SynchronizationMainStatus);

        Func<(SynchronizationProgress?, SynchronizationProcessStatuses), bool> canSkip =
            tuple =>
            {
                var synchronizationProgress = tuple.Item1;
                var synchronizationMainStatus = tuple.Item2;

                return (synchronizationProgress == null || synchronizationProgress.HasNonZeroProperty()) &&
                       synchronizationMainStatus.In(SynchronizationProcessStatuses.Running);
            };

        // Share the source so that it's not subscribed multiple times
        var sharedSource = source.Publish().RefCount();

        // Sample the source observable every 0.5 seconds, but only for values that can be skipped
        var sampled = sharedSource
            .Where(canSkip)
            .Sample(TimeSpan.FromSeconds(0.5), _scheduler);

        // Get the values from the shared source that can not be skipped
        var notSkipped = sharedSource
            .Where(value => !canSkip(value));

        // Merge the sampled and notSkipped sequences
        var merged = sampled.Merge(notSkipped);

        return merged.Select(tuple =>
            (tuple.Item1?.TotalVolumeToProcess ?? 0,
                tuple.Item1?.SynchronizedVolume ?? 0));
    }
}