using Autofac;
using ByteSync.Business.PathItems;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Helpers;
using ByteSync.Factories;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Synchronizations;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Repositories;
using ByteSync.Services.Inventories;
using ByteSync.Services.Sessions;
using ByteSync.Services.Synchronizations;
using ByteSync.TestsCommon;
using FluentAssertions;
using Moq;

namespace ByteSync.Client.IntegrationTests.Services.Inventories;

public class TestPathItemsService : IntegrationTest
{
    private ByteSyncEndpoint _currentEndPoint;
    private PathItemsService _pathItemsService;
    
    [SetUp]
    public void SetUp()
    {
        RegisterType<PathItemRepository, IPathItemRepository>();
        RegisterType<SessionMemberRepository, ISessionMemberRepository>();
        RegisterType<PathItemsService>();
        
        RegisterType<DeltaManager, IDeltaManager>();
        RegisterType<CloudSessionLocalDataManager, ICloudSessionLocalDataManager>();
        RegisterType<TemporaryFileManagerFactory, ITemporaryFileManagerFactory>();
        RegisterType<TemporaryFileManager, ITemporaryFileManager>();
        RegisterType<FileDatesSetter, IFileDatesSetter>();
        RegisterType<ComparisonResultPreparer>();
        RegisterType<SynchronizationActionHandler>();
        BuildMoqContainer();

        var contextHelper = new TestContextGenerator(Container);
        contextHelper.GenerateSession();
        _currentEndPoint = contextHelper.GenerateCurrentEndpoint();
        var testDirectory = _testDirectoryService.CreateTestDirectory();

        var mockEnvironmentService = Container.Resolve<Mock<IEnvironmentService>>();
        mockEnvironmentService.Setup(m => m.AssemblyFullName).Returns(IOUtils.Combine(testDirectory.FullName, "Assembly", "Assembly.exe"));

        var mockLocalApplicationDataManager = Container.Resolve<Mock<ILocalApplicationDataManager>>();
        mockLocalApplicationDataManager.Setup(m => m.ApplicationDataPath).Returns(IOUtils.Combine(testDirectory.FullName, 
            "ApplicationDataPath"));

        _pathItemsService = Container.Resolve<PathItemsService>();
    }
    
    
    [Test]
    public async Task TryAddPathItem_ShouldAddToRepository_WhenCheckPasses()
    {
        var pathItemChecker = Container.Resolve<Mock<IPathItemChecker>>();
        var pathItemRepository = Container.Resolve<IPathItemRepository>();
        
        pathItemChecker
            .Setup(x => x.CheckPathItem(It.IsAny<PathItem>(), It.IsAny<IEnumerable<PathItem>>()))
            .ReturnsAsync(true);
        // _connectionService.SetupGet(x => x.ClientInstanceId).Returns("TestClient");
        // _sessionService.SetupGet(x => x.CurrentSession).Returns(new CloudSession { SessionId = "Session1" });
        // _inventoryApiClient.Setup(x => x.AddPathItem("Session1", It.IsAny<EncryptedPathItem>()))
        //     .ReturnsAsync(true);

        var pathItem = new PathItem { ClientInstanceId = "TestClient" };
        var result = await _pathItemsService.TryAddPathItem(pathItem);
        
        result.Should().BeTrue();
        pathItemRepository.Elements.Should().Contain(pathItem);

        // _pathItemRepository.Verify(repo => repo.AddOrUpdate(It.IsAny<PathItem>()), Times.Once);
    }
}