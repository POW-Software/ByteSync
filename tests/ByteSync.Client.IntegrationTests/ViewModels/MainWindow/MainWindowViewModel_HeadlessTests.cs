using System.Reactive.Linq;
using Autofac;
using Autofac.Features.Indexed;
using ByteSync.Business.Navigations;
using ByteSync.Business.Sessions;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Navigations;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions.Connecting;
using ByteSync.Services.Navigations;
using ByteSync.ViewModels;
using ByteSync.ViewModels.Announcements;
using ByteSync.ViewModels.Headers;
using ByteSync.ViewModels.Misc;
using Moq;
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
    public void NavigateToHomeUpdatesNavigationService()
    {
        var navigationService = Container.Resolve<INavigationService>();
        NavigationDetails? navigationDetails = null;
        
        // Subscribe to navigation changes
        navigationService.CurrentPanel.Subscribe(details => navigationDetails = details);
        
        // Trigger navigation
        navigationService.NavigateTo(NavigationPanel.Home);
        
        // Verify navigation worked
        Assert.That(navigationDetails, Is.Not.Null);
        Assert.That(navigationDetails!.NavigationPanel, Is.EqualTo(NavigationPanel.Home));
        Assert.That(navigationDetails.IconName, Is.EqualTo("RegularHomeAlt"));
    }

    [Test]
    public void MainWindowViewModelCanBeCreated()
    {
        // Test simple creation without any async operations or UI thread usage
        var viewModel = Container.Resolve<MainWindowViewModel>();
        
        Assert.That(viewModel, Is.Not.Null);
        Assert.That(viewModel.Router, Is.Not.Null);
        
        // Don't activate - just test that creation works
        Assert.Pass("MainWindowViewModel created successfully");
    }

    [Test]  
    public void OnCloseWindowRequested_WithCtrlDown_ReturnsTrue_Sync()
    {
        // Test without async/await and UI thread to avoid infinite loops
        var viewModel = Container.Resolve<MainWindowViewModel>();
        
        // This should work synchronously since CtrlDown bypasses async operations
        var task = viewModel.OnCloseWindowRequested(true);
        
        // Wait with timeout to avoid infinite loop
        bool completed = task.Wait(TimeSpan.FromSeconds(5));
        Assert.That(completed, Is.True, "Test should complete within 5 seconds");
        Assert.That(task.Result, Is.True);
    }
}
