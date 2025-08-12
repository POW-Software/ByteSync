﻿using ByteSync.Business;
using ByteSync.Business.DataSources;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Dialogs;
using ByteSync.Services.Inventories;
using ByteSync.ViewModels.Misc;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Inventories;

public class DataSourceCheckerTests
{
    private Mock<IDialogService> _mockDialogService;
    private List<DataSource> _existingDataSources;
    
    private DataSourceChecker _dataSourceChecker;

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

        _dataSourceChecker = new DataSourceChecker(_mockDialogService.Object);
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
}