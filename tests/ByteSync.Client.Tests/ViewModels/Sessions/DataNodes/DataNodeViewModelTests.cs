using System.Reactive.Linq;
using Avalonia.Media;
using ByteSync.Business.DataNodes;
using ByteSync.Business.SessionMembers;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.ViewModels.Sessions.DataNodes;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.ViewModels.Sessions.DataNodes;

[TestFixture]
public class DataNodeViewModelTests
{
    [Test]
    public void Constructor_WithAllDependencies_ShouldCreateInstance()
    {
        // Arrange
        var endpoint = new ByteSyncEndpoint { ClientInstanceId = "ci-1", ClientId = "c-1", IpAddress = "127.0.0.1", Version = "1.0" };
        var member = new SessionMember { Endpoint = endpoint, PrivateData = new SessionMemberPrivateData { MachineName = "HOST" }, JoinedSessionOn = DateTimeOffset.Now };
        var dataNode = new DataNode { Id = "DN1", Code = "NODE-1", OrderIndex = 2 };

        var sourcesVm = new DataNodeSourcesViewModel();
        var headerVm = new DataNodeHeaderViewModel();

        var sessionMemberRepo = new Mock<ISessionMemberRepository>();
        var localization = new Mock<ByteSync.Interfaces.Services.Localizations.ILocalizationService>();
        localization.SetupGet(l => l.CurrentCultureObservable).Returns(Observable.Never<ByteSync.Business.CultureDefinition>());
        var statusVm = new DataNodeStatusViewModel(member, true, sessionMemberRepo.Object, localization.Object);

        var theme = new Mock<IThemeService>();
        theme.Setup(t => t.GetBrush(It.IsAny<string>())).Returns(Mock.Of<IBrush>());
        theme.SetupGet(t => t.SelectedTheme).Returns(Observable.Never<ByteSync.Business.Themes.Theme>());

        var dataNodeService = new Mock<IDataNodeService>();
        var dataNodeRepo = new Mock<IDataNodeRepository>();
        var sessionService = new Mock<ISessionService>();
        sessionService.SetupGet(s => s.SessionStatusObservable).Returns(Observable.Never<ByteSync.Business.Sessions.SessionStatus>());

        // Act
        var vm = new DataNodeViewModel(
            member,
            dataNode,
            true,
            sourcesVm,
            headerVm,
            statusVm,
            theme.Object,
            dataNodeService.Object,
            dataNodeRepo.Object,
            sessionService.Object);

        // Assert
        vm.Should().NotBeNull();
        vm.SourcesViewModel.Should().BeSameAs(sourcesVm);
        vm.HeaderViewModel.Should().BeSameAs(headerVm);
        vm.StatusViewModel.Should().BeSameAs(statusVm);
        vm.IsLocalMachine.Should().BeTrue();
        vm.AddDataNodeCommand.Should().NotBeNull();
        vm.MainGridBrush.Should().NotBeNull();
    }
}

