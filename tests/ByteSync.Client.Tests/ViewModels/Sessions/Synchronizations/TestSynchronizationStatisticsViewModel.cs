using System.Reactive.Subjects;
using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Misc;
using ByteSync.Business.Synchronizations;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Controls.TimeTracking;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.TestsCommon;
using ByteSync.ViewModels.Sessions.Synchronizations;
using DynamicData;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.ViewModels.Sessions.Synchronizations;

[TestFixture]
public class TestSynchronizationStatisticsViewModel : AbstractTester
{
    private SynchronizationStatisticsViewModel _viewModel = null!;
    private SynchronizationProcessData _processData = null!;
    private BehaviorSubject<TimeTrack> _timeTrackSubject = null!;

    [SetUp]
    public void SetUp()
    {
        var synchronizationService = new Mock<ISynchronizationService>();
        _processData = new SynchronizationProcessData();
        synchronizationService.SetupGet(s => s.SynchronizationProcessData).Returns(_processData);

        var sessionService = new Mock<ISessionService>();
        sessionService.SetupGet(s => s.IsCloudSession).Returns(false);
        sessionService.SetupGet(s => s.SessionId).Returns("id");

        var sharedActionsCache = new SourceCache<SharedActionsGroup, string>(s => s.ActionsGroupId);
        var sharedActionsRepository = new Mock<ISharedActionsGroupRepository>();
        sharedActionsRepository.SetupGet(r => r.ObservableCache).Returns(sharedActionsCache);

        var timeTrackingCache = new Mock<ITimeTrackingCache>();
        var track = new TimeTrack();
        track.Reset(DateTime.Now);
        _timeTrackSubject = new BehaviorSubject<TimeTrack>(track);
        var timeTrackingComputer = new Mock<ITimeTrackingComputer>();
        timeTrackingComputer.Setup(t => t.RemainingTime).Returns(_timeTrackSubject);
        timeTrackingCache.Setup(c => c.GetTimeTrackingComputer("id", TimeTrackingComputerType.Synchronization))
            .ReturnsAsync(timeTrackingComputer.Object);

        _viewModel = new SynchronizationStatisticsViewModel(synchronizationService.Object, sessionService.Object,
            sharedActionsRepository.Object, timeTrackingCache.Object);
    }

    [Test]
    public void Test_Construction()
    {
        _viewModel.EstimatedEndDateTimeLabel.Should().NotBeNull();
    }

    [Test]
    public void OnSynchronizationStarted_ShouldInitializeCounters()
    {
        using var _ = _viewModel.Activator.Activate();

        var start = new Synchronization { Started = DateTimeOffset.Now };
        _processData.SynchronizationStart.OnNext(start);

        _viewModel.StartDateTime.Should().Be(start.Started.LocalDateTime);
        _viewModel.HandledActions.Should().Be(0);
        _viewModel.Errors.Should().Be(0);
        _viewModel.ElapsedTime.Should().Be(TimeSpan.Zero);
    }

    [Test]
    public void OnSynchronizationDataTransmitted_ShouldSetTreatableActions()
    {
        using var _ = _viewModel.Activator.Activate();

        _processData.TotalActionsToProcess = 42;
        _processData.SynchronizationDataTransmitted.OnNext(true);

        _viewModel.TreatableActions.Should().Be(42);
    }

    [Test]
    public void OnSynchronizationProgressChanged_ShouldUpdateValues()
    {
        using var _ = _viewModel.Activator.Activate();

        _processData.SynchronizationStart.OnNext(new Synchronization { Started = DateTimeOffset.Now });

        var progress = new SynchronizationProgress
        {
            SessionId = "id",
            Version = 1,
            FinishedActionsCount = 5,
            ErrorActionsCount = 1,
            ProcessedVolume = 10,
            ExchangedVolume = 20
        };
        _processData.SynchronizationProgress.OnNext(progress);

        // Allow reactive pipeline (ObserveOn MainThread) to process
        SpinWait.SpinUntil(() => _viewModel.HandledActions == 5, TimeSpan.FromSeconds(1));
        _viewModel.HandledActions.Should().Be(5);
        _viewModel.Errors.Should().Be(1);
        _viewModel.ProcessedVolume.Should().Be(10);
        _viewModel.ExchangedVolume.Should().Be(20);
    }

    [Test]
    public void OnSynchronizationEnded_ShouldSetFinalValues()
    {
        using var _ = _viewModel.Activator.Activate();

        var progress = new SynchronizationProgress { Version = 1, FinishedActionsCount = 3, ErrorActionsCount = 1 };
        _processData.SynchronizationProgress.OnNext(progress);
        _processData.SynchronizationEnd.OnNext(new SynchronizationEnd
        {
            FinishedOn = DateTimeOffset.Now,
            Status = SynchronizationEndStatuses.Regular
        });

        _viewModel.EstimatedEndDateTimeLabel.Should().Be("End:");
        _viewModel.HandledActions.Should().Be(3);
        _viewModel.Errors.Should().Be(1);
    }
}