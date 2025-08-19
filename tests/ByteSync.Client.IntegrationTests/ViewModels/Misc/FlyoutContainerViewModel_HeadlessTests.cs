using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Business;
using ByteSync.Interfaces.Factories.ViewModels;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.ViewModels.Misc;
using FluentAssertions;
using Moq;

namespace ByteSync.Client.IntegrationTests.ViewModels.Misc;

public class FlyoutContainerViewModel_HeadlessTests : HeadlessIntegrationTest
{
    private Mock<ILocalizationService> _localizationServiceMock = null!;
    private Mock<IFlyoutElementViewModelFactory> _factoryMock = null!;

    [SetUp]
    public void Setup()
    {
        _localizationServiceMock = new Mock<ILocalizationService>();
        _localizationServiceMock
            .Setup(ls => ls[It.IsAny<string>()])
            .Returns((string key) => $"{key}-title");

        _factoryMock = new Mock<IFlyoutElementViewModelFactory>();
    }

    [Test]
    public async Task ShowMessageBoxAsync_Should_Open_And_Return_OK()
    {
        var vm = new FlyoutContainerViewModel(_localizationServiceMock.Object, _factoryMock.Object);
        vm.Activator.Activate();

        var messageBox = new MessageBoxViewModel("Test.Title", null, null, _localizationServiceMock.Object);

        // Start ShowMessageBoxAsync on UI thread but DO NOT await its completion here
        // We need the returned Task so we can click OK later, otherwise we deadlock
        var resultTask = await ExecuteOnUiThread(() => Task.FromResult(vm.ShowMessageBoxAsync(messageBox)));

        // While waiting, ensure the flyout is shown and content is bound
        await ExecuteOnUiThread(async () =>
        {
            vm.Content.Should().BeSameAs(messageBox);
            vm.IsFlyoutContainerVisible.Should().BeTrue();
            await Task.CompletedTask;
        });

        // Simulate user clicking OK on UI thread
        await ExecuteOnUiThread(async () =>
        {
            messageBox.OKButtonCommand.Execute().Subscribe();
            await Task.CompletedTask;
        });

        // Retrieve result
        var result = await resultTask;
        result.Should().Be(MessageBoxResult.OK);

        // And the flyout should be closed
        await ExecuteOnUiThread(async () =>
        {
            vm.IsFlyoutContainerVisible.Should().BeFalse();
            vm.Content.Should().BeNull();
            await Task.CompletedTask;
        });
    }

    [Test]
    public async Task ShowMessageBoxAsync_From_BackgroundThread_Uses_Dispatcher_Path()
    {
        var vm = new FlyoutContainerViewModel(_localizationServiceMock.Object, _factoryMock.Object);
        vm.Activator.Activate();

        var messageBox = new MessageBoxViewModel("Bg.Title", null, null, _localizationServiceMock.Object);

        // Call from a background thread so Dispatcher.UIThread.CheckAccess() is false
        Task<MessageBoxResult?> resultTask = Task.Run(() => vm.ShowMessageBoxAsync(messageBox));

        // Ensure UI actually opened on dispatcher
        PumpUntil(() => vm.Content is not null);
        await ExecuteOnUiThread(async () =>
        {
            vm.Content.Should().BeSameAs(messageBox);
            vm.IsFlyoutContainerVisible.Should().BeTrue();
            await Task.CompletedTask;
        });

        // Click OK on UI
        await ExecuteOnUiThread(async () =>
        {
            messageBox.OKButtonCommand.Execute().Subscribe();
            await Task.CompletedTask;
        });

        var result = await resultTask;
        result.Should().Be(MessageBoxResult.OK);

        await ExecuteOnUiThread(async () =>
        {
            vm.IsFlyoutContainerVisible.Should().BeFalse();
            vm.Content.Should().BeNull();
            await Task.CompletedTask;
        });
    }
}


