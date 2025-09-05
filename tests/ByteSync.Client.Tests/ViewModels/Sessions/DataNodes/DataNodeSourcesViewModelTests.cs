using ByteSync.Business.DataNodes;
using ByteSync.Business.DataSources;
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

namespace ByteSync.Tests.ViewModels.Sessions.DataNodes;

[TestFixture]
public class DataNodeSourcesViewModelTests
{
    [Test]
    public void Constructor_WithAllDependencies_ShouldCreateInstance()
    {
        // Arrange
        var dataNode = new DataNode { Id = "DN_1" };
        var sessionService = new Mock<ISessionService>();
        var dataSourceService = new Mock<IDataSourceService>();
        var dataSourceProxyFactory = new Mock<IDataSourceProxyFactory>();
        var dataSourceRepository = new Mock<IDataSourceRepository>();
        var fileDialogService = new Mock<IFileDialogService>();

        var cache = new SourceCache<DataSource, string>(ds => ds.Key);
        dataSourceRepository.SetupGet(r => r.ObservableCache).Returns(cache);

        // Act
        var vm = new DataNodeSourcesViewModel(
            dataNode,
            true,
            sessionService.Object,
            dataSourceService.Object,
            dataSourceProxyFactory.Object,
            dataSourceRepository.Object,
            fileDialogService.Object);

        // Assert
        vm.Should().NotBeNull();
        vm.AddDirectoryCommand.Should().NotBeNull();
        vm.AddFileCommand.Should().NotBeNull();
        vm.RemoveDataSourceCommand.Should().NotBeNull();
        vm.IsLocalMachine.Should().BeTrue();
    }
}
