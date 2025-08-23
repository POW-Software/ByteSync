using ByteSync.Business.Actions.Local;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Business.Sessions.RunSessionInfos;
using ByteSync.Common.Business.Sessions;
using ByteSync.Business.Synchronizations;
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
using NUnit.Framework.Legacy;
using System.Reactive.Linq;

namespace ByteSync.Tests.ViewModels.Sessions.Synchronizations;

[TestFixture]
public class TestSynchronizationBeforeStartViewModel : AbstractTester
{
    private SynchronizationBeforeStartViewModel _viewModel;

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

        var localizationService = new Mock<ILocalizationService>();
        var synchronizationStarter = new Mock<ISynchronizationStarter>();
        var errorViewModel = new ErrorViewModel(localizationService.Object);

        _viewModel = new SynchronizationBeforeStartViewModel(sessionService.Object, localizationService.Object,
            synchronizationService.Object, synchronizationStarter.Object, atomicRepository.Object,
            sessionMemberRepository.Object, errorViewModel);
    }

    [Test]
    public void Test_Construction()
    {
        ClassicAssert.IsNotNull(_viewModel.StartSynchronizationCommand);
    }
}
