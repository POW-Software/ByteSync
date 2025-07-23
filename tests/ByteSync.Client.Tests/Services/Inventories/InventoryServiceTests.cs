using System.Reactive.Subjects;
using ByteSync.Business;
using ByteSync.Business.Communications;
using ByteSync.Business.DataNodes;
using ByteSync.Business.Inventories;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Services.Inventories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Inventories
{
    [TestFixture]
    public class InventoryServiceTests
    {
        private Mock<ISessionService> _sessionServiceMock;
        private Mock<IInventoryFileRepository> _inventoryFileRepositoryMock;
        private Mock<IDataNodeRepository> _dataNodeRepositoryMock;
        private Mock<ILogger<InventoryService>> _loggerMock;
        private InventoryService _inventoryService;

        [SetUp]
        public void Setup()
        {
            _sessionServiceMock = new Mock<ISessionService>();
            _inventoryFileRepositoryMock = new Mock<IInventoryFileRepository>();
            _dataNodeRepositoryMock = new Mock<IDataNodeRepository>();
            _loggerMock = new Mock<ILogger<InventoryService>>();

            var sessionStatusSubject = new Subject<SessionStatus>();
            _sessionServiceMock.Setup(x => x.SessionStatusObservable).Returns(sessionStatusSubject);

            _inventoryService = new InventoryService(
                _sessionServiceMock.Object,
                _inventoryFileRepositoryMock.Object,
                _dataNodeRepositoryMock.Object,
                _loggerMock.Object);
        }

        [Test]
        public async Task CheckInventoriesReady_WhenAllDataNodesHaveInventories_ShouldSetCompleteToTrue()
        {
            // Arrange
            var dataNodes = new List<DataNode>
            {
                new DataNode { Id = "node1", ClientInstanceId = "other_client_1", Code = "A" },
                new DataNode { Id = "node2", ClientInstanceId = "other_client_2", Code = "B" },
                new DataNode { Id = "node3", ClientInstanceId = "current_client_instance_id", Code = "C" }
            };

            var inventoryFiles = new List<InventoryFile>
            {
                CreateInventoryFile("other_client_1", "A", SharedFileTypes.BaseInventory),
                CreateInventoryFile("other_client_2", "B", SharedFileTypes.BaseInventory),
                CreateInventoryFile("current_client_instance_id", "C", SharedFileTypes.BaseInventory)
            };

            _dataNodeRepositoryMock.Setup(x => x.Elements).Returns(dataNodes);
            _inventoryFileRepositoryMock.Setup(x => x.Elements).Returns(inventoryFiles);

            bool? result = null;
            _inventoryService.InventoryProcessData.AreBaseInventoriesComplete
                .Subscribe(value => result = value);

            // Act
            await _inventoryService.SetLocalInventory(inventoryFiles, LocalInventoryModes.Base);

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public async Task CheckInventoriesReady_WhenMissingInventoryForOtherDataNode_ShouldSetCompleteToFalse()
        {
            // Arrange
            var dataNodes = new List<DataNode>
            {
                new DataNode { Id = "node1", ClientInstanceId = "other_client_1", Code = "A" },
                new DataNode { Id = "node2", ClientInstanceId = "other_client_2", Code = "B" },
                new DataNode { Id = "node3", ClientInstanceId = "current_client_instance_id", Code = "C" }
            };

            var inventoryFiles = new List<InventoryFile>
            {
                CreateInventoryFile("other_client_1", "A", SharedFileTypes.BaseInventory),
                // Missing inventory for other_client_2 with code "B"
                CreateInventoryFile("current_client_instance_id", "C", SharedFileTypes.BaseInventory)
            };

            _dataNodeRepositoryMock.Setup(x => x.Elements).Returns(dataNodes);
            _inventoryFileRepositoryMock.Setup(x => x.Elements).Returns(inventoryFiles);

            bool? result = null;
            _inventoryService.InventoryProcessData.AreBaseInventoriesComplete
                .Subscribe(value => result = value);

            // Act
            await _inventoryService.SetLocalInventory(inventoryFiles, LocalInventoryModes.Base);

            // Assert
            result.Should().BeFalse();
        }

        [Test]
        public async Task CheckInventoriesReady_WhenCurrentMemberHasNoInventory_ShouldSetCompleteToFalse()
        {
            // Arrange
            var dataNodes = new List<DataNode>
            {
                new DataNode { Id = "node1", ClientInstanceId = "other_client_1", Code = "A" },
                new DataNode { Id = "node2", ClientInstanceId = "current_client_instance_id", Code = "B" }
            };

            var inventoryFiles = new List<InventoryFile>
            {
                CreateInventoryFile("other_client_1", "A", SharedFileTypes.BaseInventory)
                // Missing inventory for current member
            };

            _dataNodeRepositoryMock.Setup(x => x.Elements).Returns(dataNodes);
            _inventoryFileRepositoryMock.Setup(x => x.Elements).Returns(inventoryFiles);

            bool? result = null;
            _inventoryService.InventoryProcessData.AreBaseInventoriesComplete
                .Subscribe(value => result = value);

            // Act
            await _inventoryService.SetLocalInventory(inventoryFiles, LocalInventoryModes.Base);

            // Assert
            result.Should().BeFalse();
        }

        [Test]
        public async Task CheckInventoriesReady_WhenAllDataNodesHaveInventoriesForCurrentMember_ShouldSetCompleteToTrue()
        {
            // Arrange
            var dataNodes = new List<DataNode>
            {
                new DataNode { Id = "node1", ClientInstanceId = "current_client_instance_id", Code = "A" },
                new DataNode { Id = "node2", ClientInstanceId = "current_client_instance_id", Code = "B" }
            };

            var inventoryFiles = new List<InventoryFile>
            {
                CreateInventoryFile("current_client_instance_id", "A", SharedFileTypes.BaseInventory),
                CreateInventoryFile("current_client_instance_id", "B", SharedFileTypes.BaseInventory)
            };

            _dataNodeRepositoryMock.Setup(x => x.Elements).Returns(dataNodes);
            _inventoryFileRepositoryMock.Setup(x => x.Elements).Returns(inventoryFiles);

            bool? result = null;
            _inventoryService.InventoryProcessData.AreBaseInventoriesComplete
                .Subscribe(value => result = value);

            // Act
            await _inventoryService.SetLocalInventory(inventoryFiles, LocalInventoryModes.Base);

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public async Task CheckInventoriesReady_WhenFullInventoryMode_ShouldCheckFullInventories()
        {
            // Arrange
            var dataNodes = new List<DataNode>
            {
                new DataNode { Id = "node1", ClientInstanceId = "other_client_1", Code = "A" },
                new DataNode { Id = "node2", ClientInstanceId = "current_client_instance_id", Code = "B" }
            };

            var inventoryFiles = new List<InventoryFile>
            {
                CreateInventoryFile("other_client_1", "A", SharedFileTypes.FullInventory),
                CreateInventoryFile("current_client_instance_id", "B", SharedFileTypes.FullInventory)
            };

            _dataNodeRepositoryMock.Setup(x => x.Elements).Returns(dataNodes);
            _inventoryFileRepositoryMock.Setup(x => x.Elements).Returns(inventoryFiles);

            bool? result = null;
            _inventoryService.InventoryProcessData.AreFullInventoriesComplete
                .Subscribe(value => result = value);

            // Act
            await _inventoryService.SetLocalInventory(inventoryFiles, LocalInventoryModes.Full);

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public async Task OnFileIsFullyDownloaded_WhenInventoryFile_ShouldCheckInventoriesReady()
        {
            // Arrange
            var dataNodes = new List<DataNode>
            {
                new DataNode { Id = "node1", ClientInstanceId = "other_client_1", Code = "A" },
                new DataNode { Id = "node2", ClientInstanceId = "current_client_instance_id", Code = "B" }
            };

            var inventoryFiles = new List<InventoryFile>
            {
                CreateInventoryFile("other_client_1", "A", SharedFileTypes.BaseInventory),
                CreateInventoryFile("current_client_instance_id", "B", SharedFileTypes.BaseInventory)
            };

            var localSharedFile = new LocalSharedFile(
                CreateSharedFileDefinition("other_client_1", "A", SharedFileTypes.BaseInventory),
                "test_path");

            _dataNodeRepositoryMock.Setup(x => x.Elements).Returns(dataNodes);
            _inventoryFileRepositoryMock.Setup(x => x.Elements).Returns(inventoryFiles);

            bool? result = null;
            _inventoryService.InventoryProcessData.AreBaseInventoriesComplete
                .Subscribe(value => result = value);

            // Act
            await _inventoryService.OnFileIsFullyDownloaded(localSharedFile);

            // Assert
            result.Should().BeTrue();
        }
        
        [Test]
        public async Task CheckInventoriesReady_ShouldNotDependOnCodeProperty()
        {
            // Arrange
            var dataNodes = new List<DataNode>
            {
                new DataNode { Id = "node1", ClientInstanceId = "other_client_1", Code = "X" },
                new DataNode { Id = "node2", ClientInstanceId = "current_client_instance_id", Code = "Y" }
            };

            var inventoryFiles = new List<InventoryFile>
            {
                // Les codes ne correspondent pas, mais les ClientInstanceId oui
                CreateInventoryFile("other_client_1", "DIFFERENT_CODE", SharedFileTypes.BaseInventory),
                CreateInventoryFile("current_client_instance_id", "ANOTHER_CODE", SharedFileTypes.BaseInventory)
            };

            _dataNodeRepositoryMock.Setup(x => x.Elements).Returns(dataNodes);
            _inventoryFileRepositoryMock.Setup(x => x.Elements).Returns(inventoryFiles);

            bool? result = null;
            _inventoryService.InventoryProcessData.AreBaseInventoriesComplete
                .Subscribe(value => result = value);

            // Act
            await _inventoryService.SetLocalInventory(inventoryFiles, LocalInventoryModes.Base);

            // Assert
            result.Should().BeTrue();
        }

        private static InventoryFile CreateInventoryFile(string clientInstanceId, string code, SharedFileTypes sharedFileType)
        {
            var sharedFileDefinition = CreateSharedFileDefinition(clientInstanceId, code, sharedFileType);
            return new InventoryFile(sharedFileDefinition, $"test_path_{clientInstanceId}_{code}");
        }

        private static SharedFileDefinition CreateSharedFileDefinition(string clientInstanceId, string code, SharedFileTypes sharedFileType)
        {
            return new SharedFileDefinition
            {
                ClientInstanceId = clientInstanceId,
                AdditionalName = $"{clientInstanceId}_{code}",
                SharedFileType = sharedFileType
            };
        }
    }
}