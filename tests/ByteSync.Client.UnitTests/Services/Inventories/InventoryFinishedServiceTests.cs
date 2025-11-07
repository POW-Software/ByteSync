using ByteSync.Business;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.Common.Business.Sessions.Local;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.Inventories;
using ByteSync.Services.Inventories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Inventories;

[TestFixture]
public class InventoryFinishedServiceTests
{
    private Mock<ISessionService> _mockSessionService = null!;
    private Mock<ICloudSessionLocalDataManager> _mockCloudSessionLocalDataManager = null!;
    private Mock<IFileUploaderFactory> _mockFileUploaderFactory = null!;
    private Mock<IInventoryService> _mockInventoryService = null!;
    private Mock<ISessionMemberService> _mockSessionMemberService = null!;
    private Mock<ILogger<InventoryFinishedService>> _mockLogger = null!;
    private InventoryFinishedService _service = null!;
    
    [SetUp]
    public void SetUp()
    {
        _mockSessionService = new Mock<ISessionService>();
        _mockCloudSessionLocalDataManager = new Mock<ICloudSessionLocalDataManager>();
        _mockFileUploaderFactory = new Mock<IFileUploaderFactory>();
        _mockInventoryService = new Mock<IInventoryService>();
        _mockSessionMemberService = new Mock<ISessionMemberService>();
        _mockLogger = new Mock<ILogger<InventoryFinishedService>>();
        
        _service = new InventoryFinishedService(
            _mockSessionService.Object,
            _mockCloudSessionLocalDataManager.Object,
            _mockFileUploaderFactory.Object,
            _mockInventoryService.Object,
            _mockSessionMemberService.Object,
            _mockLogger.Object);
    }
    
    [Test]
    public async Task SetLocalInventoryFinished_WithLocalSession_ShouldNotUploadFiles()
    {
        // Arrange
        var localSession = new LocalSession { SessionId = "local-session-1" };
        _mockSessionService.Setup(x => x.CurrentSession).Returns(localSession);
        
        var inventories = CreateTestInventories(1);
        var localInventoryMode = LocalInventoryModes.Base;
        
        _mockCloudSessionLocalDataManager
            .Setup(x => x.GetCurrentMachineInventoryPath(It.IsAny<Inventory>(), localInventoryMode))
            .Returns("C:\\test\\inventory.bin");
        
        var inventoryProcessData = new InventoryProcessData();
        _mockInventoryService.Setup(x => x.InventoryProcessData).Returns(inventoryProcessData);
        
        // Act
        await _service.SetLocalInventoryFinished(inventories, localInventoryMode);
        
        // Assert
        _mockFileUploaderFactory.Verify(x => x.Build(It.IsAny<string>(), It.IsAny<SharedFileDefinition>()), Times.Never);
        _mockInventoryService.Verify(x => x.SetLocalInventory(It.IsAny<List<InventoryFile>>(), localInventoryMode), Times.Once);
        _mockSessionMemberService.Verify(x => x.UpdateCurrentMemberGeneralStatus(It.IsAny<SessionMemberGeneralStatus>()), Times.Once);
    }
    
    [Test]
    public async Task SetLocalInventoryFinished_WithCloudSession_ShouldUploadFiles()
    {
        // Arrange
        var cloudSession = new CloudSession { SessionId = "cloud-session-1" };
        _mockSessionService.Setup(x => x.CurrentSession).Returns(cloudSession);
        
        var inventories = CreateTestInventories(2);
        var localInventoryMode = LocalInventoryModes.Full;
        
        _mockCloudSessionLocalDataManager
            .Setup(x => x.GetCurrentMachineInventoryPath(It.IsAny<Inventory>(), localInventoryMode))
            .Returns("C:\\test\\inventory.bin");
        
        var inventoryProcessData = new InventoryProcessData();
        _mockInventoryService.Setup(x => x.InventoryProcessData).Returns(inventoryProcessData);
        
        var mockFileUploader = new Mock<IFileUploader>();
        _mockFileUploaderFactory
            .Setup(x => x.Build(It.IsAny<string>(), It.IsAny<SharedFileDefinition>()))
            .Returns(mockFileUploader.Object);
        
        // Act
        await _service.SetLocalInventoryFinished(inventories, localInventoryMode);
        
        // Assert
        _mockFileUploaderFactory.Verify(
            x => x.Build(It.IsAny<string>(), It.IsAny<SharedFileDefinition>()),
            Times.Exactly(2));
        mockFileUploader.Verify(x => x.Upload(), Times.Exactly(2));
        _mockInventoryService.Verify(x => x.SetLocalInventory(It.IsAny<List<InventoryFile>>(), localInventoryMode), Times.Once);
    }
    
