using System.Reactive.Linq;
using Avalonia.Media;
using ByteSync.Business.DataNodes;
using ByteSync.Business.SessionMembers;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.ViewModels.Misc;
using ByteSync.ViewModels.Sessions.DataNodes;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.ViewModels.Sessions.DataNodes;

[TestFixture]
public class DataNodeHeaderViewModelTests
{
    [Test]
    public void Constructor_WithAllDependencies_ShouldCreateInstance()
    {
        // Arrange
        var endpoint = new ByteSyncEndpoint { ClientInstanceId = "ci-1", ClientId = "c-1", IpAddress = "127.0.0.1", Version = "1.0" };
        var member = new SessionMember { Endpoint = endpoint, PrivateData = new SessionMemberPrivateData { MachineName = "HOST" } };
        var dataNode = new DataNode { Id = "DN1", Code = "NODE-1" };

        var localization = new Mock<ILocalizationService>();
        localization.SetupGet(l => l.CurrentCultureObservable).Returns(Observable.Never<ByteSync.Business.CultureDefinition>());
        localization.Setup(l => l[It.IsAny<string>()]).Returns((string key) => key);

        var theme = new Mock<IThemeService>();
        theme.Setup(t => t.GetBrush(It.IsAny<string>())).Returns(Mock.Of<IBrush>());
        theme.SetupGet(t => t.SelectedTheme).Returns(Observable.Never<ByteSync.Business.Themes.Theme>());

        var nodeRepo = new Mock<IDataNodeRepository>();
        nodeRepo.SetupGet(r => r.SortedCurrentMemberDataNodes).Returns(new List<DataNode>());

        var nodeService = new Mock<IDataNodeService>();
        var sessionService = new Mock<ISessionService>();
        sessionService.SetupGet(s => s.SessionStatusObservable).Returns(Observable.Never<ByteSync.Business.Sessions.SessionStatus>());

        var errorVm = new ErrorViewModel();

        // Act
        var vm = new DataNodeHeaderViewModel(
            member,
            dataNode,
            true,
            localization.Object,
            theme.Object,
            nodeRepo.Object,
            nodeService.Object,
            sessionService.Object,
            errorVm);

        // Assert
        vm.Should().NotBeNull();
        vm.ClientInstanceId.Should().Be(endpoint.ClientInstanceId);
        vm.Code.Should().Be("NODE-1");
        vm.MachineDescription.Should().NotBeNullOrEmpty();
        vm.LetterBackBrush.Should().NotBeNull();
        vm.LetterBorderBrush.Should().NotBeNull();
        vm.RemoveDataNodeCommand.Should().NotBeNull();
        vm.RemoveDataNodeError.Should().NotBeNull();
    }
}

