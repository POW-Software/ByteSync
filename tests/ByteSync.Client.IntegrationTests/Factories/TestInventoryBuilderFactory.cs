using System.Reactive.Linq;
using Autofac;
using ByteSync.Business.DataNodes;
using ByteSync.Business.DataSources;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Helpers;
using ByteSync.Factories;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Inventories;
using ByteSync.Services.Sessions;
using ByteSync.TestsCommon;
using DynamicData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ByteSync.Client.IntegrationTests.Factories;

public class TestInventoryBuilderFactory : IntegrationTest
{
    private IInventoryBuilderFactory _factory = null!;
    
    [SetUp]
    public void Setup()
    {
        RegisterClientTypes();
        
        var contextHelper = new TestContextGenerator(Container);
        contextHelper.GenerateSession();
        contextHelper.GenerateCurrentEndpoint();
        
        var testDirectory = _testDirectoryService.CreateTestDirectory();
        
        var mockEnvironmentService = Container.Resolve<Mock<IEnvironmentService>>();
        mockEnvironmentService.Setup(m => m.AssemblyFullName)
            .Returns(IOUtils.Combine(testDirectory.FullName, "Assembly", "Assembly.exe"));
        mockEnvironmentService.Setup(m => m.OSPlatform).Returns(OSPlatforms.Windows);
        
        var mockLocalApplicationDataManager = Container.Resolve<Mock<ILocalApplicationDataManager>>();
        mockLocalApplicationDataManager.Setup(m => m.ApplicationDataPath)
            .Returns(IOUtils.Combine(testDirectory.FullName, "ApplicationDataPath"));
        
        _factory = Container.Resolve<IInventoryBuilderFactory>();
    }
    
    private void RegisterClientTypes()
    {
        RegisterType<InventoryBuilderFactory, IInventoryBuilderFactory>();
        _builder.RegisterType<InventoryBuilder>().As<IInventoryBuilder>().InstancePerDependency();
        _builder.RegisterType<InventoryFileAnalyzer>().As<IInventoryFileAnalyzer>().InstancePerDependency();
        RegisterType<InventorySaver, IInventorySaver>();
        _builder.RegisterType<InventoryIndexer>().As<IInventoryIndexer>().InstancePerDependency();
        RegisterType<CloudSessionLocalDataManager, ICloudSessionLocalDataManager>();
        
        _builder.RegisterGeneric(typeof(Mock<>)).SingleInstance();
        _builder.Register(_ => new Mock<ILogger<InventoryBuilder>>().Object).As<ILogger<InventoryBuilder>>();
        _builder.Register(_ => new Mock<ILogger<InventoryBuilderFactory>>().Object).As<ILogger<InventoryBuilderFactory>>();
        _builder.Register(_ => new Mock<ILogger<InventoryFileAnalyzer>>().Object).As<ILogger<InventoryFileAnalyzer>>();
        _builder.Register(_ => new Mock<ILogger<InventoryService>>().Object).As<ILogger<InventoryService>>();
        
        _builder.Register(ctx =>
        {
            var sessionService = ctx.Resolve<ISessionService>();
            var inventoryFileRepository = ctx.Resolve<IInventoryFileRepository>();
            var dataNodeRepository = ctx.Resolve<IDataNodeRepository>();
            var sessionMemberRepository = ctx.Resolve<ISessionMemberRepository>();
            var logger = ctx.Resolve<ILogger<InventoryService>>();
            
            return new InventoryService(sessionService, inventoryFileRepository, dataNodeRepository, sessionMemberRepository, logger);
        }).As<IInventoryService>().SingleInstance();
        
        BuildMoqContainer();
        
        var mockSessionService = Container.Resolve<Mock<ISessionService>>();
        mockSessionService.Setup(s => s.SessionStatusObservable)
            .Returns(Observable.Never<SessionStatus>());
        mockSessionService.Setup(s => s.CurrentSessionSettings)
            .Returns(SessionSettings.BuildDefault());
        
        var mockSessionMemberRepository = Container.Resolve<Mock<ISessionMemberRepository>>();
        var emptyChangeSet = Observable.Never<ISortedChangeSet<SessionMember, string>>();
        mockSessionMemberRepository.Setup(r => r.SortedSessionMembersObservable)
            .Returns(emptyChangeSet);
    }
    