    [Test]
    public async Task SetLocalInventoryFinished_WithCloudSession_BaseMode_ShouldSetCorrectSharedFileType()
    {
        // Arrange
        var cloudSession = new CloudSession { SessionId = "cloud-session-1" };
        _mockSessionService.Setup(x => x.CurrentSession).Returns(cloudSession);
        
        var inventories = CreateTestInventories(1);
        var localInventoryMode = LocalInventoryModes.Base;
        
        _mockCloudSessionLocalDataManager
            .Setup(x => x.GetCurrentMachineInventoryPath(It.IsAny<Inventory>(), localInventoryMode))
            .Returns("C:\\test\\inventory.bin");
        
        var inventoryProcessData = new InventoryProcessData();
        _mockInventoryService.Setup(x => x.InventoryProcessData).Returns(inventoryProcessData);
        
        var mockFileUploader = new Mock<IFileUploader>();
        
        SharedFileDefinition? capturedDefinition = null;
        _mockFileUploaderFactory
            .Setup(x => x.Build(It.IsAny<string>(), It.IsAny<SharedFileDefinition>()))
            .Callback<string, SharedFileDefinition>((_, def) => capturedDefinition = def)
            .Returns(mockFileUploader.Object);
        
        // Act
        await _service.SetLocalInventoryFinished(inventories, localInventoryMode);
        
        // Assert
        capturedDefinition.Should().NotBeNull();
        capturedDefinition!.SharedFileType.Should().Be(SharedFileTypes.BaseInventory);
    }
    
    [Test]
    public async Task SetLocalInventoryFinished_WithCloudSession_FullMode_ShouldSetCorrectSharedFileType()
    {
        // Arrange
        var cloudSession = new CloudSession { SessionId = "cloud-session-1" };
        _mockSessionService.Setup(x => x.CurrentSession).Returns(cloudSession);
        
        var inventories = CreateTestInventories(1);
        var localInventoryMode = LocalInventoryModes.Full;
        
        _mockCloudSessionLocalDataManager
            .Setup(x => x.GetCurrentMachineInventoryPath(It.IsAny<Inventory>(), localInventoryMode))
            .Returns("C:\\test\\inventory.bin");
        
        var inventoryProcessData = new InventoryProcessData();
        _mockInventoryService.Setup(x => x.InventoryProcessData).Returns(inventoryProcessData);
        
        var mockFileUploader = new Mock<IFileUploader>();
        
        SharedFileDefinition? capturedDefinition = null;
        _mockFileUploaderFactory
            .Setup(x => x.Build(It.IsAny<string>(), It.IsAny<SharedFileDefinition>()))
            .Callback<string, SharedFileDefinition>((_, def) => capturedDefinition = def)
            .Returns(mockFileUploader.Object);
        
        // Act
        await _service.SetLocalInventoryFinished(inventories, localInventoryMode);
        
        // Assert
        capturedDefinition.Should().NotBeNull();
        capturedDefinition!.SharedFileType.Should().Be(SharedFileTypes.FullInventory);
    }
    
