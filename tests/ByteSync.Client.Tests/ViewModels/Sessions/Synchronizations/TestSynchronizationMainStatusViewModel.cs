using ByteSync.Business;
using ByteSync.Business.Sessions;
using ByteSync.Business.Synchronizations;
using ByteSync.Common.Business.Synchronizations;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.TestsCommon;
using ByteSync.ViewModels.Misc;
using ByteSync.ViewModels.Sessions.Synchronizations;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace ByteSync.Tests.ViewModels.Sessions.Synchronizations;

[TestFixture]
public class TestSynchronizationMainStatusViewModel : AbstractTester
{
    private SynchronizationMainStatusViewModel _viewModel;
    private SynchronizationProcessData _processData;
    private Mock<ISynchronizationService> _synchronizationService;
    private Mock<IDialogService> _dialogService;

    [SetUp]
    public void SetUp()
    {
        var sessionService = new Mock<ISessionService>();
        sessionService.SetupGet(s => s.SessionStatusObservable).Returns(Observable.Return(SessionStatus.None));

        _synchronizationService = new Mock<ISynchronizationService>();
        _processData = new SynchronizationProcessData();
        _synchronizationService.SetupGet(s => s.SynchronizationProcessData).Returns(_processData);

        _dialogService = new Mock<IDialogService>();

        _viewModel = new SynchronizationMainStatusViewModel(sessionService.Object, _synchronizationService.Object, _dialogService.Object);
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

    [Test]
    public void OnSynchronizationAbortRequested_ShouldUpdateStatus()
    {
        using var _ = _viewModel.Activator.Activate();

        _processData.SynchronizationStart.OnNext(new Synchronization { Started = DateTimeOffset.Now });
        _processData.SynchronizationAbortRequest.OnNext(new SynchronizationAbortRequest());

        ClassicAssert.AreEqual("Synchronization abort requested", _viewModel.MainStatus);
    }

    [Test]
    public void OnSynchronizationEnded_ShouldShowErrorStatus()
    {
        using var _ = _viewModel.Activator.Activate();

        _processData.SynchronizationStart.OnNext(new Synchronization { Started = DateTimeOffset.Now });
        _processData.SynchronizationEnd.OnNext(new SynchronizationEnd
        {
            FinishedOn = DateTimeOffset.Now,
            Status = SynchronizationEndStatuses.Error
        });

        ClassicAssert.AreEqual("Error during synchronization", _viewModel.MainStatus);
        ClassicAssert.AreEqual("SolidXCircle", _viewModel.MainIcon);
    }

    [Test]
    public void OnSynchronizationEnded_ShouldShowDoneStatus()
    {
        using var _ = _viewModel.Activator.Activate();

        _processData.SynchronizationStart.OnNext(new Synchronization { Started = DateTimeOffset.Now });
        _processData.SynchronizationEnd.OnNext(new SynchronizationEnd
        {
            FinishedOn = DateTimeOffset.Now,
            Status = SynchronizationEndStatuses.Regular
        });

        ClassicAssert.AreEqual("Synchronization done!", _viewModel.MainStatus);
        ClassicAssert.AreEqual("SolidCheckCircle", _viewModel.MainIcon);
    }

    [Test]
    public async Task AbortSynchronizationCommand_ShouldAbort_WhenConfirmed()
    {
        var messageBox = new MessageBoxViewModel();
        _dialogService.Setup(d => d.CreateMessageBoxViewModel(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>())).Returns(messageBox);
        _dialogService.Setup(d => d.ShowMessageBoxAsync(messageBox)).ReturnsAsync(MessageBoxResult.Yes);

        await _viewModel.AbortSynchronizationCommand.Execute();

        _synchronizationService.Verify(s => s.AbortSynchronization(), Times.Once);
    }

    [Test]
    public async Task AbortSynchronizationCommand_ShouldNotAbort_WhenCancelled()
    {
        var messageBox = new MessageBoxViewModel();
        _dialogService.Setup(d => d.CreateMessageBoxViewModel(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>())).Returns(messageBox);
        _dialogService.Setup(d => d.ShowMessageBoxAsync(messageBox)).ReturnsAsync(MessageBoxResult.No);

        await _viewModel.AbortSynchronizationCommand.Execute();

        _synchronizationService.Verify(s => s.AbortSynchronization(), Times.Never);
    }
}
