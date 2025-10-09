using System.Reactive.Linq;
using Autofac;
using ByteSync.Business;
using ByteSync.Business.DataNodes;
using ByteSync.Business.DataSources;
using ByteSync.Business.Inventories;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.FileSystems;
using ByteSync.Models.Inventories;
using ByteSync.Repositories;
using ByteSync.Services.Comparisons;
using ByteSync.Services.Inventories;
using ByteSync.Services.Sessions;
using ByteSync.TestsCommon;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ByteSync.Client.IntegrationTests.Services.Inventories;

public class FullInventoryRunner_IntegrationTests : IntegrationTest
{
    private IFullInventoryRunner _fullInventoryRunner = null!;
    private IInventoryService _inventoryService = null!;
    private ICloudSessionLocalDataManager _cloudSessionLocalDataManager = null!;
    private ISessionService _sessionService = null!;
    private IDataNodeRepository _dataNodeRepository = null!;
    private IDataSourceRepository _dataSourceRepository = null!;
    private ISessionMemberRepository _sessionMemberRepository = null!;
    private Mock<ISessionMemberService> _sessionMemberServiceMock = null!;
    private Mock<IInventoryFinishedService> _inventoryFinishedServiceMock = null!;
    private Mock<IInventoryComparerFactory> _inventoryComparerFactoryMock = null!;
    private InventoryProcessData _inventoryProcessData = null!;
    
    [SetUp]
    public void Setup()
    {
        RegisterType<SessionInvalidationCachePolicy<InventoryFile, string>, ISessionInvalidationCachePolicy<InventoryFile, string>>();
        RegisterType<SessionInvalidationCachePolicy<DataNode, string>, ISessionInvalidationCachePolicy<DataNode, string>>();
        RegisterType<SessionInvalidationCachePolicy<DataSource, string>, ISessionInvalidationCachePolicy<DataSource, string>>();
        RegisterType<SessionInvalidationCachePolicy<SessionMember, string>, ISessionInvalidationCachePolicy<SessionMember, string>>();
        
        RegisterType<InventoryFileRepository, IInventoryFileRepository>();
        RegisterType<DataNodeRepository, IDataNodeRepository>();
        RegisterType<DataSourceRepository, IDataSourceRepository>();
        RegisterType<CloudSessionLocalDataManager, ICloudSessionLocalDataManager>();
        RegisterType<InitialStatusBuilder, IInitialStatusBuilder>();
        
        _builder.Register(ctx =>
        {
            var sessionService = ctx.Resolve<ISessionService>();
            var inventoryFileRepository = ctx.Resolve<IInventoryFileRepository>();
            var dataNodeRepository = ctx.Resolve<IDataNodeRepository>();
            var logger = ctx.Resolve<ILogger<InventoryService>>();
            
            return new InventoryService(sessionService, inventoryFileRepository, dataNodeRepository, logger);
        }).As<IInventoryService>().SingleInstance();
        
        BuildMoqContainer();
        
        _inventoryComparerFactoryMock = Container.Resolve<Mock<IInventoryComparerFactory>>();
        
        var contextHelper = new TestContextGenerator(Container);
        contextHelper.GenerateSession();
        var currentEndpoint = contextHelper.GenerateCurrentEndpoint();
        var testDirectory = _testDirectoryService.CreateTestDirectory();
        
        var mockSessionMemberRepository = Container.Resolve<Mock<ISessionMemberRepository>>();
        var currentSessionMember = new SessionMember
        {
            Endpoint = currentEndpoint,
            PrivateData = new SessionMemberPrivateData
            {
                MachineName = "TestMachine"
            }
        };
        mockSessionMemberRepository.Setup(m => m.GetCurrentSessionMember()).Returns(currentSessionMember);
        
        var mockEnvironment = Container.Resolve<Mock<IEnvironmentService>>();
        mockEnvironment.Setup(m => m.AssemblyFullName)
            .Returns(IOUtils.Combine(testDirectory.FullName, "Assembly", "ByteSync.exe"));
        
        var mockLocalAppData = Container.Resolve<Mock<ILocalApplicationDataManager>>();
        mockLocalAppData.Setup(m => m.ApplicationDataPath)
            .Returns(IOUtils.Combine(testDirectory.FullName, "AppData"));
        
        _sessionService = Container.Resolve<ISessionService>();
        var sessionSettings = SessionSettings.BuildDefault();
        sessionSettings.AnalysisMode = AnalysisModes.Smart;
        sessionSettings.DataType = DataTypes.Files;
        sessionSettings.MatchingMode = MatchingModes.Tree;
        sessionSettings.LinkingCase = LinkingCases.Insensitive;
        sessionSettings.ExcludeHiddenFiles = true;
        sessionSettings.ExcludeSystemFiles = true;
        
        var mockSessionService = Container.Resolve<Mock<ISessionService>>();
        mockSessionService.Setup(m => m.CurrentSessionSettings).Returns(sessionSettings);
        
        _inventoryService = Container.Resolve<IInventoryService>();
        _cloudSessionLocalDataManager = Container.Resolve<ICloudSessionLocalDataManager>();
        _dataNodeRepository = Container.Resolve<IDataNodeRepository>();
        _dataSourceRepository = Container.Resolve<IDataSourceRepository>();
        _sessionMemberRepository = Container.Resolve<ISessionMemberRepository>();
        
        _sessionMemberServiceMock = Container.Resolve<Mock<ISessionMemberService>>();
        _inventoryFinishedServiceMock = Container.Resolve<Mock<IInventoryFinishedService>>();
        
        _inventoryProcessData = _inventoryService.InventoryProcessData;
        
        var logger = Container.Resolve<ILogger<FullInventoryRunner>>();
        
        _fullInventoryRunner = new FullInventoryRunner(
            _inventoryFinishedServiceMock.Object,
            _inventoryService,
            _cloudSessionLocalDataManager,
            _inventoryComparerFactoryMock.Object,
            _sessionMemberServiceMock.Object,
            logger);
    }
    
