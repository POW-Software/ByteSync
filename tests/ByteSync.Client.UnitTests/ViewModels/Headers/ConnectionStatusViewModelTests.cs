using System.Globalization;
using System.Reactive.Subjects;
using System.Resources;
using Avalonia.Media;
using ByteSync.Business;
using ByteSync.Business.Communications;
using ByteSync.Business.Themes;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.ViewModels.Headers;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.ViewModels.Headers;

public class ConnectionStatusViewModelTests
{
    private static readonly ResourceManager Resources = new("ByteSync.Assets.Resources.Resources", typeof(ConnectionStatusViewModel).Assembly);

    [TestCase("fr", "Serveur relais incompatible", "La reconnexion automatique est arrêtée.")]
    [TestCase("en", "Incompatible relay server", "Automatic reconnection has stopped.")]
    [TestCase("", "Incompatible relay server", "Automatic reconnection has stopped.")]
    public void MissingChallengeEndpoint_DisplaysLocalizedActionableError(string cultureName, string title, string details)
    {
        using var statuses = new BehaviorSubject<ConnectionStatuses>(ConnectionStatuses.RelayIncompatible);
        using var cultures = new BehaviorSubject<CultureDefinition>(new(CultureInfo.GetCultureInfo(cultureName)));
        using var themes = new BehaviorSubject<Theme>(new("test", ThemeModes.Light, null!, null!));
        var viewModel = CreateViewModel(statuses, cultures, themes);

        using var activation = viewModel.Activator.Activate();

        viewModel.Text.Should().Be(title);
        viewModel.Details.Should().Contain(details);
        viewModel.BadgeBrush.Should().BeSameAs(Brushes.Red);
    }

    [TestCase(ConnectionStatuses.Connected, "ConnectionStatus_Connected")]
    [TestCase(ConnectionStatuses.Connecting, "ConnectionStatus_Connecting")]
    [TestCase(ConnectionStatuses.ConnectionFailed, "ConnectionStatus_ConnectionFailed")]
    [TestCase(ConnectionStatuses.NotConnected, "ConnectionStatus_NotConnected")]
    [TestCase(ConnectionStatuses.RetryConnectingSoon, "ConnectionStatus_RetryConnectingSoon")]
    [TestCase((ConnectionStatuses)99, "ConnectionStatus_Unkown")]
    public void StatusChange_ClearsRelayErrorDetails(ConnectionStatuses nextStatus, string textKey)
    {
        using var statuses = new BehaviorSubject<ConnectionStatuses>(ConnectionStatuses.RelayIncompatible);
        using var cultures = new BehaviorSubject<CultureDefinition>(new(CultureInfo.GetCultureInfo("fr")));
        using var themes = new BehaviorSubject<Theme>(new("test", ThemeModes.Light, null!, null!));
        var viewModel = CreateViewModel(statuses, cultures, themes);
        using var activation = viewModel.Activator.Activate();

        statuses.OnNext(nextStatus);

        viewModel.Text.Should().Be(Resources.GetString(textKey, cultures.Value.CultureInfo));
        viewModel.Details.Should().BeNull();
        viewModel.BadgeBrush.Should().BeSameAs(nextStatus switch
        {
            ConnectionStatuses.Connected => Brushes.Green,
            ConnectionStatuses.Connecting => Brushes.Gray,
            _ => Brushes.Red
        });
    }

    [Test]
    public void CultureChange_UpdatesTheVisibleRelayErrorAndDetails()
    {
        using var statuses = new BehaviorSubject<ConnectionStatuses>(ConnectionStatuses.RelayIncompatible);
        using var cultures = new BehaviorSubject<CultureDefinition>(new(CultureInfo.GetCultureInfo("en")));
        using var themes = new BehaviorSubject<Theme>(new("test", ThemeModes.Light, null!, null!));
        var viewModel = CreateViewModel(statuses, cultures, themes);
        using var activation = viewModel.Activator.Activate();

        cultures.OnNext(new CultureDefinition(CultureInfo.GetCultureInfo("fr")));

        viewModel.Text.Should().Be("Serveur relais incompatible");
        viewModel.Details.Should().Contain("Vérifiez l’adresse du relais ou mettez à jour le serveur.");
    }

    private static ConnectionStatusViewModel CreateViewModel(BehaviorSubject<ConnectionStatuses> statuses,
        BehaviorSubject<CultureDefinition> cultures, BehaviorSubject<Theme> themes)
    {
        var connection = new Mock<IConnectionService>();
        connection.SetupGet(c => c.ConnectionStatus).Returns(statuses);
        connection.SetupGet(c => c.CurrentConnectionStatus).Returns(() => statuses.Value);
        var localization = new Mock<ILocalizationService>();
        localization.SetupGet(l => l.CurrentCultureObservable).Returns(cultures);
        localization.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => Resources.GetString(key, cultures.Value.CultureInfo)!);
        var theme = new Mock<IThemeService>();
        theme.SetupGet(t => t.SelectedTheme).Returns(themes);
        theme.Setup(t => t.GetBrush("HomeCloudSynchronizationBackGround")).Returns(Brushes.Green);
        theme.Setup(t => t.GetBrush("SystemControlForegroundBaseHighBrush")).Returns(Brushes.Gray);
        theme.Setup(t => t.GetBrush("HomeLocalSynchronizationBackGround")).Returns(Brushes.Red);
        return new ConnectionStatusViewModel(connection.Object, localization.Object, theme.Object);
    }
}
