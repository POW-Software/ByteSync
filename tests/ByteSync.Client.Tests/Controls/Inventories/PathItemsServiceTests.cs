using ByteSync.Business.PathItems;
using ByteSync.Business.SessionMembers;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Inventories;
using DynamicData;
using DynamicData.Binding;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Controls.Inventories;

[TestFixture]
public class PathItemsServiceTests
{
    private Mock<ISessionService> _sessionService;
    private Mock<IPathItemChecker> _pathItemChecker;
    private Mock<IDataEncrypter> _dataEncrypter;
    private Mock<IConnectionService> _connectionService;
    private Mock<IInventoryApiClient> _inventoryApiClient;
    private Mock<IPathItemRepository> _pathItemRepository;
    private Mock<ISessionMemberRepository> _sessionMemberRepository;
    private PathItemsService _service;

    [SetUp]
    public void Setup()
    {
        var mockObservable = new SourceCache<SessionMemberInfo, string>(r => r.ClientInstanceId).Connect()
            .Sort(SortExpressionComparer<SessionMemberInfo>.Ascending(smi => smi.JoinedSessionOn),
            SortOptimisations.ComparesImmutableValuesOnly);

        _sessionMemberRepository = new Mock<ISessionMemberRepository>();
        _sessionMemberRepository
            .SetupGet(x => x.SortedSessionMembersObservable)
            .Returns(mockObservable);
        
        _sessionService = new Mock<ISessionService>();
        _pathItemChecker = new Mock<IPathItemChecker>();
        _dataEncrypter = new Mock<IDataEncrypter>();
        _connectionService = new Mock<IConnectionService>();
        _inventoryApiClient = new Mock<IInventoryApiClient>();
        _pathItemRepository = new Mock<IPathItemRepository>();
        _sessionMemberRepository = new Mock<ISessionMemberRepository>();

        _service = new PathItemsService(
            _sessionService.Object,
            _pathItemChecker.Object,
            _dataEncrypter.Object,
            _connectionService.Object,
            _inventoryApiClient.Object,
            _pathItemRepository.Object,
            _sessionMemberRepository.Object
        );
    }

    [Test]
    public async Task TryAddPathItem_ShouldAddToRepository_WhenCheckPasses()
    {
        _pathItemChecker
            .Setup(x => x.CheckPathItem(It.IsAny<PathItem>(), It.IsAny<IEnumerable<PathItem>>()))
            .ReturnsAsync(true);
        _connectionService.SetupGet(x => x.ClientInstanceId).Returns("TestClient");
        _sessionService.SetupGet(x => x.CurrentSession).Returns(new CloudSession { SessionId = "Session1" });
        _inventoryApiClient.Setup(x => x.AddPathItem("Session1", It.IsAny<EncryptedPathItem>()))
            .ReturnsAsync(true);

        var pathItem = new PathItem { ClientInstanceId = "TestClient" };
        await _service.TryAddPathItem(pathItem);

        _pathItemRepository.Verify(repo => repo.AddOrUpdate(It.IsAny<PathItem>()), Times.Once);
    }

    [Test]
    public async Task TryRemovePathItem_ShouldRemoveFromRepository_WhenApiCallSucceeds()
    {
        var pathItem = new PathItem();
        _dataEncrypter.Setup(x => x.EncryptPathItem(pathItem)).Returns(new EncryptedPathItem());
        _sessionService.SetupGet(x => x.SessionId).Returns("Session1");
        _inventoryApiClient.Setup(x => x.RemovePathItem("Session1", It.IsAny<EncryptedPathItem>()))
            .ReturnsAsync(true);

        await _service.TryRemovePathItem(pathItem);

        _pathItemRepository.Verify(repo => repo.Remove(pathItem), Times.Once);
    }

    [Test]
    public async Task TryAddPathItem_ShouldNotAdd_WhenCheckFails()
    {
        _pathItemChecker
            .Setup(x => x.CheckPathItem(It.IsAny<PathItem>(), It.IsAny<IEnumerable<PathItem>>()))
            .ReturnsAsync(false);

        var pathItem = new PathItem();
        await _service.TryAddPathItem(pathItem);

        _pathItemRepository.Verify(repo => repo.AddOrUpdate(It.IsAny<PathItem>()), Times.Never);
    }

    [Test]
    public async Task CreateAndTryAddPathItem_ShouldGenerateCodeAndAdd()
    {
        _connectionService.SetupGet(x => x.ClientInstanceId).Returns("TestClient");
        _sessionMemberRepository
            .Setup(x => x.GetCurrentSessionMember())
            .Returns(new SessionMemberInfo
            {
                Endpoint = new ByteSyncEndpoint { ClientInstanceId = "TestClient" }, PrivateData = new SessionMemberPrivateData { MachineName = "TestMachine" },
                PositionInList = 0
            });
        _pathItemChecker
            .Setup(x => x.CheckPathItem(It.IsAny<PathItem>(), It.IsAny<IEnumerable<PathItem>>()))
            .ReturnsAsync(true);
        _sessionService.SetupGet(x => x.CurrentSession).Returns(new CloudSession { SessionId = "Session1" });
        _inventoryApiClient.Setup(x => x.AddPathItem("Session1", It.IsAny<EncryptedPathItem>()))
            .ReturnsAsync(true);

        await _service.CreateAndTryAddPathItem("C:\\testpath", FileSystemTypes.File);

        _pathItemRepository.Verify(repo => repo.AddOrUpdate(It.IsAny<PathItem>()), Times.Once);
    }
}