using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;
using Avalonia.Media;
using ByteSync.Business;
using ByteSync.Business.DataNodes;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Business.Themes;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Controls.Themes;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Localizations;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Tests.Helpers;
using ByteSync.ViewModels.Sessions.DataNodes;
using DynamicData;
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
        var member = new SessionMember
        {
            Endpoint = endpoint, PrivateData = new SessionMemberPrivateData { MachineName = "HOST" }, JoinedSessionOn = DateTimeOffset.Now
        };
        var dataNode = new DataNode { Id = "DN1", Code = "NODE-1", OrderIndex = 2 };
        
        var sourcesVm = new DataNodeSourcesViewModel();
        var headerVm = new DataNodeHeaderViewModel();
        
        var sessionMemberRepo = new Mock<ISessionMemberRepository>();
        var localization = new Mock<ILocalizationService>();
        localization.SetupGet(l => l.CurrentCultureObservable).Returns(Observable.Never<CultureDefinition>());
        var statusVm = new DataNodeStatusViewModel(member, true, sessionMemberRepo.Object, localization.Object);
        
        var theme = new Mock<IThemeService>();
        theme.Setup(t => t.GetBrush(It.IsAny<string>())).Returns(Mock.Of<IBrush>());
        theme.SetupGet(t => t.SelectedTheme).Returns(Observable.Never<Theme>());
        
        var dataNodeService = new Mock<IDataNodeService>();
        var dataNodeRepo = new Mock<IDataNodeRepository>();
        var sessionService = new Mock<ISessionService>();
        sessionService.SetupGet(s => s.SessionStatusObservable).Returns(Observable.Never<SessionStatus>());
        
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
    
    [Test]
    public void ShowAddButton_ShouldBeVisibleOnlyInPreparation_WhenLocalAndLast()
    {
        // Arrange
        var endpoint = new ByteSyncEndpoint { ClientInstanceId = "ci-1", ClientId = "c-1", IpAddress = "127.0.0.1", Version = "1.0" };
        var member = new SessionMember
        {
            Endpoint = endpoint, PrivateData = new SessionMemberPrivateData { MachineName = "HOST" }, JoinedSessionOn = DateTimeOffset.Now
        };
        var dataNode = new DataNode { Id = "DN1", Code = "NODE-1", OrderIndex = 1 };
        
        var sourcesVm = new DataNodeSourcesViewModel();
        var headerVm = new DataNodeHeaderViewModel();
        
        var sessionMemberRepo = new Mock<ISessionMemberRepository>();
        sessionMemberRepo
            .Setup(r => r.Watch(It.IsAny<SessionMember>()))
            .Returns(Observable.Never<Change<SessionMember, string>>());
        var localization = new Mock<ILocalizationService>();
        localization.SetupGet(l => l.CurrentCultureObservable).Returns(Observable.Never<CultureDefinition>());
        var statusVm = new DataNodeStatusViewModel(member, true, sessionMemberRepo.Object, localization.Object);
        
        var theme = new Mock<IThemeService>();
        theme.Setup(t => t.GetBrush(It.IsAny<string>())).Returns(Mock.Of<IBrush>());
        theme.SetupGet(t => t.SelectedTheme).Returns(Observable.Never<Theme>());
        
        var dataNodeService = new Mock<IDataNodeService>();
        
        // Repository with a single node where DN1 is last
        var cache = new SourceCache<DataNode, string>(n => n.Id);
        cache.AddOrUpdate(dataNode);
        var sorted = new List<DataNode> { dataNode };
        var dataNodeRepo = new Mock<IDataNodeRepository>();
        dataNodeRepo.SetupGet(r => r.ObservableCache).Returns(cache.AsObservableCache());
        dataNodeRepo.SetupGet(r => r.SortedCurrentMemberDataNodes).Returns(sorted);
        
        // Session status subject
        var status = new BehaviorSubject<SessionStatus>(SessionStatus.Preparation);
        var sessionService = new Mock<ISessionService>();
        sessionService.SetupGet(s => s.SessionStatusObservable).Returns(status);
        
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
        
        // Act
        vm.Activator.Activate();
        
        // Assert: visible in Preparation when local AND last
        vm.ShowAddButton.Should().BeTrue();
        
        // Move out of Preparation -> should hide
        status.OnNext(SessionStatus.Inventory);
        ReactiveViewModelTestHelpers.ShouldEventuallyBe(vm, x => x.ShowAddButton, false);
        
        // Back to Preparation -> visible again
        status.OnNext(SessionStatus.Preparation);
        ReactiveViewModelTestHelpers.ShouldEventuallyBe(vm, x => x.ShowAddButton, true);
    }
    
    [Test]
    public void ShowAddButton_ShouldUpdateWhenLastNodeChanges()
    {
        // Arrange
        var endpoint = new ByteSyncEndpoint { ClientInstanceId = "ci-1", ClientId = "c-1", IpAddress = "127.0.0.1", Version = "1.0" };
        var member = new SessionMember
        {
            Endpoint = endpoint, PrivateData = new SessionMemberPrivateData { MachineName = "HOST" }, JoinedSessionOn = DateTimeOffset.Now
        };
        var dn1 = new DataNode { Id = "DN1", Code = "NODE-1", OrderIndex = 1 };
        var dn2 = new DataNode { Id = "DN2", Code = "NODE-2", OrderIndex = 2 };
        
        var sourcesVm = new DataNodeSourcesViewModel();
        var headerVm = new DataNodeHeaderViewModel();
        
        var sessionMemberRepo = new Mock<ISessionMemberRepository>();
        sessionMemberRepo
            .Setup(r => r.Watch(It.IsAny<SessionMember>()))
            .Returns(Observable.Never<Change<SessionMember, string>>());
        var localization = new Mock<ILocalizationService>();
        localization.SetupGet(l => l.CurrentCultureObservable).Returns(Observable.Never<CultureDefinition>());
        var statusVm = new DataNodeStatusViewModel(member, true, sessionMemberRepo.Object, localization.Object);
        
        var theme = new Mock<IThemeService>();
        theme.Setup(t => t.GetBrush(It.IsAny<string>())).Returns(Mock.Of<IBrush>());
        theme.SetupGet(t => t.SelectedTheme).Returns(Observable.Never<Theme>());
        
        var dataNodeService = new Mock<IDataNodeService>();
        
        var cache = new SourceCache<DataNode, string>(n => n.Id);
        cache.AddOrUpdate(dn1);
        var sorted = new List<DataNode> { dn1 }; // dn1 is last initially
        var dataNodeRepo = new Mock<IDataNodeRepository>();
        dataNodeRepo.SetupGet(r => r.ObservableCache).Returns(cache.AsObservableCache());
        dataNodeRepo.SetupGet(r => r.SortedCurrentMemberDataNodes).Returns(sorted);
        
        var status = new BehaviorSubject<SessionStatus>(SessionStatus.Preparation);
        var sessionService = new Mock<ISessionService>();
        sessionService.SetupGet(s => s.SessionStatusObservable).Returns(status);
        
        var vm = new DataNodeViewModel(
            member,
            dn1,
            true,
            sourcesVm,
            headerVm,
            statusVm,
            theme.Object,
            dataNodeService.Object,
            dataNodeRepo.Object,
            sessionService.Object);
        
        vm.Activator.Activate();
        
        // Initially visible: dn1 is last
        vm.ShowAddButton.Should().BeTrue();
        
        // Add dn2 and make it the last in sorted list
        sorted.Clear();
        sorted.AddRange([dn1, dn2]);
        cache.AddOrUpdate(dn2); // triggers repository change
        
        ReactiveViewModelTestHelpers.ShouldEventuallyBe(vm, x => x.ShowAddButton, false);
        
        // Remove dn2 and make dn1 last again
        sorted.Clear();
        sorted.Add(dn1);
        cache.Remove(dn2);
        
        ReactiveViewModelTestHelpers.ShouldEventuallyBe(vm, x => x.ShowAddButton, true);
    }
    
    [Test]
    public void AddDataNodeCommand_CanExecute_MirrorsVisibilityRule()
    {
        // Arrange
        var endpoint = new ByteSyncEndpoint { ClientInstanceId = "ci-1", ClientId = "c-1", IpAddress = "127.0.0.1", Version = "1.0" };
        var member = new SessionMember
        {
            Endpoint = endpoint, PrivateData = new SessionMemberPrivateData { MachineName = "HOST" }, JoinedSessionOn = DateTimeOffset.Now
        };
        var dn1 = new DataNode { Id = "DN1", Code = "NODE-1", OrderIndex = 1 };
        
        var sourcesVm = new DataNodeSourcesViewModel();
        var headerVm = new DataNodeHeaderViewModel();
        
        var sessionMemberRepo = new Mock<ISessionMemberRepository>();
        sessionMemberRepo
            .Setup(r => r.Watch(It.IsAny<SessionMember>()))
            .Returns(Observable.Never<Change<SessionMember, string>>());
        var localization = new Mock<ILocalizationService>();
        localization.SetupGet(l => l.CurrentCultureObservable).Returns(Observable.Never<CultureDefinition>());
        var statusVm = new DataNodeStatusViewModel(member, true, sessionMemberRepo.Object, localization.Object);
        
        var theme = new Mock<IThemeService>();
        theme.Setup(t => t.GetBrush(It.IsAny<string>())).Returns(Mock.Of<IBrush>());
        theme.SetupGet(t => t.SelectedTheme).Returns(Observable.Never<Theme>());
        
        var dataNodeService = new Mock<IDataNodeService>();
        
        var cache = new SourceCache<DataNode, string>(n => n.Id);
        cache.AddOrUpdate(dn1);
        var sorted = new List<DataNode> { dn1 };
        var dataNodeRepo = new Mock<IDataNodeRepository>();
        dataNodeRepo.SetupGet(r => r.ObservableCache).Returns(cache.AsObservableCache());
        dataNodeRepo.SetupGet(r => r.SortedCurrentMemberDataNodes).Returns(sorted);
        
        var status = new BehaviorSubject<SessionStatus>(SessionStatus.Preparation);
        var sessionService = new Mock<ISessionService>();
        sessionService.SetupGet(s => s.SessionStatusObservable).Returns(status);
        
        var vm = new DataNodeViewModel(
            member,
            dn1,
            true,
            sourcesVm,
            headerVm,
            statusVm,
            theme.Object,
            dataNodeService.Object,
            dataNodeRepo.Object,
            sessionService.Object);
        
        vm.Activator.Activate();
        
        // In preparation and last -> can execute
        ((ICommand)vm.AddDataNodeCommand).CanExecute(null).Should().BeTrue();
        
        // Leave preparation -> cannot execute
        bool? latestCanExec = null;
        using var sub = vm.AddDataNodeCommand.CanExecute.Subscribe(b => latestCanExec = b);
        status.OnNext(SessionStatus.Inventory);
        SpinWait.SpinUntil(() => latestCanExec == false, TimeSpan.FromSeconds(5)).Should()
            .BeTrue("AddDataNodeCommand.CanExecute should become false outside Preparation");
    }
}