    [Test]
    public async Task RunFullInventory_WithNoFilesToAnalyze_CompletesSuccessfully()
    {
        var sourceDir = CreateTestDirectory("source1");
        _testDirectoryService.CreateFileInDirectory(sourceDir.FullName, "file1.txt", "same content");
        
        var dataNode = CreateDataNode("node1", "A");
        _dataNodeRepository.AddOrUpdate([dataNode]);
        
        var dataSource = new DataSource
            { Id = Guid.NewGuid().ToString(), DataNodeId = dataNode.Id, Path = sourceDir.FullName, Code = "A1" };
        _dataSourceRepository.AddOrUpdate([dataSource]);
        
        var inventoryBuilder = CreateInventoryBuilder(dataNode, [dataSource]);
        
        var baseInventoryPath = _cloudSessionLocalDataManager.GetCurrentMachineInventoryPath(
            inventoryBuilder.Inventory, LocalInventoryModes.Base);
        await inventoryBuilder.BuildBaseInventoryAsync(baseInventoryPath);
        
        await _inventoryService.SetLocalInventory(
            [new InventoryFile(BuildSharedFileDefinition(inventoryBuilder.Inventory, LocalInventoryModes.Base), baseInventoryPath)],
            LocalInventoryModes.Base);
        
        SetupInventoryComparerMock(inventoryBuilder, []);
        
        _inventoryProcessData.InventoryBuilders = [inventoryBuilder];
        
        var result = await _fullInventoryRunner.RunFullInventory();
        
        result.Should().BeTrue();
        var monitorData3 = await _inventoryProcessData.InventoryMonitorObservable.FirstAsync();
        monitorData3.AnalyzableFiles.Should().Be(0);
        _inventoryFinishedServiceMock.Verify(
            x => x.SetLocalInventoryFinished(It.IsAny<List<Inventory>>(), LocalInventoryModes.Full),
            Times.Once);
    }
    
