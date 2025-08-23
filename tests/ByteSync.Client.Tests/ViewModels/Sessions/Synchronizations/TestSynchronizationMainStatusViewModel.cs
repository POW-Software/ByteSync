using ByteSync.Business.Sessions;
using ByteSync.Business.Synchronizations;
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

    [SetUp]
    public void SetUp()
    {
        var sessionService = new Mock<ISessionService>();
        sessionService.SetupGet(s => s.SessionStatusObservable).Returns(Observable.Return(SessionStatus.None));

        var synchronizationService = new Mock<ISynchronizationService>();
        synchronizationService.SetupGet(s => s.SynchronizationProcessData).Returns(new SynchronizationProcessData());

        var dialogService = new Mock<IDialogService>();

        _viewModel = new SynchronizationMainStatusViewModel(sessionService.Object, synchronizationService.Object, dialogService.Object);
    }

    [Test]
    public void Test_Construction()
    {
        ClassicAssert.IsNotNull(_viewModel.AbortSynchronizationCommand);
    }
}
