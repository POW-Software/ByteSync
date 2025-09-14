using System.Reactive.Threading.Tasks;
using ByteSync.Common.Interfaces;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.ViewModels.Headers;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.ViewModels.Headers;

[TestFixture]
public class AboutApplicationViewModelCommandTests
{
    [Test]
    public async Task ExploreAppDataCommand_Should_Open_ShellApplicationDataPath()
    {
        var env = new Mock<IEnvironmentService>();
        var web = new Mock<IWebAccessor>();
        var fs = new Mock<IFileSystemAccessor>();
        var ladm = new Mock<ILocalApplicationDataManager>();
        var logger = new Mock<ILogger<AboutApplicationViewModel>>();

        ladm.SetupGet(l => l.ShellApplicationDataPath).Returns("SHELL_PATH");
        fs.Setup(f => f.OpenDirectory("SHELL_PATH")).Returns(Task.CompletedTask).Verifiable();

        var vm = new AboutApplicationViewModel(env.Object, web.Object, fs.Object, ladm.Object, logger.Object);

        await vm.ExploreAppDataCommand.Execute().ToTask();

        fs.Verify(f => f.OpenDirectory("SHELL_PATH"), Times.Once);
    }

    [Test]
    public async Task OpenLogCommand_Should_Open_Shell_Log_Path()
    {
        var env = new Mock<IEnvironmentService>();
        var web = new Mock<IWebAccessor>();
        var fs = new Mock<IFileSystemAccessor>();
        var ladm = new Mock<ILocalApplicationDataManager>();
        var logger = new Mock<ILogger<AboutApplicationViewModel>>();

        ladm.SetupGet(l => l.DebugLogFilePath).Returns((string)null);
        ladm.SetupGet(l => l.LogFilePath).Returns("LOGICAL_LOG_PATH");
        ladm.Setup(l => l.GetShellPath("LOGICAL_LOG_PATH")).Returns("SHELL_LOG_PATH");
        fs.Setup(f => f.OpenFile("SHELL_LOG_PATH")).Returns(Task.CompletedTask).Verifiable();

        var vm = new AboutApplicationViewModel(env.Object, web.Object, fs.Object, ladm.Object, logger.Object);

        await vm.OpenLogCommand.Execute().ToTask();

        fs.Verify(f => f.OpenFile("SHELL_LOG_PATH"), Times.Once);
    }
}