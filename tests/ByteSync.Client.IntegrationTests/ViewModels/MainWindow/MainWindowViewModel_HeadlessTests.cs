using System.Reactive.Linq;
using Autofac;
using Autofac.Features.Indexed;
using ByteSync.Business.Navigations;
using ByteSync.Business.Sessions;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Navigations;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Interfaces.Services.Sessions.Connecting;
using ByteSync.Services.Navigations;
using ByteSync.ViewModels;
using ByteSync.ViewModels.Announcements;
using ByteSync.ViewModels.Headers;
using ByteSync.ViewModels.Misc;
using Moq;
using NUnit.Framework;
using ReactiveUI;

namespace ByteSync.Client.IntegrationTests.ViewModels.MainWindow;

public class MainWindowViewModel_HeadlessTests : HeadlessIntegrationTest
{
    [SetUp]
    public void Setup()
    {
        var flyout = new FlyoutContainerViewModel { CanCloseCurrentFlyout = true };
        var header = new HeaderViewModel();
        var announcement = new AnnouncementViewModel();

        var panelViewModels = new Mock<IIndex<NavigationPanel, IRoutableViewModel>>();
        var dummy = new Mock<IRoutableViewModel>().Object;
        panelViewModels.Setup(p => p[It.IsAny<NavigationPanel>()]).Returns(dummy);
        _builder.RegisterInstance(panelViewModels.Object);

        var zoomService = new Mock<IZoomService>();
        zoomService.SetupGet(z => z.ZoomLevel).Returns(Observable.Return(100));
        _builder.RegisterInstance(zoomService.Object).As<IZoomService>();

        var cloudService = new Mock<ICloudSessionConnectionService>();
        cloudService.Setup(s => s.CanLogOutOrShutdown).Returns(Observable.Return(true));
        _builder.RegisterInstance(cloudService.Object).As<ICloudSessionConnectionService>();

        var repository = new Mock<ICloudSessionConnectionRepository>();
        repository.Setup(r => r.ConnectionStatusObservable).Returns(Observable.Never<SessionConnectionStatus>());
        _builder.RegisterInstance(repository.Object).As<ICloudSessionConnectionRepository>();

        _builder.RegisterInstance(flyout);
        _builder.RegisterInstance(header);
        _builder.RegisterInstance(announcement);
        RegisterType<NavigationService, INavigationService>();
        RegisterType<MainWindowViewModel>();
        BuildMoqContainer();
    }

    [Test]
    public async Task NavigateToHomeUpdatesRouter()
    {
        var navigationService = Container.Resolve<INavigationService>();
        var viewModel = Container.Resolve<MainWindowViewModel>();

        await ExecuteOnUiThread(() =>
        {
            navigationService.NavigateTo(NavigationPanel.Home);
            Assert.That(viewModel.Router.NavigationStack.Count, Is.EqualTo(1));
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task OnCloseWindowRequested_WithCtrlDown_ReturnsTrue()
    {
        var viewModel = Container.Resolve<MainWindowViewModel>();
        var result = await ExecuteOnUiThread(() => viewModel.OnCloseWindowRequested(true));
        Assert.That(result, Is.True);
    }
}
