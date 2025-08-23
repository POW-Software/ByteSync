using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Sessions;
using ByteSync.Business.Synchronizations;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Business.Misc;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Controls.TimeTracking;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.TestsCommon;
using ByteSync.ViewModels.Sessions.Synchronizations;
using DynamicData;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Reactive.Subjects;

namespace ByteSync.Tests.ViewModels.Sessions.Synchronizations;

[TestFixture]
public class TestSynchronizationStatisticsViewModel : AbstractTester
{
    private SynchronizationStatisticsViewModel _viewModel;
    private SynchronizationProcessData _processData;
    private BehaviorSubject<TimeTrack> _timeTrackSubject;

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
        _timeTrackSubject = new BehaviorSubject<TimeTrack>(new TimeTrack());
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
        ClassicAssert.IsNotNull(_viewModel.EstimatedEndDateTimeLabel);
    }

    [Test]
    public void OnSynchronizationStarted_ShouldInitializeCounters()
    {
        using var _ = _viewModel.Activator.Activate();

        var start = new Synchronization { Started = DateTimeOffset.Now };
        _processData.SynchronizationStart.OnNext(start);

        ClassicAssert.AreEqual(start.Started.LocalDateTime, _viewModel.StartDateTime);
        ClassicAssert.AreEqual(0, _viewModel.HandledActions);
        ClassicAssert.AreEqual(0, _viewModel.Errors);
        ClassicAssert.AreEqual(TimeSpan.Zero, _viewModel.ElapsedTime);
    }

    [Test]
    public void OnSynchronizationDataTransmitted_ShouldSetTreatableActions()
    {
        using var _ = _viewModel.Activator.Activate();

        _processData.TotalActionsToProcess = 42;
        _processData.SynchronizationDataTransmitted.OnNext(true);

        ClassicAssert.AreEqual(42, _viewModel.TreatableActions);
    }

    [Test]
    public void OnSynchronizationProgressChanged_ShouldUpdateValues()
    {
        using var _ = _viewModel.Activator.Activate();

        _processData.SynchronizationStart.OnNext(new Synchronization { Started = DateTimeOffset.Now });

        var progress = new SynchronizationProgress
        {
            Version = 1,
            FinishedActionsCount = 5,
            ErrorActionsCount = 1,
            ProcessedVolume = 10,
            ExchangedVolume = 20
        };
        _processData.SynchronizationProgress.OnNext(progress);

        ClassicAssert.AreEqual(5, _viewModel.HandledActions);
        ClassicAssert.AreEqual(1, _viewModel.Errors);
        ClassicAssert.AreEqual(10, _viewModel.ProcessedVolume);
        ClassicAssert.AreEqual(20, _viewModel.ExchangedVolume);
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

        ClassicAssert.AreEqual("End:", _viewModel.EstimatedEndDateTimeLabel);
        ClassicAssert.AreEqual(3, _viewModel.HandledActions);
        ClassicAssert.AreEqual(1, _viewModel.Errors);
    }
}