    [Test]
    public void CreateInventoryBuilder_ShouldResolveAllDependencies()
    {
        var sessionMemberRepository = Container.Resolve<ISessionMemberRepository>();
        var sessionMember = new SessionMember
        {
            Endpoint = new ByteSyncEndpoint { ClientInstanceId = "Client1" },
            PrivateData = new() { MachineName = "TestMachine" }
        };
        Mock.Get(sessionMemberRepository).Setup(r => r.GetCurrentSessionMember()).Returns(sessionMember);
        
        var dataNode = new DataNode
        {
            Id = "Node1",
            Code = "A",
            ClientInstanceId = "Client1",
            OrderIndex = 1
        };
        
        var dataSource = new DataSource
        {
            DataNodeId = "Node1",
            Code = "DS1",
            Path = _testDirectoryService.TestDirectory.FullName
        };
        
        var dataSourceRepository = Container.Resolve<IDataSourceRepository>();
        Mock.Get(dataSourceRepository).Setup(r => r.SortedCurrentMemberDataSources)
            .Returns(new List<DataSource> { dataSource });
        
        var inventoryBuilder = _factory.CreateInventoryBuilder(dataNode);
        
        inventoryBuilder.Should().NotBeNull();
        inventoryBuilder.Should().BeAssignableTo<IInventoryBuilder>();
        inventoryBuilder.Inventory.Should().NotBeNull();
        inventoryBuilder.Inventory.NodeId.Should().Be("Node1");
        inventoryBuilder.Inventory.Code.Should().Be("A");
        inventoryBuilder.Inventory.InventoryParts.Should().HaveCount(1);
    }
    
    [Test]
    public void CreateInventoryBuilder_ShouldFilterDataSourcesByDataNodeId()
    {
        var sessionMemberRepository = Container.Resolve<ISessionMemberRepository>();
        var sessionMember = new SessionMember
        {
            Endpoint = new ByteSyncEndpoint { ClientInstanceId = "Client1" },
            PrivateData = new() { MachineName = "TestMachine" }
        };
        Mock.Get(sessionMemberRepository).Setup(r => r.GetCurrentSessionMember()).Returns(sessionMember);
        
        var dataNode = new DataNode
        {
            Id = "Node1",
            Code = "A",
            ClientInstanceId = "Client1",
            OrderIndex = 1
        };
        
        var dataSource1 = new DataSource { DataNodeId = "Node1", Code = "DS1", Path = _testDirectoryService.TestDirectory.FullName };
        var dataSource2 = new DataSource { DataNodeId = "Node1", Code = "DS2", Path = _testDirectoryService.TestDirectory.FullName };
        var dataSource3 = new DataSource { DataNodeId = "Node2", Code = "DS3", Path = _testDirectoryService.TestDirectory.FullName };
        
        var dataSourceRepository = Container.Resolve<IDataSourceRepository>();
        Mock.Get(dataSourceRepository).Setup(r => r.SortedCurrentMemberDataSources)
            .Returns(new List<DataSource> { dataSource1, dataSource2, dataSource3 });
        
        var inventoryBuilder = _factory.CreateInventoryBuilder(dataNode);
        
        inventoryBuilder.Should().NotBeNull();
        inventoryBuilder.Inventory.InventoryParts.Should().HaveCount(2);
        inventoryBuilder.Inventory.InventoryParts.Should().OnlyContain(ip =>
            ip.RootPath == dataSource1.Path || ip.RootPath == dataSource2.Path);
    }
    
