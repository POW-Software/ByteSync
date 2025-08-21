using Autofac;
using ByteSync.Business.Navigations;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Interfaces.Controls.Navigations;
using ByteSync.Services.Navigations;

namespace ByteSync.Client.IntegrationTests.Services.Navigations;

public class NavigationService_HeadlessTests : HeadlessIntegrationTest
{
    [SetUp]
    public void Setup()
    {
        RegisterType<NavigationService, INavigationService>();
        BuildMoqContainer();
    }

    [Test]
    public void NavigateToHomePublishesDetails()
    {
        var service = Container.Resolve<INavigationService>();
        NavigationDetails? details = null;
        service.CurrentPanel.Subscribe(d => details = d);

        service.NavigateTo(NavigationPanel.Home);

        Assert.That(details?.NavigationPanel, Is.EqualTo(NavigationPanel.Home));
        Assert.That(details?.IconName, Is.EqualTo("RegularHomeAlt"));
        Assert.That(details?.TitleLocalizationName, Is.EqualTo("Shell_Home"));
    }
}
