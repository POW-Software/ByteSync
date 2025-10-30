using System.Reactive.Linq;
using ByteSync.Business.DataNodes;
using ByteSync.Business.DataSources;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Factories.Proxies;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.ViewModels.Sessions.DataNodes;
using DynamicData;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.ViewModels.Sessions.DataNodes;

[TestFixture]
public class DataNodeSourcesViewModelTests
{
    private Mock<ISessionService> _sessionServiceMock = null!;
    private Mock<IDataSourceService> _dataSourceServiceMock = null!;
    private Mock<IDataSourceProxyFactory> _dataSourceProxyFactoryMock = null!;
    private Mock<IDataSourceRepository> _dataSourceRepositoryMock = null!;
    private Mock<IFileDialogService> _fileDialogServiceMock = null!;
    private DataNode _dataNode = null!;
    private SourceCache<DataSource, string> _cache = null!;

    [SetUp]
    public void SetUp()
    {
        _dataNode = new DataNode { Id = "DN_1" };
        _sessionServiceMock = new Mock<ISessionService>();
        _dataSourceServiceMock = new Mock<IDataSourceService>();
        _dataSourceProxyFactoryMock = new Mock<IDataSourceProxyFactory>();
        _dataSourceRepositoryMock = new Mock<IDataSourceRepository>();
        _fileDialogServiceMock = new Mock<IFileDialogService>();
        
        _cache = new SourceCache<DataSource, string>(ds => ds.Key);
        _dataSourceRepositoryMock.SetupGet(r => r.ObservableCache).Returns(_cache);
    }

    [Test]
    public void Constructor_WithAllDependencies_ShouldCreateInstance()
    {
        // Act
        var vm = CreateViewModel();
        
        // Assert
        vm.Should().NotBeNull();
        vm.AddDirectoryCommand.Should().NotBeNull();
        vm.AddFileCommand.Should().NotBeNull();
        vm.RemoveDataSourceCommand.Should().NotBeNull();
        vm.IsLocalMachine.Should().BeTrue();
    }

    [Test]
    public async Task AddDirectoryCommand_WhenUserSelectsDirectory_ShouldCallCreateAndTryAddDataSource()
    {
        // Arrange
        var selectedPath = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
        _fileDialogServiceMock
            .Setup(f => f.ShowOpenFolderDialogAsync(It.IsAny<string>()))
            .ReturnsAsync(selectedPath);

        var vm = CreateViewModel();

        // Act
        await vm.AddDirectoryCommand.Execute().FirstAsync();

        // Assert
        _fileDialogServiceMock.Verify(
            f => f.ShowOpenFolderDialogAsync(It.IsAny<string>()), 
            Times.Once);
        _dataSourceServiceMock.Verify(
            s => s.CreateAndTryAddDataSource(selectedPath, FileSystemTypes.Directory, _dataNode), 
            Times.Once);
    }

    [Test]
    public async Task AddDirectoryCommand_WhenUserSelectsRootDirectory_ShouldCallCreateAndTryAddDataSource()
    {
        // Arrange
        const string selectedPath = @"C:\";
        _fileDialogServiceMock
            .Setup(f => f.ShowOpenFolderDialogAsync(It.IsAny<string>()))
            .ReturnsAsync(selectedPath);

        var vm = CreateViewModel();

        // Act
        await vm.AddDirectoryCommand.Execute().FirstAsync();

        // Assert
        _fileDialogServiceMock.Verify(
            f => f.ShowOpenFolderDialogAsync(It.IsAny<string>()), 
            Times.Once);
        _dataSourceServiceMock.Verify(
            s => s.CreateAndTryAddDataSource(selectedPath, FileSystemTypes.Directory, _dataNode), 
            Times.Once);
    }

    [Test]
    public async Task AddDirectoryCommand_WhenUserCancelsDialog_ShouldNotCallCreateAndTryAddDataSource()
    {
        // Arrange
        _fileDialogServiceMock
            .Setup(f => f.ShowOpenFolderDialogAsync(It.IsAny<string>()))
            .ReturnsAsync((string?)null);

        var vm = CreateViewModel();

        // Act
        await vm.AddDirectoryCommand.Execute().FirstAsync();

        // Assert
        _fileDialogServiceMock.Verify(
            f => f.ShowOpenFolderDialogAsync(It.IsAny<string>()), 
            Times.Once);
        _dataSourceServiceMock.Verify(
            s => s.CreateAndTryAddDataSource(It.IsAny<string>(), It.IsAny<FileSystemTypes>(), It.IsAny<DataNode>()), 
            Times.Never);
    }

    [Test]
    public async Task AddFileCommand_WhenUserSelectsFiles_ShouldCallCreateAndTryAddDataSourceForEachFile()
    {
        // Arrange
        var selectedFiles = new[] 
        { 
            @"C:\Users\TestUser\file1.txt", 
            @"C:\Users\TestUser\file2.txt" 
        };
        _fileDialogServiceMock
            .Setup(f => f.ShowOpenFileDialogAsync(It.IsAny<string>(), true))
            .ReturnsAsync(selectedFiles);

        var vm = CreateViewModel();

        // Act
        await vm.AddFileCommand.Execute().FirstAsync();

        // Assert
        _fileDialogServiceMock.Verify(
            f => f.ShowOpenFileDialogAsync(It.IsAny<string>(), true), 
            Times.Once);
        _dataSourceServiceMock.Verify(
            s => s.CreateAndTryAddDataSource(selectedFiles[0], FileSystemTypes.File, _dataNode), 
            Times.Once);
        _dataSourceServiceMock.Verify(
            s => s.CreateAndTryAddDataSource(selectedFiles[1], FileSystemTypes.File, _dataNode), 
            Times.Once);
    }

    [Test]
    public async Task AddFileCommand_WhenUserCancelsDialog_ShouldNotCallCreateAndTryAddDataSource()
    {
        // Arrange
        _fileDialogServiceMock
            .Setup(f => f.ShowOpenFileDialogAsync(It.IsAny<string>(), true))
            .ReturnsAsync((string[]?)null);

        var vm = CreateViewModel();

        // Act
        await vm.AddFileCommand.Execute().FirstAsync();

        // Assert
        _fileDialogServiceMock.Verify(
            f => f.ShowOpenFileDialogAsync(It.IsAny<string>(), true), 
            Times.Once);
        _dataSourceServiceMock.Verify(
            s => s.CreateAndTryAddDataSource(It.IsAny<string>(), It.IsAny<FileSystemTypes>(), It.IsAny<DataNode>()), 
            Times.Never);
    }

    private DataNodeSourcesViewModel CreateViewModel()
    {
        return new DataNodeSourcesViewModel(
            _dataNode,
            true,
            _sessionServiceMock.Object,
            _dataSourceServiceMock.Object,
            _dataSourceProxyFactoryMock.Object,
            _dataSourceRepositoryMock.Object,
            _fileDialogServiceMock.Object);
    }
}