    [Test]
    public void CreateInventoryBuilder_ShouldCreateEmptyInventoryWhenNoMatchingDataSources()
    {
        var sessionMemberRepository = Container.Resolve<ISessionMemberRepository>();
        var sessionMember = new SessionMember
        {
            Endpoint = new ByteSyncEndpoint { ClientInstanceId = "Client1" },
            PrivateData = new() { MachineName = "TestMachine" }
        };
        Mock.Get(sessionMemberRepository).Setup(r => r.GetCurrentSessionMember()).Returns(sessionMember);
        
        var dataNode = new DataNode
        {
            Id = "Node1",
            Code = "A",
            ClientInstanceId = "Client1",
            OrderIndex = 1
        };
        
        var dataSource = new DataSource { DataNodeId = "Node2", Code = "DS1", Path = _testDirectoryService.TestDirectory.FullName };
        
        var dataSourceRepository = Container.Resolve<IDataSourceRepository>();
        Mock.Get(dataSourceRepository).Setup(r => r.SortedCurrentMemberDataSources)
            .Returns(new List<DataSource> { dataSource });
        
        var inventoryBuilder = _factory.CreateInventoryBuilder(dataNode);
        
        inventoryBuilder.Should().NotBeNull();
        inventoryBuilder.Inventory.InventoryParts.Should().BeEmpty();
    }
    
    [Test]
    public void CreateInventoryBuilder_ShouldUseCurrentSessionSettings()
    {
        var sessionMemberRepository = Container.Resolve<ISessionMemberRepository>();
        var sessionService = Container.Resolve<ISessionService>();
        
        var sessionMember = new SessionMember
        {
            Endpoint = new ByteSyncEndpoint { ClientInstanceId = "Client1" },
            PrivateData = new() { MachineName = "TestMachine" }
        };
        Mock.Get(sessionMemberRepository).Setup(r => r.GetCurrentSessionMember()).Returns(sessionMember);
        
        var sessionSettings = SessionSettings.BuildDefault();
        sessionSettings.AnalysisMode = AnalysisModes.Smart;
        sessionSettings.ExcludeHiddenFiles = true;
        
        Mock.Get(sessionService).Setup(s => s.CurrentSessionSettings).Returns(sessionSettings);
        
        var dataNode = new DataNode
        {
            Id = "Node1",
            Code = "A",
            ClientInstanceId = "Client1"
        };
        
        var dataSourceRepository = Container.Resolve<IDataSourceRepository>();
        Mock.Get(dataSourceRepository).Setup(r => r.SortedCurrentMemberDataSources)
            .Returns(new List<DataSource>());
        
        var inventoryBuilder = _factory.CreateInventoryBuilder(dataNode);
        
        inventoryBuilder.Should().NotBeNull();
        inventoryBuilder.SessionSettings.Should().NotBeNull();
        inventoryBuilder.SessionSettings!.AnalysisMode.Should().Be(AnalysisModes.Smart);
        inventoryBuilder.SessionSettings.ExcludeHiddenFiles.Should().BeTrue();
    }
    
    [Test]
    public void CreateInventoryBuilder_ShouldResolveSharedDependencies()
    {
        var sessionMemberRepository = Container.Resolve<ISessionMemberRepository>();
        var sessionMember = new SessionMember
        {
            Endpoint = new ByteSyncEndpoint { ClientInstanceId = "Client1" },
            PrivateData = new() { MachineName = "TestMachine" }
        };
        Mock.Get(sessionMemberRepository).Setup(r => r.GetCurrentSessionMember()).Returns(sessionMember);
        
        var dataNode1 = new DataNode { Id = "Node1", Code = "A", ClientInstanceId = "Client1" };
        var dataNode2 = new DataNode { Id = "Node2", Code = "B", ClientInstanceId = "Client1" };
        
        var dataSourceRepository = Container.Resolve<IDataSourceRepository>();
        Mock.Get(dataSourceRepository).Setup(r => r.SortedCurrentMemberDataSources)
            .Returns(new List<DataSource>());
        
        var inventoryBuilder1 = _factory.CreateInventoryBuilder(dataNode1);
        var inventoryBuilder2 = _factory.CreateInventoryBuilder(dataNode2);
        
        inventoryBuilder1.Should().NotBeNull();
        inventoryBuilder2.Should().NotBeNull();
        inventoryBuilder1.Should().NotBeSameAs(inventoryBuilder2);
    }
}
