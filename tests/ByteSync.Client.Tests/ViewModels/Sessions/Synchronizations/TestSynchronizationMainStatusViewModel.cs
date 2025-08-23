using ByteSync.Business.Sessions;
using ByteSync.Business.Synchronizations;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.TestsCommon;
using ByteSync.ViewModels.Sessions.Synchronizations;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Reactive.Linq;

namespace ByteSync.Tests.ViewModels.Sessions.Synchronizations;

[TestFixture]
public class TestSynchronizationMainStatusViewModel : AbstractTester
{
    private SynchronizationMainStatusViewModel _viewModel;
    private SynchronizationProcessData _processData;

    [SetUp]
    public void SetUp()
    {
        var sessionService = new Mock<ISessionService>();
        sessionService.SetupGet(s => s.SessionStatusObservable).Returns(Observable.Return(SessionStatus.None));

        var synchronizationService = new Mock<ISynchronizationService>();
        _processData = new SynchronizationProcessData();
        synchronizationService.SetupGet(s => s.SynchronizationProcessData).Returns(_processData);

        var dialogService = new Mock<IDialogService>();

        _viewModel = new SynchronizationMainStatusViewModel(sessionService.Object, synchronizationService.Object, dialogService.Object);
    }

    [Test]
    public void Test_Construction()
    {
        ClassicAssert.IsNotNull(_viewModel.AbortSynchronizationCommand);
    }

    [Test]
    public void OnSynchronizationStarted_ShouldUpdateState()
    {
        using var _ = _viewModel.Activator.Activate();

        _processData.SynchronizationStart.OnNext(new Synchronization { Started = DateTimeOffset.Now });

        ClassicAssert.IsTrue(_viewModel.IsSynchronizationRunning);
        ClassicAssert.IsTrue(_viewModel.IsMainProgressRingVisible);
        ClassicAssert.IsFalse(_viewModel.IsMainCheckVisible);
        ClassicAssert.AreEqual("Synchronization running", _viewModel.MainStatus);
    }

    [Test]
    public void OnSynchronizationEnded_ShouldShowFinalStatus()
    {
        using var _ = _viewModel.Activator.Activate();

        _processData.SynchronizationStart.OnNext(new Synchronization { Started = DateTimeOffset.Now });
        _processData.SynchronizationEnd.OnNext(new SynchronizationEnd
        {
            FinishedOn = DateTimeOffset.Now,
            Status = SynchronizationEndStatuses.Abortion
        });

        ClassicAssert.IsFalse(_viewModel.IsSynchronizationRunning);
        ClassicAssert.AreEqual("Synchronization aborted", _viewModel.MainStatus);
        ClassicAssert.AreEqual("SolidXCircle", _viewModel.MainIcon);
        ClassicAssert.IsFalse(_viewModel.IsMainProgressRingVisible);
        ClassicAssert.IsTrue(_viewModel.IsMainCheckVisible);
    }
}
