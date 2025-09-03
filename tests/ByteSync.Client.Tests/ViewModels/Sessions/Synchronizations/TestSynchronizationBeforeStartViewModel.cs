using ByteSync.Business.Actions.Local;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Business.Sessions.RunSessionInfos;
using ByteSync.Business.Synchronizations;
using ByteSync.Common.Business.Sessions;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.TestsCommon;
using ByteSync.ViewModels.Misc;
using ByteSync.ViewModels.Sessions.Synchronizations;
using DynamicData;
using Moq;
using NUnit.Framework;
using FluentAssertions;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;

namespace ByteSync.Tests.ViewModels.Sessions.Synchronizations;

[TestFixture]
public class TestSynchronizationBeforeStartViewModel : AbstractTester
{
    private SynchronizationBeforeStartViewModel _viewModel = null!;
    private Mock<ISynchronizationStarter> _synchronizationStarter = null!;
    private Mock<ILocalizationService> _localizationService = null!;
    private Mock<ILogger<SynchronizationBeforeStartViewModel>> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        var sessionService = new Mock<ISessionService>();
        sessionService.SetupGet(s => s.SessionObservable).Returns(Observable.Return<AbstractSession?>(null));
        sessionService.SetupGet(s => s.SessionStatusObservable).Returns(Observable.Return(SessionStatus.None));
        sessionService.SetupGet(s => s.IsCloudSession).Returns(false);
        sessionService.SetupGet(s => s.CurrentRunSessionProfileInfo).Returns((AbstractRunSessionProfileInfo?)null);

        var atomicCache = new SourceCache<AtomicAction, string>(a => a.AtomicActionId);
        var atomicRepository = new Mock<IAtomicActionRepository>();
        atomicRepository.SetupGet(r => r.ObservableCache).Returns(atomicCache);

        var sessionMemberRepository = new Mock<ISessionMemberRepository>();
        sessionMemberRepository.SetupGet(r => r.IsCurrentUserFirstSessionMemberObservable).Returns(Observable.Return(true));
        sessionMemberRepository.SetupGet(r => r.IsCurrentUserFirstSessionMemberCurrentValue).Returns(true);
        sessionMemberRepository.SetupGet(r => r.Elements).Returns(new List<SessionMember>());

        var synchronizationService = new Mock<ISynchronizationService>();
        synchronizationService.SetupGet(s => s.SynchronizationProcessData).Returns(new SynchronizationProcessData());

        _localizationService = new Mock<ILocalizationService>();
        _localizationService.Setup(l => l["ErrorView_ErrorMessage"]).Returns("Error {0}");

        _logger = new Mock<ILogger<SynchronizationBeforeStartViewModel>>();
        
        _synchronizationStarter = new Mock<ISynchronizationStarter>();
        var errorViewModel = new ErrorViewModel(_localizationService.Object);

        _viewModel = new SynchronizationBeforeStartViewModel(sessionService.Object, _localizationService.Object,
            synchronizationService.Object, _synchronizationStarter.Object, atomicRepository.Object,
            sessionMemberRepository.Object, _logger.Object, errorViewModel);
    }

    [Test]
    public void Test_Construction()
    {
        _viewModel.StartSynchronizationCommand.Should().NotBeNull();
    }

    [Test]
    public void ShowStartSynchronizationObservable_ShouldReflectConditions()
    {
        using var _ = _viewModel.Activator.Activate();

        _viewModel.IsSynchronizationRunning = false;
        _viewModel.IsCloudSession = false;
        _viewModel.IsSessionCreatedByMe = true;
        _viewModel.IsProfileSessionSynchronization = false;
        _viewModel.HasSessionBeenRestarted = false;

        _viewModel.ShowStartSynchronizationObservable.Should().BeTrue();

        _viewModel.IsSynchronizationRunning = true;
        _viewModel.ShowStartSynchronizationObservable.Should().BeFalse();
    }

    [Test]
    public void ShowWaitingForSynchronizationStartObservable_ShouldBeTrue_ForProfileSession()
    {
        using var _ = _viewModel.Activator.Activate();

        _viewModel.IsSynchronizationRunning = false;
        _viewModel.IsCloudSession = false;
        _viewModel.IsSessionCreatedByMe = true;
        _viewModel.IsProfileSessionSynchronization = true;
        _viewModel.HasSessionBeenRestarted = false;

        _viewModel.ShowWaitingForSynchronizationStartObservable.Should().BeTrue();
    }

    [Test]
    public void ShowStartSynchronizationObservable_ShouldBeFalse_ForProfileSessionWithoutRestart()
    {
        using var _ = _viewModel.Activator.Activate();

        _viewModel.IsSynchronizationRunning = false;
        _viewModel.IsCloudSession = false;
        _viewModel.IsSessionCreatedByMe = true;
        _viewModel.IsProfileSessionSynchronization = true;
        _viewModel.HasSessionBeenRestarted = false;

        _viewModel.ShowStartSynchronizationObservable.Should().BeFalse();
    }

    [Test]
    public void ShowStartSynchronizationObservable_ShouldBeTrue_ForRestartedProfileSession()
    {
        using var _ = _viewModel.Activator.Activate();

        _viewModel.IsSynchronizationRunning = false;
        _viewModel.IsCloudSession = false;
        _viewModel.IsSessionCreatedByMe = true;
        _viewModel.IsProfileSessionSynchronization = true;
        _viewModel.HasSessionBeenRestarted = true;

        _viewModel.ShowStartSynchronizationObservable.Should().BeTrue();
    }

    [Test]
    public void ShowWaitingForSynchronizationStartObservable_ShouldBeTrue_ForCloudSessionNotCreatedByMe()
    {
        using var _ = _viewModel.Activator.Activate();

        _viewModel.IsSynchronizationRunning = false;
        _viewModel.IsCloudSession = true;
        _viewModel.IsSessionCreatedByMe = false;
        _viewModel.IsProfileSessionSynchronization = false;
        _viewModel.HasSessionBeenRestarted = false;

        _viewModel.ShowWaitingForSynchronizationStartObservable.Should().BeTrue();
    }

    [Test]
    public async Task StartSynchronizationCommand_ShouldStartProcess()
    {
        await _viewModel.StartSynchronizationCommand.Execute();
        _synchronizationStarter.Verify(s => s.StartSynchronization(true), Times.Once);
        _viewModel.StartSynchronizationError.ErrorMessage.Should().BeNull();
    }

    [Test]
    public async Task StartSynchronizationCommand_ShouldHandleException()
    {
        _synchronizationStarter.Setup(s => s.StartSynchronization(true)).ThrowsAsync(new InvalidOperationException());

        await _viewModel.StartSynchronizationCommand.Execute();

        _viewModel.StartSynchronizationError.ErrorMessage.Should().NotBeNull();
    }
}