    private void SetupInventoryComparerMock(IInventoryBuilder inventoryBuilder, List<string> filesToAnalyze)
    {
        var comparerMock = new Mock<IInventoryComparer>();
        
        var comparisonResult = new ComparisonResult();
        comparisonResult.Inventories.Add(inventoryBuilder.Inventory);
        
        var inventoryPart = inventoryBuilder.Inventory.InventoryParts.First();
        
        foreach (var fileRelativePath in filesToAnalyze)
        {
            var pathIdentity = new PathIdentity(FileSystemTypes.File, fileRelativePath, Path.GetFileName(fileRelativePath),
                fileRelativePath);
            var fileInfo = new FileInfo(IOUtils.Combine(inventoryPart.RootPath, fileRelativePath.TrimStart('/', '\\')));
            var fileDescription = new FileDescription
            {
                InventoryPart = inventoryPart,
                RelativePath = fileRelativePath,
                Size = fileInfo.Exists ? fileInfo.Length : 0
            };
            inventoryBuilder.Indexer.Register(fileDescription, pathIdentity);
            
            var comparisonItem = new ComparisonItem(pathIdentity);
            
            var contentIdentity1 = new ContentIdentity(new ContentIdentityCore { Size = 100 });
            var contentIdentity2 = new ContentIdentity(new ContentIdentityCore { Size = 200 });
            
            contentIdentity1.InventoryPartsByLastWriteTimes[DateTime.UtcNow] = [inventoryPart];
            
            comparisonItem.AddContentIdentity(contentIdentity1);
            comparisonItem.AddContentIdentity(contentIdentity2);
            
            comparisonResult.AddItem(comparisonItem);
        }
        
        comparerMock.Setup(x => x.Compare()).Returns(comparisonResult);
        
        _inventoryComparerFactoryMock
            .Setup(x => x.CreateInventoryComparer(It.IsAny<LocalInventoryModes>(), It.IsAny<IInventoryIndexer>()))
            .Returns(comparerMock.Object);
    }
    
    private DirectoryInfo CreateTestDirectory(string name)
    {
        var dir = new DirectoryInfo(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, name));
        dir.Create();
        
        return dir;
    }
    
    private DataNode CreateDataNode(string id, string code)
    {
        var currentClientInstanceId = Container.Resolve<Mock<IEnvironmentService>>().Object.ClientInstanceId;
        
        return new DataNode
        {
            Id = id,
            ClientInstanceId = currentClientInstanceId,
            Code = code,
            OrderIndex = 1
        };
    }
    
    private IInventoryBuilder CreateInventoryBuilder(DataNode dataNode, IEnumerable<DataSource> dataSources)
    {
        var sessionMember = _sessionMemberRepository.GetCurrentSessionMember();
        var sessionSettings = _sessionService.CurrentSessionSettings!;
        var processData = _inventoryService.InventoryProcessData;
        
        var inventoryBuilderLogger = new Mock<ILogger<InventoryBuilder>>().Object;
        var inventoryFileAnalyzerLogger = new Mock<ILogger<InventoryFileAnalyzer>>().Object;
        
        var saver = new InventorySaver();
        var analyzer = new InventoryFileAnalyzer(FingerprintModes.Rsync, processData, saver, inventoryFileAnalyzerLogger);
        
        var inventoryBuilder = new InventoryBuilder(
            sessionMember,
            dataNode,
            sessionSettings,
            processData,
            OSPlatforms.Windows,
            FingerprintModes.Rsync,
            inventoryBuilderLogger,
            analyzer,
            saver,
            new InventoryIndexer());
        
        foreach (var dataSource in dataSources)
        {
            inventoryBuilder.AddInventoryPart(dataSource);
        }
        
        return inventoryBuilder;
    }
    
    private SharedFileDefinition BuildSharedFileDefinition(
        Inventory inventory,
        LocalInventoryModes localInventoryMode)
    {
        return new SharedFileDefinition
        {
            SessionId = _sessionService.SessionId!,
            ClientInstanceId = inventory.Endpoint.ClientInstanceId,
            SharedFileType = localInventoryMode == LocalInventoryModes.Base
                ? SharedFileTypes.BaseInventory
                : SharedFileTypes.FullInventory,
            AdditionalName = inventory.CodeAndId
        };
    }
}