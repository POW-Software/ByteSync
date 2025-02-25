using ByteSync.Business;
using ByteSync.Business.PathItems;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Services.Inventories;
using ByteSync.ViewModels.Misc;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Controls.Inventories;

public class PathItemCheckerTests
{
    private Mock<IDialogService> _mockDialogService;
    private List<PathItem> _existingPathItems;
    
    private PathItemChecker _pathItemChecker;

    [SetUp]
    public void Setup()
    {
        _mockDialogService = new Mock<IDialogService>();
        
        _mockDialogService
            .Setup(x => x.ShowMessageBoxAsync(It.IsAny<MessageBoxViewModel>()))
            .ReturnsAsync(MessageBoxResult.OK);
        _mockDialogService
            .Setup(x => x.CreateMessageBoxViewModel(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
            .Returns((string title, string message, string[] parameters) => new MessageBoxViewModel { ShowOK = true });

        _pathItemChecker = new PathItemChecker(_mockDialogService.Object);
        _existingPathItems = new List<PathItem>();
    }

    [Test]
    public async Task CheckPathItem_File_Unique_ReturnsTrue()
    {
        var pathItem = new PathItem
        {
            ClientInstanceId = "client1",
            Type = FileSystemTypes.File,
            Path = "file.txt"
        };

        var result = await _pathItemChecker.CheckPathItem(pathItem, _existingPathItems);

        result.Should().BeTrue();
        _mockDialogService.Verify(x => x.ShowMessageBoxAsync(It.IsAny<MessageBoxViewModel>()), Times.Never);
    }

    [Test]
    public async Task CheckPathItem_File_Duplicate_ReturnsFalse()
    {
        var existing = new PathItem
        {
            ClientInstanceId = "client1",
            Type = FileSystemTypes.File,
            Path = "FILE.txt"
        };
        _existingPathItems.Add(existing);

        var pathItem = new PathItem
        {
            ClientInstanceId = "client1",
            Type = FileSystemTypes.File,
            Path = "file.txt"
        };

        var result = await _pathItemChecker.CheckPathItem(pathItem, _existingPathItems);

        result.Should().BeFalse();
        _mockDialogService.Verify(x => x.ShowMessageBoxAsync(It.IsAny<MessageBoxViewModel>()), Times.Once);
    }

    [Test]
    public async Task CheckPathItem_Directory_Unique_ReturnsTrue()
    {
        var pathItem = new PathItem
        {
            ClientInstanceId = "client1",
            Type = FileSystemTypes.Directory,
            Path = "/dirA"
        };

        var result = await _pathItemChecker.CheckPathItem(pathItem, _existingPathItems);

        result.Should().BeTrue();
        _mockDialogService.Verify(x => x.ShowMessageBoxAsync(It.IsAny<MessageBoxViewModel>()), Times.Never);
    }

    [Test]
    public async Task CheckPathItem_Directory_Duplicate_ReturnsFalse()
    {
        var existing = new PathItem
        {
            ClientInstanceId = "client1",
            Type = FileSystemTypes.Directory,
            Path = "/dirA"
        };
        _existingPathItems.Add(existing);

        var pathItem = new PathItem
        {
            ClientInstanceId = "client1",
            Type = FileSystemTypes.Directory,
            Path = "/dirA"
        };

        var result = await _pathItemChecker.CheckPathItem(pathItem, _existingPathItems);

        result.Should().BeFalse();
        _mockDialogService.Verify(x => x.ShowMessageBoxAsync(It.IsAny<MessageBoxViewModel>()), Times.Once);
    }

    [Test]
    public async Task CheckPathItem_Directory_SubPath_ReturnsFalse()
    {
        var existing = new PathItem
        {
            ClientInstanceId = "client1",
            Type = FileSystemTypes.Directory,
            Path = "/dirA"
        };
        _existingPathItems.Add(existing);

        var pathItem = new PathItem
        {
            ClientInstanceId = "client1",
            Type = FileSystemTypes.Directory,
            Path = "/dirA/subDir"
        };

        var result = await _pathItemChecker.CheckPathItem(pathItem, _existingPathItems);

        result.Should().BeFalse();
        _mockDialogService.Verify(x => x.ShowMessageBoxAsync(It.IsAny<MessageBoxViewModel>()), Times.Once);
    }

    [Test]
    public async Task CheckPathItem_Directory_ParentOfExisting_ReturnsFalse()
    {
        var existing = new PathItem
        {
            ClientInstanceId = "client1",
            Type = FileSystemTypes.Directory,
            Path = "/dirA/subDir"
        };
        _existingPathItems.Add(existing);

        var pathItem = new PathItem
        {
            ClientInstanceId = "client1",
            Type = FileSystemTypes.Directory,
            Path = "/dirA"
        };

        var result = await _pathItemChecker.CheckPathItem(pathItem, _existingPathItems);

        result.Should().BeFalse();
        _mockDialogService.Verify(x => x.ShowMessageBoxAsync(It.IsAny<MessageBoxViewModel>()), Times.Once);
    }
}