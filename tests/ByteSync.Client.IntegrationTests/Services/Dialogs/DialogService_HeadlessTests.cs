using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using ByteSync.Business;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.Services.Dialogs;
using ByteSync.ViewModels.Misc;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.IntegrationTests.Services.Dialogs;

public class DialogService_HeadlessTests : HeadlessIntegrationTest
{
    [SetUp]
    public void Setup()
    {
        var dialogView = new Mock<IDialogView>();
        dialogView
            .Setup(d => d.ShowMessageBoxAsync(It.IsAny<MessageBoxViewModel>()))
            .ReturnsAsync(MessageBoxResult.OK);

        var localizationService = new Mock<ILocalizationService>();
        var factory = new Mock<IMessageBoxViewModelFactory>();
        factory.Setup(f => f.CreateMessageBoxViewModel(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string[]?>()))
            .Returns((string titleKey, string? messageKey, string[]? args) =>
                new MessageBoxViewModel(titleKey, messageKey, args == null ? null : [..args], localizationService.Object));

        _builder.RegisterInstance(dialogView.Object).As<IDialogView>();
        _builder.RegisterInstance(factory.Object).As<IMessageBoxViewModelFactory>();
        RegisterType<DialogService, IDialogService>();
        BuildMoqContainer();
    }

    [Test]
    public async Task ShowMessageBoxAsync_ReturnsExpectedResult()
    {
        var service = Container.Resolve<IDialogService>();
        var vm = service.CreateMessageBoxViewModel("title");
        var result = await service.ShowMessageBoxAsync(vm);
        Assert.That(result, Is.EqualTo(MessageBoxResult.OK));
    }
}
