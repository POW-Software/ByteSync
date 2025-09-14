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
    private Mock<IEnvironmentService> _environmentService = null!;
    private Mock<IWebAccessor> _webAccessor = null!;
    private Mock<IFileSystemAccessor> _fileSystemAccessor = null!;
    private Mock<ILocalApplicationDataManager> _localApplicationDataManager = null!;
    private Mock<ILogger<AboutApplicationViewModel>> _loggerMock = null!;
    private AboutApplicationViewModel _vm = null!;

    [SetUp]
    public void SetUp()
    {
        _environmentService = new Mock<IEnvironmentService>();
        _webAccessor = new Mock<IWebAccessor>();
        _fileSystemAccessor = new Mock<IFileSystemAccessor>();
        _localApplicationDataManager = new Mock<ILocalApplicationDataManager>();
        _loggerMock = new Mock<ILogger<AboutApplicationViewModel>>();

        _vm = new AboutApplicationViewModel(_environmentService.Object, _webAccessor.Object, _fileSystemAccessor.Object,
            _localApplicationDataManager.Object, _loggerMock.Object);
    }

    [Test]
    public async Task ExploreAppDataCommand_Should_Open_AppData_Path()
    {
        // Arrange
        _localApplicationDataManager.SetupGet(l => l.ApplicationDataPath).Returns("APPDATA_PATH");
        _fileSystemAccessor.Setup(f => f.OpenDirectory("APPDATA_PATH")).Returns(Task.CompletedTask).Verifiable();

        // Act
        await _vm.ExploreAppDataCommand.Execute().ToTask();

        // Assert
        _fileSystemAccessor.Verify(f => f.OpenDirectory("APPDATA_PATH"), Times.Once);
    }

    [Test]
    public async Task OpenLogCommand_Should_Open_Log_Path()
    {
        // Arrange
        _localApplicationDataManager.SetupGet(l => l.DebugLogFilePath).Returns((string)null!);
        _localApplicationDataManager.SetupGet(l => l.LogFilePath).Returns("LOG_PATH");
        _fileSystemAccessor.Setup(f => f.OpenFile("LOG_PATH")).Returns(Task.CompletedTask).Verifiable();

        // Act
        await _vm.OpenLogCommand.Execute().ToTask();

        // Assert
        _fileSystemAccessor.Verify(f => f.OpenFile("LOG_PATH"), Times.Once);
    }
}