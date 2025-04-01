using ByteSync.Business.Actions.Shared;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Communications;
using ByteSync.Services.Actions;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Actions;

[TestFixture]
public class SharedActionsGroupOrganizer_UnitTests
{
    private SharedActionsGroupOrganizer _sharedActionsGroupOrganizer;
    private Mock<IConnectionService> _connectionService;
    private Mock<ISharedActionsGroupRepository> _sharedActionsGroupRepository;
    
    [SetUp]
    public void SetUp()
    {
        _connectionService = new Mock<IConnectionService>();
        _sharedActionsGroupRepository = new Mock<ISharedActionsGroupRepository>();
        
        _sharedActionsGroupOrganizer = new SharedActionsGroupOrganizer(_connectionService.Object, _sharedActionsGroupRepository.Object);
    }
    
    [Test]
    public async Task OrganizeSharedActionsGroups_ShouldHandleEmptyList()
    {
        // Arrange
        _sharedActionsGroupRepository.Setup(x => x.Elements)
            .Returns(new List<SharedActionsGroup>());
        
        // Act
        await _sharedActionsGroupOrganizer.OrganizeSharedActionGroups();
        
        // Assert
        _sharedActionsGroupRepository.Verify(x => x.SetOrganizedSharedActionsGroups(It.Is<List<SharedActionsGroup>>(s => s.Count == 0)), Times.Once);
    }
    
    [Test]
    [TestCase("CID1", 1)]
    [TestCase("CID2", 0)]
    public async Task OrganizeSharedActionsGroups_WhenSynchronizeContentAndDate_ShouldSortCorrectly(string clientInstanceId, int expectedCount)
    {
        // Arrange
        var sharedActionsGroups = new List<SharedActionsGroup>
        {
            new()
            { 
                Operator = ActionOperatorTypes.SynchronizeContentAndDate,
                ActionsGroupId = "AGId1",
                PathIdentity = new PathIdentity(FileSystemTypes.File, "file1", "file1", "file1"), 
                Source = new SharedDataPart("A1", FileSystemTypes.Directory, "CID1", "A", "root", "relative", null, null, false),
                Targets = [
                    new("B1", FileSystemTypes.Directory, "CID2", "B", "root", "relative", null, null, false)
                ],
                SynchronizationType = SynchronizationTypes.Full,
                SynchronizationStatus = null, 
                IsFromSynchronizationRule = false,
                Size = null,
                CreationTimeUtc = null,
                LastWriteTimeUtc = null,
                AppliesOnlySynchronizeDate = false,
            }
        };
        
        _connectionService.Setup(x => x.ClientInstanceId)
            .Returns(clientInstanceId);

        _sharedActionsGroupRepository.Setup(x => x.Elements)
            .Returns(sharedActionsGroups);
        
        List<SharedActionsGroup> capturedGroups = null!;
        _sharedActionsGroupRepository.Setup(x => x.SetOrganizedSharedActionsGroups(It.IsAny<List<SharedActionsGroup>>()))
            .Callback<List<SharedActionsGroup>>(arg => capturedGroups = arg);
        
        // Act
        await _sharedActionsGroupOrganizer.OrganizeSharedActionGroups();
        
        capturedGroups.Should().NotBeNull();
        capturedGroups.Should().HaveCount(expectedCount);
        if (expectedCount == 1)
        {
            capturedGroups[0].Should().BeSameAs(sharedActionsGroups[0]);
        }
        _sharedActionsGroupRepository.Verify(x => x.SetOrganizedSharedActionsGroups(It.IsAny<List<SharedActionsGroup>>()), Times.Once);
    }
    
    [Test]
    [TestCase("CID1", 0)]
    [TestCase("CID2", 1)]
    public async Task OrganizeSharedActionsGroups_When_SynchronizeContentAndDate_And_AppliesOnlySynchronizeDate_ShouldSortCorrectly(
        string clientInstanceId, int expectedCount)
    {
        // Arrange
        var sharedActionsGroups = new List<SharedActionsGroup>
        {
            new()
            { 
                Operator = ActionOperatorTypes.SynchronizeContentAndDate,
                ActionsGroupId = "AGId1",
                PathIdentity = new PathIdentity(FileSystemTypes.File, "file1", "file1", "file1"), 
                Source = new SharedDataPart("A1", FileSystemTypes.Directory, "CID1", "A", "root", "relative", null, null, false),
                Targets = [
                    new("B1", FileSystemTypes.Directory, "CID2", "B", "root", "relative", null, null, false)
                ],
                SynchronizationType = null,
                SynchronizationStatus = null, 
                IsFromSynchronizationRule = false,
                Size = null,
                CreationTimeUtc = null,
                LastWriteTimeUtc = null,
                AppliesOnlySynchronizeDate = true,
            }
        };
        
        _connectionService.Setup(x => x.ClientInstanceId)
            .Returns(clientInstanceId);

        _sharedActionsGroupRepository.Setup(x => x.Elements)
            .Returns(sharedActionsGroups);
        
        List<SharedActionsGroup> capturedGroups = null!;
        _sharedActionsGroupRepository.Setup(x => x.SetOrganizedSharedActionsGroups(It.IsAny<List<SharedActionsGroup>>()))
            .Callback<List<SharedActionsGroup>>(arg => capturedGroups = arg);
        
        // Act
        await _sharedActionsGroupOrganizer.OrganizeSharedActionGroups();

        capturedGroups.Should().NotBeNull();
        capturedGroups.Should().HaveCount(expectedCount);
        if (expectedCount == 1)
        {
            capturedGroups[0].Should().BeSameAs(sharedActionsGroups[0]);
        }
        _sharedActionsGroupRepository.Verify(x => x.SetOrganizedSharedActionsGroups(It.IsAny<List<SharedActionsGroup>>()), Times.Once);
    }
}