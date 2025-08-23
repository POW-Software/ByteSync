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
using FluentAssertions;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;

namespace ByteSync.Tests.ViewModels.Sessions.Synchronizations;

[TestFixture]
public class TestSynchronizationMainStatusViewModel : AbstractTester
{
    private SynchronizationMainStatusViewModel _viewModel = null!;
    private SynchronizationProcessData _processData = null!;
    private Mock<ISynchronizationService> _synchronizationService = null!;
    private Mock<IDialogService> _dialogService = null!;
    private Mock<ILogger<SynchronizationMainStatusViewModel>> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        var sessionService = new Mock<ISessionService>();
        sessionService.SetupGet(s => s.SessionStatusObservable).Returns(Observable.Return(SessionStatus.None));

        _synchronizationService = new Mock<ISynchronizationService>();
        _processData = new SynchronizationProcessData();
        _synchronizationService.SetupGet(s => s.SynchronizationProcessData).Returns(_processData);

        _dialogService = new Mock<IDialogService>();
        _logger = new Mock<ILogger<SynchronizationMainStatusViewModel>>();

        _viewModel = new SynchronizationMainStatusViewModel(sessionService.Object, _synchronizationService.Object, 
            _dialogService.Object, _logger.Object);
    }

    [Test]
    public void Test_Construction()
    {
        _viewModel.AbortSynchronizationCommand.Should().NotBeNull();
    }

    [Test]
    public void OnSynchronizationStarted_ShouldUpdateState()
    {
        using var _ = _viewModel.Activator.Activate();

        _processData.SynchronizationStart.OnNext(new Synchronization { Started = DateTimeOffset.Now });

        _viewModel.IsSynchronizationRunning.Should().BeTrue();
        _viewModel.IsMainProgressRingVisible.Should().BeTrue();
        _viewModel.IsMainCheckVisible.Should().BeFalse();
        _viewModel.MainStatus.Should().Be("Synchronization running");
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

        _viewModel.IsSynchronizationRunning.Should().BeFalse();
        _viewModel.MainStatus.Should().Be("Synchronization aborted");
        _viewModel.MainIcon.Should().Be("SolidXCircle");
        _viewModel.IsMainProgressRingVisible.Should().BeFalse();
        _viewModel.IsMainCheckVisible.Should().BeTrue();
    }

    [Test]
    public void OnSynchronizationAbortRequested_ShouldUpdateStatus()
    {
        using var _ = _viewModel.Activator.Activate();

        _processData.SynchronizationStart.OnNext(new Synchronization { Started = DateTimeOffset.Now });
        _processData.SynchronizationAbortRequest.OnNext(new SynchronizationAbortRequest());

        _viewModel.MainStatus.Should().Be("Synchronization abort requested");
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

        _viewModel.MainStatus.Should().Be("Error during synchronization");
        _viewModel.MainIcon.Should().Be("SolidXCircle");
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

        _viewModel.MainStatus.Should().Be("Synchronization done!");
        _viewModel.MainIcon.Should().Be("SolidCheckCircle");
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
