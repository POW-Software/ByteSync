using Autofac;
using Avalonia;
using Avalonia.Styling;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.DependencyInjection;
using ByteSync.Interfaces.Controls.Themes;

namespace ByteSync.Client.IntegrationTests.Services.Themes;

public class ThemeFactory_HeadlessTests : HeadlessIntegrationTest
{
    private ILifetimeScope _scope = null!;

    [SetUp]
    public void SetUp()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (ByteSync.Services.ContainerProvider.Container == null)
        {
            ServiceRegistrar.RegisterComponents();
        }

        _scope = ByteSync.Services.ContainerProvider.Container!.BeginLifetimeScope();

        // Ensure clean state between tests: ThemeService is a singleton; avoid duplicates
        var themeService = _scope.Resolve<IThemeService>();
        themeService.AvailableThemes.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        _scope.Dispose();
    }

    [Test]
    public void BuildThemes_registers_all_and_applies_default()
    {
        var themeFactory = _scope.Resolve<IThemeFactory>();
        var themeService = _scope.Resolve<IThemeService>();

        ExecuteOnUiThread(async () =>
        {
            themeFactory.BuildThemes();
            await Task.CompletedTask;
        }).Wait();

        Assert.That(themeService.AvailableThemes.Count, Is.EqualTo(24));
        Assert.That(Application.Current!.RequestedThemeVariant, Is.EqualTo(ThemeVariant.Light));

        var brush = themeService.GetBrush("SystemAccentColor");
        Assert.That(brush, Is.Not.Null);
    }

    [Test]
    public void SelectTheme_switches_variant_and_updates_resources()
    {
        var themeFactory = _scope.Resolve<IThemeFactory>();
        var themeService = _scope.Resolve<IThemeService>();

        ExecuteOnUiThread(async () =>
        {
            themeFactory.BuildThemes();
            themeService.SelectTheme("Blue1", isDarkMode: true);
            await Task.CompletedTask;
        }).Wait();

        Assert.That(Application.Current!.RequestedThemeVariant, Is.EqualTo(ThemeVariant.Dark));
        Assert.That(themeService.GetBrush("BsAccentButtonBackGround"), Is.Not.Null);
    }
}


