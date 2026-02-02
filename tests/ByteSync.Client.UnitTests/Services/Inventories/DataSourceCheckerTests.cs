using ByteSync.Business;
using ByteSync.Business.DataSources;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Misc;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Services.Inventories;
using ByteSync.ViewModels.Misc;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Inventories;

public class DataSourceCheckerTests
{
    private Mock<IDialogService> _mockDialogService = null!;
    private Mock<IEnvironmentService> _mockEnvironmentService = null!;
    private List<DataSource> _existingDataSources = null!;
    
    private DataSourceChecker _dataSourceChecker = null!;
    
    [SetUp]
    public void Setup()
    {
        _mockDialogService = new Mock<IDialogService>();
        
        _mockDialogService
            .Setup(x => x.ShowMessageBoxAsync(It.IsAny<MessageBoxViewModel>()))
            .ReturnsAsync(MessageBoxResult.OK);
        _mockDialogService
            .Setup(x => x.CreateMessageBoxViewModel(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
            .Returns((string _, string _, string[] _) => new MessageBoxViewModel { ShowOK = true });
        
        _mockEnvironmentService = new Mock<IEnvironmentService>();
        _mockEnvironmentService.SetupGet(x => x.ClientInstanceId).Returns("client1");
        _mockEnvironmentService.SetupGet(x => x.OSPlatform).Returns(OSPlatforms.Linux);
        var logger = new Mock<ILogger<DataSourceChecker>>().Object;
        
        _dataSourceChecker = new DataSourceChecker(_mockDialogService.Object, _mockEnvironmentService.Object, logger);
        _existingDataSources = new List<DataSource>();
    }
    
    [Test]
    public async Task CheckDataSource_File_Unique_ReturnsTrue()
    {
        var dataSource = new DataSource
        {
            ClientInstanceId = "client1",
            Type = FileSystemTypes.File,
            Path = "file.txt"
        };
        
        var result = await _dataSourceChecker.CheckDataSource(dataSource, _existingDataSources);
        
        result.Should().BeTrue();
        _mockDialogService.Verify(x => x.ShowMessageBoxAsync(It.IsAny<MessageBoxViewModel>()), Times.Never);
    }
    
    [Test]
    public async Task CheckDataSource_File_Duplicate_ReturnsFalse()
    {
        var existing = new DataSource
        {
            ClientInstanceId = "client1",
            Type = FileSystemTypes.File,
            Path = "FILE.txt"
        };
        _existingDataSources.Add(existing);
        
        var source = new DataSource
        {
            ClientInstanceId = "client1",
            Type = FileSystemTypes.File,
            Path = "file.txt"
        };
        
        var result = await _dataSourceChecker.CheckDataSource(source, _existingDataSources);
        
        result.Should().BeFalse();
        _mockDialogService.Verify(x => x.ShowMessageBoxAsync(It.IsAny<MessageBoxViewModel>()), Times.Once);
    }
    
    [Test]
    public async Task CheckDataSource_Directory_Unique_ReturnsTrue()
    {
        var dataSource = new DataSource
        {
            ClientInstanceId = "client1",
            Type = FileSystemTypes.Directory,
            Path = "/dirA"
        };
        
        var result = await _dataSourceChecker.CheckDataSource(dataSource, _existingDataSources);
        
        result.Should().BeTrue();
        _mockDialogService.Verify(x => x.ShowMessageBoxAsync(It.IsAny<MessageBoxViewModel>()), Times.Never);
    }
    
    [Test]
    public async Task CheckDataSource_Directory_Duplicate_ReturnsFalse()
    {
        var existing = new DataSource
        {
            ClientInstanceId = "client1",
            Type = FileSystemTypes.Directory,
            Path = "/dirA"
        };
        _existingDataSources.Add(existing);
        
        var dataSource = new DataSource
        {
            ClientInstanceId = "client1",
            Type = FileSystemTypes.Directory,
            Path = "/dirA"
        };
        
        var result = await _dataSourceChecker.CheckDataSource(dataSource, _existingDataSources);
        
        result.Should().BeFalse();
        _mockDialogService.Verify(x => x.ShowMessageBoxAsync(It.IsAny<MessageBoxViewModel>()), Times.Once);
    }
    
    [Test]
    public async Task CheckDataSourceDirectory_SubPath_ReturnsFalse()
    {
        var existing = new DataSource
        {
            ClientInstanceId = "client1",
            Type = FileSystemTypes.Directory,
            Path = "/dirA"
        };
        _existingDataSources.Add(existing);
        
        var dataSource = new DataSource
        {
            ClientInstanceId = "client1",
            Type = FileSystemTypes.Directory,
            Path = "/dirA/subDir"
        };
        
        var result = await _dataSourceChecker.CheckDataSource(dataSource, _existingDataSources);
        
        result.Should().BeFalse();
        _mockDialogService.Verify(x => x.ShowMessageBoxAsync(It.IsAny<MessageBoxViewModel>()), Times.Once);
    }
    
    [Test]
    public async Task CheckDataSource_Directory_ParentOfExisting_ReturnsFalse()
    {
        var existing = new DataSource
        {
            ClientInstanceId = "client1",
            Type = FileSystemTypes.Directory,
            Path = "/dirA/subDir"
        };
        _existingDataSources.Add(existing);
        
        var dataSource = new DataSource
        {
            ClientInstanceId = "client1",
            Type = FileSystemTypes.Directory,
            Path = "/dirA"
        };
        
        var result = await _dataSourceChecker.CheckDataSource(dataSource, _existingDataSources);
        
        result.Should().BeFalse();
        _mockDialogService.Verify(x => x.ShowMessageBoxAsync(It.IsAny<MessageBoxViewModel>()), Times.Once);
    }
    
    [Test]
    public async Task CheckDataSource_ProtectedPath_Local_ReturnsFalse()
    {
        var dataSource = new DataSource
        {
            ClientInstanceId = "client1",
            Type = FileSystemTypes.Directory,
            Path = "/dev"
        };
        
        var result = await _dataSourceChecker.CheckDataSource(dataSource, _existingDataSources);
        
        result.Should().BeFalse();
        _mockDialogService.Verify(x => x.ShowMessageBoxAsync(It.IsAny<MessageBoxViewModel>()), Times.Once);
    }
    
    [Test]
    public async Task CheckDataSource_ProtectedPath_Remote_ReturnsTrue()
    {
        var dataSource = new DataSource
        {
            ClientInstanceId = "client2",
            Type = FileSystemTypes.Directory,
            Path = "/dev"
        };
        
        var result = await _dataSourceChecker.CheckDataSource(dataSource, _existingDataSources);
        
        result.Should().BeTrue();
        _mockDialogService.Verify(x => x.ShowMessageBoxAsync(It.IsAny<MessageBoxViewModel>()), Times.Never);
    }
}