using System;
using System.Threading.Tasks;
using ByteSync.Business.PathItems;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Controls.Sessions;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Services.Inventories;
using ByteSync.Tests.TestUtilities.Helpers;
using ByteSync.Tests.TestUtilities.Mock;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace ByteSync.Tests.Controls.Inventories;

public class PathItemsServiceTests
{
    private Mock<ISessionService> _mockSessionService;
    private Mock<IPathItemChecker> _mockPathItemChecker;
    private Mock<IDataEncrypter> _mockDataEncrypter;
    private Mock<IConnectionService> _mockConnectionService;
    private Mock<IInventoryApiClient> _mockInventoryApiClient;
    private Mock<IPathItemRepository> _mockPathItemRepository;
    private Mock<ISessionMemberRepository> _mockSessionMemberRepository;
    
    private PathItemsService _pathItemsService;

    [SetUp]
    public void SetUp()
    {
        _mockSessionService = new Mock<ISessionService>();
        _mockPathItemChecker = new Mock<IPathItemChecker>();
        _mockDataEncrypter = new Mock<IDataEncrypter>();
        _mockConnectionService = new Mock<IConnectionService>();
        _mockInventoryApiClient = new Mock<IInventoryApiClient>();
        _mockPathItemRepository = new Mock<IPathItemRepository>();
        _mockSessionMemberRepository = new Mock<ISessionMemberRepository>();

        _pathItemsService = new PathItemsService(
            _mockSessionService.Object,
            _mockPathItemChecker.Object,
            _mockDataEncrypter.Object,
            _mockConnectionService.Object,
            _mockInventoryApiClient.Object,
            _mockPathItemRepository.Object,
            _mockSessionMemberRepository.Object
        );
    }
    /*
    [Test]
    public async Task AddPathItem_ValidPathItem_AddsPathItemToCache()
    {
        // Arrange
        var pathItem = new PathItem
        {
            Path = "/test/path",
            Type = FileSystemTypes.File,
            ClientInstanceId = Guid.NewGuid().ToString(),
            Code = "A1"
        };

        var encryptedPathItem = new EncryptedPathItem();

        _mockPathItemChecker.Setup(x => x.CheckPathItem(It.IsAny<PathItem>())).ReturnsAsync(true);
        _mockSessionService.Setup(x => x.CurrentSession).Returns(new CloudSession());
        _mockDataEncrypter.Setup(x => x.EncryptPathItem(It.IsAny<PathItem>())).Returns(encryptedPathItem);
        _mockConnectionManager.Setup(x => x.HubWrapper.SetPathItemAdded(It.IsAny<string>(), It.IsAny<EncryptedPathItem>()))
            .ReturnsAsync(true)
            .Verifiable();
        _mockConnectionManager.SetupGetCurrentEndpoint("CID0");

        // Act
        await _pathItemsService.AddPathItem(pathItem);

        // Assert
        ClassicAssert.AreEqual(1, _pathItemsService.AllPathItems.Count);

        _mockConnectionManager.Verify();
    }

    [Test]
    public async Task AddPathItem_ValidPathItem_AddsPathItemToCache_2()
    {
        var encryptedPathItem = new EncryptedPathItem();

        _mockPathItemChecker.Setup(x => x.CheckPathItem(It.IsAny<PathItem>())).ReturnsAsync(true)
            .Verifiable();
        _mockSessionService.Setup(x => x.CurrentSession).Returns(new CloudSession())
            .Verifiable();
        _mockDataEncrypter.Setup(x => x.EncryptPathItem(It.IsAny<PathItem>()))
            .Returns(encryptedPathItem)
            .Verifiable();
        _mockSessionMembersService.Setup(x => x.GetCurrentSessionMember())
            .Returns(new SessionMemberInfo())
            .Verifiable();
        _mockConnectionManager.Setup(x => x.HubWrapper.SetPathItemAdded(It.IsAny<string>(), It.IsAny<EncryptedPathItem>()))
            .ReturnsAsync(true)
            .Verifiable();
        _mockConnectionManager.SetupGetCurrentEndpoint("CID0");

        // Act
        await _pathItemsService.CreateAndAddPathItem("/test/path", FileSystemTypes.Directory);

        // Assert
        ClassicAssert.AreEqual(1, service.AllPathItems.Count);
        var pathItem = service.AllPathItems.Items.First();
        ClassicAssert.AreEqual("/test/path", pathItem.Path);
        ClassicAssert.AreEqual(FileSystemTypes.Directory, pathItem.Type);
        ClassicAssert.AreEqual("A1", pathItem.Code);

        _mockSessionMembersService.Verify();
        _mockConnectionManager.Verify();
    }

    [Test]
    public async Task AddPathItem_ValidPathItem_AddsPathItemToCache_3()
    {
        var encryptedPathItem = new EncryptedPathItem();

        _mockPathItemChecker.Setup(x => x.CheckPathItem(It.IsAny<PathItem>())).ReturnsAsync(true)
            .Verifiable();
        _mockSessionService.Setup(x => x.CurrentSession).Returns(new CloudSession())
            .Verifiable();
        _mockDataEncrypter.Setup(x => x.EncryptPathItem(It.IsAny<PathItem>()))
            .Returns(encryptedPathItem)
            .Verifiable();
        var sessionMemberInfo = new SessionMemberInfo { Endpoint = ByteSyncEndPointHelper.BuildEndPoint("CID0") };
        _mockSessionMembersService.Setup(x => x.GetCurrentSessionMember())
            .Returns(sessionMemberInfo)
            .Verifiable();
        _mockConnectionManager.Setup(x => x.HubWrapper.SetPathItemAdded(It.IsAny<string>(), It.IsAny<EncryptedPathItem>()))
            .ReturnsAsync(true)
            .Verifiable();
        _mockConnectionManager.SetupGetCurrentEndpoint("CID0");

        // Act
        await _pathItemsService.CreateAndAddPathItem("/test/path1", FileSystemTypes.Directory);
        await _pathItemsService.CreateAndAddPathItem("/test/path2", FileSystemTypes.File);
        await _pathItemsService.CreateAndAddPathItem("/test/path3", FileSystemTypes.Directory);

        // Assert
        ClassicAssert.AreEqual(3, service.AllPathItems.Count);
        var allPathItems = service.AllPathItems.Items.ToList();

        var pathItem = allPathItems[0];
        ClassicAssert.AreEqual("/test/path1", pathItem.Path);
        ClassicAssert.AreEqual(FileSystemTypes.Directory, pathItem.Type);
        ClassicAssert.AreEqual("A1", pathItem.Code);

        pathItem = allPathItems[1];
        ClassicAssert.AreEqual("/test/path2", pathItem.Path);
        ClassicAssert.AreEqual(FileSystemTypes.File, pathItem.Type);
        ClassicAssert.AreEqual("A2", pathItem.Code);

        pathItem = allPathItems[2];
        ClassicAssert.AreEqual("/test/path3", pathItem.Path);
        ClassicAssert.AreEqual(FileSystemTypes.Directory, pathItem.Type);
        ClassicAssert.AreEqual("A3", pathItem.Code);

        _mockSessionMembersService.Verify();
        _mockConnectionManager.Verify();
    }*/
}