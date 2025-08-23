using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Sessions;
using ByteSync.Business.Synchronizations;
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

namespace ByteSync.Tests.ViewModels.Sessions.Synchronizations;

[TestFixture]
public class TestSynchronizationStatisticsViewModel : AbstractTester
{
    private SynchronizationStatisticsViewModel _viewModel;

    [SetUp]
    public void SetUp()
    {
        var synchronizationService = new Mock<ISynchronizationService>();
        synchronizationService.SetupGet(s => s.SynchronizationProcessData).Returns(new SynchronizationProcessData());

        var sessionService = new Mock<ISessionService>();
        sessionService.SetupGet(s => s.IsCloudSession).Returns(false);
        sessionService.SetupGet(s => s.SessionId).Returns("id");

        var sharedActionsCache = new SourceCache<SharedActionsGroup, string>(s => s.ActionsGroupId);
        var sharedActionsRepository = new Mock<ISharedActionsGroupRepository>();
        sharedActionsRepository.SetupGet(r => r.ObservableCache).Returns(sharedActionsCache);

        var timeTrackingCache = new Mock<ITimeTrackingCache>();

        _viewModel = new SynchronizationStatisticsViewModel(synchronizationService.Object, sessionService.Object,
            sharedActionsRepository.Object, timeTrackingCache.Object);
    }

    [Test]
    public void Test_Construction()
    {
        ClassicAssert.IsNotNull(_viewModel.EstimatedEndDateTimeLabel);
    }
}
