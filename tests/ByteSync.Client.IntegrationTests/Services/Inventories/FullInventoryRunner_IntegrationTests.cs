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
using ByteSync.Common.Business.Sessions;
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
    private IInventoryFileRepository _inventoryFileRepository = null!;
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
        _inventoryFileRepository = Container.Resolve<IInventoryFileRepository>();
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
    public async Task RunFullInventory_WithSingleDataNode_SuccessfullyAnalyzesFiles()
    {
        var sourceDir = CreateTestDirectory("source1");
        _testDirectoryService.CreateFileInDirectory(sourceDir.FullName, "file1.txt", "content1");
        _testDirectoryService.CreateFileInDirectory(sourceDir.FullName, "file2.txt", "content2 different");
        
        var dataNode = CreateDataNode("node1", "A");
        _dataNodeRepository.AddOrUpdate(new[] { dataNode });
        
        var dataSource = new DataSource
            { Id = Guid.NewGuid().ToString(), DataNodeId = dataNode.Id, Path = sourceDir.FullName, Code = "A1" };
        _dataSourceRepository.AddOrUpdate(new[] { dataSource });
        
        var inventoryBuilder = CreateInventoryBuilder(dataNode, new[] { dataSource });
        
        var baseInventoryPath = _cloudSessionLocalDataManager.GetCurrentMachineInventoryPath(
            inventoryBuilder.Inventory, LocalInventoryModes.Base);
        await inventoryBuilder.BuildBaseInventoryAsync(baseInventoryPath);
        
        await _inventoryService.SetLocalInventory(
            new[] { new InventoryFile(BuildSharedFileDefinition(inventoryBuilder.Inventory, LocalInventoryModes.Base), baseInventoryPath) },
            LocalInventoryModes.Base);
        
        await Task.Delay(100);
        _testDirectoryService.CreateFileInDirectory(sourceDir.FullName, "file3.txt", "new content");
        File.WriteAllText(Path.Combine(sourceDir.FullName, "file1.txt"), "modified content1");
        
        SetupInventoryComparerMock(inventoryBuilder, new List<string> { "/file1.txt", "/file3.txt" });
        
        _inventoryProcessData.InventoryBuilders = new List<IInventoryBuilder> { inventoryBuilder };
        
        InventoryTaskStatus? analysisStatus = null;
        _inventoryProcessData.AnalysisStatus.Subscribe(s => analysisStatus = s);
        
        var result = await _fullInventoryRunner.RunFullInventory();
        
        result.Should().BeTrue();
        analysisStatus.Should().Be(InventoryTaskStatus.Success);
        _sessionMemberServiceMock.Verify(
            x => x.UpdateCurrentMemberGeneralStatus(SessionMemberGeneralStatus.InventoryRunningAnalysis),
            Times.Once);
        _inventoryFinishedServiceMock.Verify(
            x => x.SetLocalInventoryFinished(It.IsAny<List<Inventory>>(), LocalInventoryModes.Full),
            Times.Once);
        
        var monitorData1 = await _inventoryProcessData.InventoryMonitorObservable.FirstAsync();
        monitorData1.AnalyzedFiles.Should().BeGreaterThan(0);
    }
    
    [Test]
    public async Task RunFullInventory_WithMultipleDataNodes_ProcessesInParallel()
    {
        var sourceDir1 = CreateTestDirectory("source1");
        _testDirectoryService.CreateFileInDirectory(sourceDir1.FullName, "file1a.txt", "content1a");
        _testDirectoryService.CreateFileInDirectory(sourceDir1.FullName, "file1b.txt", "content1b different");
        
        var sourceDir2 = CreateTestDirectory("source2");
        _testDirectoryService.CreateFileInDirectory(sourceDir2.FullName, "file2a.txt", "content2a");
        _testDirectoryService.CreateFileInDirectory(sourceDir2.FullName, "file2b.txt", "content2b unique");
        
        var dataNode1 = CreateDataNode("node1", "A");
        var dataNode2 = CreateDataNode("node2", "B");
        _dataNodeRepository.AddOrUpdate(new[] { dataNode1, dataNode2 });
        
        var dataSource1 = new DataSource
            { Id = Guid.NewGuid().ToString(), DataNodeId = dataNode1.Id, Path = sourceDir1.FullName, Code = "A1" };
        var dataSource2 = new DataSource
            { Id = Guid.NewGuid().ToString(), DataNodeId = dataNode2.Id, Path = sourceDir2.FullName, Code = "B1" };
        _dataSourceRepository.AddOrUpdate(new[] { dataSource1, dataSource2 });
        
        var inventoryBuilder1 = CreateInventoryBuilder(dataNode1, new[] { dataSource1 });
        var inventoryBuilder2 = CreateInventoryBuilder(dataNode2, new[] { dataSource2 });
        
        var baseInventoryPath1 = _cloudSessionLocalDataManager.GetCurrentMachineInventoryPath(
            inventoryBuilder1.Inventory, LocalInventoryModes.Base);
        var baseInventoryPath2 = _cloudSessionLocalDataManager.GetCurrentMachineInventoryPath(
            inventoryBuilder2.Inventory, LocalInventoryModes.Base);
        
        await inventoryBuilder1.BuildBaseInventoryAsync(baseInventoryPath1);
        await inventoryBuilder2.BuildBaseInventoryAsync(baseInventoryPath2);
        
        await _inventoryService.SetLocalInventory(
            new[]
            {
                new InventoryFile(BuildSharedFileDefinition(inventoryBuilder1.Inventory, LocalInventoryModes.Base), baseInventoryPath1),
                new InventoryFile(BuildSharedFileDefinition(inventoryBuilder2.Inventory, LocalInventoryModes.Base), baseInventoryPath2)
            },
            LocalInventoryModes.Base);
        
        await Task.Delay(100);
        _testDirectoryService.CreateFileInDirectory(sourceDir1.FullName, "file1c.txt", "new content");
        File.WriteAllText(Path.Combine(sourceDir1.FullName, "file1a.txt"), "modified content");
        _testDirectoryService.CreateFileInDirectory(sourceDir2.FullName, "file2c.txt", "new content");
        File.WriteAllText(Path.Combine(sourceDir2.FullName, "file2a.txt"), "modified content");
        
        SetupInventoryComparerMock(inventoryBuilder1, new List<string> { "/file1a.txt", "/file1c.txt" });
        
        _inventoryProcessData.InventoryBuilders = new List<IInventoryBuilder> { inventoryBuilder1, inventoryBuilder2 };
        
        var result = await _fullInventoryRunner.RunFullInventory();
        
        result.Should().BeTrue();
        var monitorData2 = await _inventoryProcessData.InventoryMonitorObservable.FirstAsync();
        monitorData2.AnalyzedFiles.Should().BeGreaterThan(0);
        _inventoryFinishedServiceMock.Verify(
            x => x.SetLocalInventoryFinished(It.IsAny<List<Inventory>>(), LocalInventoryModes.Full),
            Times.Once);
    }
    
    [Test]
    public async Task RunFullInventory_WhenCancelled_UpdatesStatusToCancelled()
    {
        var sourceDir = CreateTestDirectory("source1");
        for (int i = 0; i < 50; i++)
        {
            _testDirectoryService.CreateFileInDirectory(sourceDir.FullName, $"file{i}.txt", $"content{i} unique content for diff");
        }
        
        var dataNode = CreateDataNode("node1", "A");
        _dataNodeRepository.AddOrUpdate(new[] { dataNode });
        
        var dataSource = new DataSource
            { Id = Guid.NewGuid().ToString(), DataNodeId = dataNode.Id, Path = sourceDir.FullName, Code = "A1" };
        _dataSourceRepository.AddOrUpdate(new[] { dataSource });
        
        var inventoryBuilder = CreateInventoryBuilder(dataNode, new[] { dataSource });
        
        var baseInventoryPath = _cloudSessionLocalDataManager.GetCurrentMachineInventoryPath(
            inventoryBuilder.Inventory, LocalInventoryModes.Base);
        await inventoryBuilder.BuildBaseInventoryAsync(baseInventoryPath);
        
        await _inventoryService.SetLocalInventory(
            new[] { new InventoryFile(BuildSharedFileDefinition(inventoryBuilder.Inventory, LocalInventoryModes.Base), baseInventoryPath) },
            LocalInventoryModes.Base);
        
        await Task.Delay(100);
        var filesToAnalyze = new List<string>();
        for (int i = 0; i < 50; i++)
        {
            _testDirectoryService.CreateFileInDirectory(sourceDir.FullName, $"file{i}_new.txt", $"new content{i} unique");
            filesToAnalyze.Add($"/file{i}_new.txt");
        }
        
        SetupInventoryComparerMock(inventoryBuilder, filesToAnalyze);
        
        _inventoryProcessData.InventoryBuilders = new List<IInventoryBuilder> { inventoryBuilder };
        
        InventoryTaskStatus? analysisStatus = null;
        _inventoryProcessData.AnalysisStatus.Subscribe(s => analysisStatus = s);
        
        _inventoryProcessData.CancellationTokenSource.Cancel();
        
        var result = await _fullInventoryRunner.RunFullInventory();
        
        result.Should().BeTrue();
        analysisStatus.Should().Be(InventoryTaskStatus.Cancelled);
        _inventoryFinishedServiceMock.Verify(
            x => x.SetLocalInventoryFinished(It.IsAny<List<Inventory>>(), LocalInventoryModes.Full),
            Times.Never);
    }
    
    [Test]
    public async Task RunFullInventory_WhenExceptionOccurs_UpdatesStatusToError()
    {
        var sourceDir = CreateTestDirectory("source1");
        _testDirectoryService.CreateFileInDirectory(sourceDir.FullName, "file1.txt", "content1");
        
        var dataNode = CreateDataNode("node1", "A");
        _dataNodeRepository.AddOrUpdate(new[] { dataNode });
        
        var dataSource = new DataSource
            { Id = Guid.NewGuid().ToString(), DataNodeId = dataNode.Id, Path = sourceDir.FullName, Code = "A1" };
        _dataSourceRepository.AddOrUpdate(new[] { dataSource });
        
        var inventoryBuilder = CreateInventoryBuilder(dataNode, new[] { dataSource });
        
        var baseInventoryPath = _cloudSessionLocalDataManager.GetCurrentMachineInventoryPath(
            inventoryBuilder.Inventory, LocalInventoryModes.Base);
        await inventoryBuilder.BuildBaseInventoryAsync(baseInventoryPath);
        
        await _inventoryService.SetLocalInventory(
            new[] { new InventoryFile(BuildSharedFileDefinition(inventoryBuilder.Inventory, LocalInventoryModes.Base), baseInventoryPath) },
            LocalInventoryModes.Base);
        
        await Task.Delay(100);
        _testDirectoryService.CreateFileInDirectory(sourceDir.FullName, "file2.txt", "new content");
        File.WriteAllText(Path.Combine(sourceDir.FullName, "file1.txt"), "modified content");
        
        SetupInventoryComparerMock(inventoryBuilder, new List<string> { "/file1.txt", "/file2.txt" });
        
        _inventoryFinishedServiceMock.Setup(x => x.SetLocalInventoryFinished(
                It.IsAny<List<Inventory>>(),
                It.IsAny<LocalInventoryModes>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));
        
        _inventoryProcessData.InventoryBuilders = new List<IInventoryBuilder> { inventoryBuilder };
        
        Exception? capturedException = null;
        _inventoryProcessData.ErrorEvent.Subscribe(_ => capturedException = _inventoryProcessData.LastException);
        
        var result = await _fullInventoryRunner.RunFullInventory();
        
        result.Should().BeFalse();
        capturedException.Should().NotBeNull();
        capturedException.Should().BeOfType<InvalidOperationException>();
    }
    
    [Test]
    public async Task RunFullInventory_WithNoFilesToAnalyze_CompletesSuccessfully()
    {
        var sourceDir = CreateTestDirectory("source1");
        _testDirectoryService.CreateFileInDirectory(sourceDir.FullName, "file1.txt", "same content");
        
        var dataNode = CreateDataNode("node1", "A");
        _dataNodeRepository.AddOrUpdate(new[] { dataNode });
        
        var dataSource = new DataSource
            { Id = Guid.NewGuid().ToString(), DataNodeId = dataNode.Id, Path = sourceDir.FullName, Code = "A1" };
        _dataSourceRepository.AddOrUpdate(new[] { dataSource });
        
        var inventoryBuilder = CreateInventoryBuilder(dataNode, new[] { dataSource });
        
        var baseInventoryPath = _cloudSessionLocalDataManager.GetCurrentMachineInventoryPath(
            inventoryBuilder.Inventory, LocalInventoryModes.Base);
        await inventoryBuilder.BuildBaseInventoryAsync(baseInventoryPath);
        
        await _inventoryService.SetLocalInventory(
            new[] { new InventoryFile(BuildSharedFileDefinition(inventoryBuilder.Inventory, LocalInventoryModes.Base), baseInventoryPath) },
            LocalInventoryModes.Base);
        
        SetupInventoryComparerMock(inventoryBuilder, new List<string>());
        
        _inventoryProcessData.InventoryBuilders = new List<IInventoryBuilder> { inventoryBuilder };
        
        var result = await _fullInventoryRunner.RunFullInventory();
        
        result.Should().BeTrue();
        var monitorData3 = await _inventoryProcessData.InventoryMonitorObservable.FirstAsync();
        monitorData3.AnalyzableFiles.Should().Be(0);
        _inventoryFinishedServiceMock.Verify(
            x => x.SetLocalInventoryFinished(It.IsAny<List<Inventory>>(), LocalInventoryModes.Full),
            Times.Once);
    }
    
    [Test]
    public async Task RunFullInventory_UpdatesMonitorDataCorrectly()
    {
        var sourceDir = CreateTestDirectory("source1");
        _testDirectoryService.CreateFileInDirectory(sourceDir.FullName, "file1.txt", "content1");
        _testDirectoryService.CreateFileInDirectory(sourceDir.FullName, "file2.txt", "content2 different");
        _testDirectoryService.CreateFileInDirectory(sourceDir.FullName, "file3.txt", "content3 also different");
        
        var dataNode = CreateDataNode("node1", "A");
        _dataNodeRepository.AddOrUpdate(new[] { dataNode });
        
        var dataSource = new DataSource
            { Id = Guid.NewGuid().ToString(), DataNodeId = dataNode.Id, Path = sourceDir.FullName, Code = "A1" };
        _dataSourceRepository.AddOrUpdate(new[] { dataSource });
        
        var inventoryBuilder = CreateInventoryBuilder(dataNode, new[] { dataSource });
        
        var baseInventoryPath = _cloudSessionLocalDataManager.GetCurrentMachineInventoryPath(
            inventoryBuilder.Inventory, LocalInventoryModes.Base);
        await inventoryBuilder.BuildBaseInventoryAsync(baseInventoryPath);
        
        await _inventoryService.SetLocalInventory(
            new[] { new InventoryFile(BuildSharedFileDefinition(inventoryBuilder.Inventory, LocalInventoryModes.Base), baseInventoryPath) },
            LocalInventoryModes.Base);
        
        await Task.Delay(100);
        _testDirectoryService.CreateFileInDirectory(sourceDir.FullName, "file4.txt", "new content");
        _testDirectoryService.CreateFileInDirectory(sourceDir.FullName, "file5.txt", "another new content");
        File.WriteAllText(Path.Combine(sourceDir.FullName, "file1.txt"), "modified content1");
        File.WriteAllText(Path.Combine(sourceDir.FullName, "file2.txt"), "modified content2");
        
        SetupInventoryComparerMock(inventoryBuilder, new List<string> { "/file1.txt", "/file2.txt", "/file4.txt", "/file5.txt" });
        
        _inventoryProcessData.InventoryBuilders = new List<IInventoryBuilder> { inventoryBuilder };
        
        var initialMonitorData = await _inventoryProcessData.InventoryMonitorObservable.FirstAsync();
        var initialAnalyzableFiles = initialMonitorData.AnalyzableFiles;
        
        await _fullInventoryRunner.RunFullInventory();
        
        var finalMonitorData = await _inventoryProcessData.InventoryMonitorObservable.FirstAsync();
        finalMonitorData.AnalyzableFiles.Should().BeGreaterThan(initialAnalyzableFiles);
        finalMonitorData.AnalyzedFiles.Should().BeGreaterThan(0);
    }
    
    private void SetupInventoryComparerMock(IInventoryBuilder inventoryBuilder, List<string> filesToAnalyze)
    {
        var comparerMock = new Mock<IInventoryComparer>();
        
        var comparisonResult = new ComparisonResult();
        comparisonResult.Inventories.Add(inventoryBuilder.Inventory);
        
        foreach (var fileRelativePath in filesToAnalyze)
        {
            var pathIdentity = new PathIdentity(FileSystemTypes.File, fileRelativePath, Path.GetFileName(fileRelativePath),
                fileRelativePath);
            var comparisonItem = new ComparisonItem(pathIdentity);
            
            var contentIdentity1 = new ContentIdentity(new ContentIdentityCore { Size = 100 });
            var contentIdentity2 = new ContentIdentity(new ContentIdentityCore { Size = 200 });
            
            var inventoryPart = inventoryBuilder.Inventory.InventoryParts.First();
            contentIdentity1.InventoryPartsByLastWriteTimes[DateTime.UtcNow] = new HashSet<InventoryPart> { inventoryPart };
            
            comparisonItem.AddContentIdentity(contentIdentity1);
            comparisonItem.AddContentIdentity(contentIdentity2);
            
            comparisonResult.AddItem(comparisonItem);
            
            var fileInfo = new FileInfo(IOUtils.Combine(inventoryPart.RootPath, fileRelativePath.TrimStart('/', '\\')));
            var fileDescription = new FileDescription
            {
                InventoryPart = inventoryPart,
                RelativePath = fileRelativePath,
                Size = fileInfo.Exists ? fileInfo.Length : 0
            };
            inventoryBuilder.Indexer.Register(fileDescription, pathIdentity);
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