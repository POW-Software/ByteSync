using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.Inventories;
using ByteSync.ServerCommon.Misc;
using ByteSync.ServerCommon.Repositories;
using ByteSync.ServerCommon.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Tests.Repositories;

public class ActionsGroupDefinitionsRepositoryTests
{
    public ActionsGroupDefinitionsRepositoryTests()
    {

    }

    [Test]
    public async Task AddOrUpdateActionsGroupDefinitions_ShouldSaveActionsGroupDefinitions_IntegrationTest()
    {
        // Arrange
        var cosmosDbSettings = TestSettingsInitializer.GetCosmosDbSettings();
        
        ByteSyncDbContext byteSyncDbContext = new ByteSyncDbContext(Options.Create(cosmosDbSettings));
        await byteSyncDbContext.InitializeCosmosDb();
        
        var repository = new ActionsGroupDefinitionsRepository(byteSyncDbContext, Options.Create(cosmosDbSettings));

        string sessionId = "sessionId_" + DateTime.Now.Ticks;
        var actionsGroupId1 = "ActionsGroupId_1_" + DateTime.Now.Ticks;
        var actionsGroupId2 = "ActionsGroupId_2_" + DateTime.Now.Ticks;
        var actionsGroupDefinitions = new List<ActionsGroupDefinition>
        {
            new ()
            {
                Operator = ActionOperatorTypes.SynchronizeContentAndDate,
                Size = 100,
                Source = "SourceTest",
                Targets = ["TargetTest"],
                FileSystemType = FileSystemTypes.File,
                CreationTimeUtc = DateTime.UtcNow.AddMinutes(-10),
                LastWriteTimeUtc = DateTime.UtcNow.AddMinutes(-5),
                AppliesOnlySynchronizeDate = false,
                ActionsGroupId = actionsGroupId1,
            },
            new ()
            {
                Operator = ActionOperatorTypes.SynchronizeContentAndDate,
                Size = 200,
                Source = "SourceTest",
                Targets = ["TargetTest"],
                FileSystemType = FileSystemTypes.File,
                CreationTimeUtc = DateTime.UtcNow.AddMinutes(-20),
                LastWriteTimeUtc = DateTime.UtcNow.AddMinutes(-10),
                AppliesOnlySynchronizeDate = false,
                ActionsGroupId = actionsGroupId2,
            },
        };
        
        // Act
        await repository.AddOrUpdateActionsGroupDefinitions(sessionId, actionsGroupDefinitions);
        
        // Assert
        var countBefore = byteSyncDbContext.ActionsGroupDefinitions
            .Count(e => e.SessionId == sessionId);
        countBefore.Should().Be(2);
    }
    
    [Test]
    public async Task ResetSession_ShouldDeleteActionsGroupDefinitions_IntegrationTest()
    {
        // Arrange
        var cosmosDbSettings = TestSettingsInitializer.GetCosmosDbSettings();
        
        ByteSyncDbContext byteSyncDbContext = new ByteSyncDbContext(Options.Create(cosmosDbSettings));
        await byteSyncDbContext.InitializeCosmosDb();
        
        var repository = new ActionsGroupDefinitionsRepository(byteSyncDbContext, Options.Create(cosmosDbSettings));

        string sessionId = "sessionId_" + DateTime.Now.Ticks;
        var actionsGroupId1 = "ActionsGroupId_1_" + DateTime.Now.Ticks;
        var actionsGroupId2 = "ActionsGroupId_2_" + DateTime.Now.Ticks;
        var actionsGroupDefinitions = new List<ActionsGroupDefinition>
        {
            new ()
            {
                Operator = ActionOperatorTypes.SynchronizeContentAndDate,
                Size = 100,
                Source = "SourceTest",
                Targets = ["TargetTest"],
                FileSystemType = FileSystemTypes.File,
                CreationTimeUtc = DateTime.UtcNow.AddMinutes(-10),
                LastWriteTimeUtc = DateTime.UtcNow.AddMinutes(-5),
                AppliesOnlySynchronizeDate = false,
                ActionsGroupId = actionsGroupId1,
            },
            new ()
            {
                Operator = ActionOperatorTypes.SynchronizeContentAndDate,
                Size = 200,
                Source = "SourceTest",
                Targets = ["TargetTest"],
                FileSystemType = FileSystemTypes.File,
                CreationTimeUtc = DateTime.UtcNow.AddMinutes(-20),
                LastWriteTimeUtc = DateTime.UtcNow.AddMinutes(-10),
                AppliesOnlySynchronizeDate = false,
                ActionsGroupId = actionsGroupId2,
            },
        };
        
        await repository.AddOrUpdateActionsGroupDefinitions(sessionId, actionsGroupDefinitions);
        
        var countBefore = byteSyncDbContext.ActionsGroupDefinitions
            .Count(e => e.SessionId == sessionId);
        countBefore.Should().Be(2);

        // Act
        await repository.DeleteActionsGroupDefinitions(sessionId);
        
        // Assert
        var countAfter = byteSyncDbContext.ActionsGroupDefinitions
            .Count(e => e.SessionId == sessionId);
        
        countAfter.Should().Be(0);
    }
}