    [Test]
    public async Task SetLocalInventoryFinished_ShouldSetSharedFileDefinitionProperties()
    {
        // Arrange
        var cloudSession = new CloudSession { SessionId = "cloud-session-1" };
        _mockSessionService.Setup(x => x.CurrentSession).Returns(cloudSession);
        
        var inventory = new Inventory
        {
            Endpoint = new ByteSyncEndpoint { ClientInstanceId = "client-123" },
            Code = "A",
            InventoryId = "1"
        };
        var inventories = new List<Inventory> { inventory };
        var localInventoryMode = LocalInventoryModes.Base;
        
        _mockCloudSessionLocalDataManager
            .Setup(x => x.GetCurrentMachineInventoryPath(inventory, localInventoryMode))
            .Returns("C:\\test\\inventory.bin");
        
        var inventoryProcessData = new InventoryProcessData();
        _mockInventoryService.Setup(x => x.InventoryProcessData).Returns(inventoryProcessData);
        
        var mockFileUploader = new Mock<IFileUploader>();
        
        SharedFileDefinition? capturedDefinition = null;
        _mockFileUploaderFactory
            .Setup(x => x.Build(It.IsAny<string>(), It.IsAny<SharedFileDefinition>()))
            .Callback<string, SharedFileDefinition>((_, def) => capturedDefinition = def)
            .Returns(mockFileUploader.Object);
        
        // Act
        await _service.SetLocalInventoryFinished(inventories, localInventoryMode);
        
        // Assert
        capturedDefinition.Should().NotBeNull();
        capturedDefinition!.ClientInstanceId.Should().Be("client-123");
        capturedDefinition.SessionId.Should().Be("cloud-session-1");
        capturedDefinition.AdditionalName.Should().Be("A_1");
    }
    
    [Test]
    public async Task SetLocalInventoryFinished_WithMultipleInventories_ShouldProcessAll()
    {
        // Arrange
        var cloudSession = new CloudSession { SessionId = "cloud-session-1" };
        _mockSessionService.Setup(x => x.CurrentSession).Returns(cloudSession);
        
        var inventories = CreateTestInventories(3);
        var localInventoryMode = LocalInventoryModes.Base;
        
        _mockCloudSessionLocalDataManager
            .Setup(x => x.GetCurrentMachineInventoryPath(It.IsAny<Inventory>(), localInventoryMode))
            .Returns("C:\\test\\inventory.bin");
        
        var inventoryProcessData = new InventoryProcessData();
        _mockInventoryService.Setup(x => x.InventoryProcessData).Returns(inventoryProcessData);
        
        var mockFileUploader = new Mock<IFileUploader>();
        _mockFileUploaderFactory
            .Setup(x => x.Build(It.IsAny<string>(), It.IsAny<SharedFileDefinition>()))
            .Returns(mockFileUploader.Object);
        
        List<InventoryFile>? capturedInventoryFiles = null;
        _mockInventoryService
            .Setup(x => x.SetLocalInventory(It.IsAny<List<InventoryFile>>(), localInventoryMode))
            .Callback<ICollection<InventoryFile>, LocalInventoryModes>((files, _) => capturedInventoryFiles = files.ToList())
            .Returns(Task.CompletedTask);
        
        // Act
        await _service.SetLocalInventoryFinished(inventories, localInventoryMode);
        
        // Assert
        capturedInventoryFiles.Should().NotBeNull();
        capturedInventoryFiles!.Count.Should().Be(3);
        _mockFileUploaderFactory.Verify(
            x => x.Build(It.IsAny<string>(), It.IsAny<SharedFileDefinition>()),
            Times.Exactly(3));
    }
    
