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
    private Mock<IEnvironmentService> _envMock = null!;
    private Mock<IWebAccessor> _webMock = null!;
    private Mock<IFileSystemAccessor> _fsMock = null!;
    private Mock<ILocalApplicationDataManager> _ladmMock = null!;
    private Mock<ILogger<AboutApplicationViewModel>> _loggerMock = null!;
    private AboutApplicationViewModel _vm = null!;

    [SetUp]
    public void SetUp()
    {
        _envMock = new Mock<IEnvironmentService>();
        _webMock = new Mock<IWebAccessor>();
        _fsMock = new Mock<IFileSystemAccessor>();
        _ladmMock = new Mock<ILocalApplicationDataManager>();
        _loggerMock = new Mock<ILogger<AboutApplicationViewModel>>();

        _vm = new AboutApplicationViewModel(_envMock.Object, _webMock.Object, _fsMock.Object, _ladmMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task ExploreAppDataCommand_Should_Open_AppData_Path()
    {
        // Arrange
        _ladmMock.SetupGet(l => l.ApplicationDataPath).Returns("APPDATA_PATH");
        _fsMock.Setup(f => f.OpenDirectory("APPDATA_PATH")).Returns(Task.CompletedTask).Verifiable();

        // Act
        await _vm.ExploreAppDataCommand.Execute().ToTask();

        // Assert
        _fsMock.Verify(f => f.OpenDirectory("APPDATA_PATH"), Times.Once);
    }

    [Test]
    public async Task OpenLogCommand_Should_Open_Log_Path()
    {
        // Arrange
        _ladmMock.SetupGet(l => l.DebugLogFilePath).Returns((string)null!);
        _ladmMock.SetupGet(l => l.LogFilePath).Returns("LOG_PATH");
        _fsMock.Setup(f => f.OpenFile("LOG_PATH")).Returns(Task.CompletedTask).Verifiable();

        // Act
        await _vm.OpenLogCommand.Execute().ToTask();

        // Assert
        _fsMock.Verify(f => f.OpenFile("LOG_PATH"), Times.Once);
    }
}