    [Test]
    public async Task SetLocalInventoryFinished_ShouldUpdateMemberGeneralStatus()
    {
        // Arrange
        var cloudSession = new CloudSession { SessionId = "cloud-session-1" };
        _mockSessionService.Setup(x => x.CurrentSession).Returns(cloudSession);
        
        var inventories = CreateTestInventories(1);
        var localInventoryMode = LocalInventoryModes.Base;
        
        _mockCloudSessionLocalDataManager
            .Setup(x => x.GetCurrentMachineInventoryPath(It.IsAny<Inventory>(), localInventoryMode))
            .Returns("C:\\test\\inventory.bin");
        
        var inventoryProcessData = new InventoryProcessData();
        _mockInventoryService.Setup(x => x.InventoryProcessData).Returns(inventoryProcessData);
        
        var mockFileUploader = new Mock<IFileUploader>();
        _mockFileUploaderFactory
            .Setup(x => x.Build(It.IsAny<string>(), It.IsAny<SharedFileDefinition>()))
            .Returns(mockFileUploader.Object);
        
        SessionMemberGeneralStatus? capturedStatus = null;
        _mockSessionMemberService
            .Setup(x => x.UpdateCurrentMemberGeneralStatus(It.IsAny<SessionMemberGeneralStatus>()))
            .Callback<SessionMemberGeneralStatus>(status => capturedStatus = status)
            .Returns(Task.CompletedTask);
        
        // Act
        await _service.SetLocalInventoryFinished(inventories, localInventoryMode);
        
        // Assert
        _mockSessionMemberService.Verify(x => x.UpdateCurrentMemberGeneralStatus(It.IsAny<SessionMemberGeneralStatus>()), Times.Once);
        capturedStatus.Should().NotBeNull();
    }
    
    [Test]
    public async Task SetLocalInventoryFinished_WhenFileNotFound_ShouldNotThrowAndContinue()
    {
        // Arrange
        var cloudSession = new CloudSession { SessionId = "cloud-session-1" };
        _mockSessionService.Setup(x => x.CurrentSession).Returns(cloudSession);
        
        var inventories = CreateTestInventories(1);
        var localInventoryMode = LocalInventoryModes.Base;
        
        _mockCloudSessionLocalDataManager
            .Setup(x => x.GetCurrentMachineInventoryPath(It.IsAny<Inventory>(), localInventoryMode))
            .Returns("C:\\nonexistent\\inventory.bin");
        
        var inventoryProcessData = new InventoryProcessData();
        _mockInventoryService.Setup(x => x.InventoryProcessData).Returns(inventoryProcessData);
        
        var mockFileUploader = new Mock<IFileUploader>();
        _mockFileUploaderFactory
            .Setup(x => x.Build(It.IsAny<string>(), It.IsAny<SharedFileDefinition>()))
            .Returns(mockFileUploader.Object);
        
        // Act
        var act = async () => await _service.SetLocalInventoryFinished(inventories, localInventoryMode);
        
        // Assert
        await act.Should().NotThrowAsync();
        mockFileUploader.Verify(x => x.Upload(), Times.Once);
        _mockInventoryService.Verify(x => x.SetLocalInventory(It.IsAny<List<InventoryFile>>(), localInventoryMode), Times.Once);
    }
    
    [Test]
    public async Task SetLocalInventoryFinished_WithEmptyInventoryList_ShouldNotThrow()
    {
        // Arrange
        var cloudSession = new CloudSession { SessionId = "cloud-session-1" };
        _mockSessionService.Setup(x => x.CurrentSession).Returns(cloudSession);
        
        var inventories = new List<Inventory>();
        var localInventoryMode = LocalInventoryModes.Base;
        
        var inventoryProcessData = new InventoryProcessData();
        _mockInventoryService.Setup(x => x.InventoryProcessData).Returns(inventoryProcessData);
        
        // Act
        var act = async () => await _service.SetLocalInventoryFinished(inventories, localInventoryMode);
        
        // Assert
        await act.Should().NotThrowAsync();
        _mockFileUploaderFactory.Verify(x => x.Build(It.IsAny<string>(), It.IsAny<SharedFileDefinition>()), Times.Never);
    }
    
    private static List<Inventory> CreateTestInventories(int count)
    {
        var inventories = new List<Inventory>();
        for (var i = 0; i < count; i++)
        {
            inventories.Add(new Inventory
            {
                Endpoint = new ByteSyncEndpoint { ClientInstanceId = $"client-{i}" },
                Code = ((char)('A' + i)).ToString(),
                InventoryId = (i + 1).ToString()
            });
        }
        
        return inventories;
    }
}