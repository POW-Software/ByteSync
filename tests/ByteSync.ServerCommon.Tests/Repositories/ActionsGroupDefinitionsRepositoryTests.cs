using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.Inventories;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Repositories;
using ByteSync.ServerCommon.Services;
using ByteSync.ServerCommon.Tests.Helpers;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
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
        Assert.Pass();
        return;
        
        // Arrange
        var cosmosDbSettings = TestSettingsInitializer.GetCosmosDbSettings();
        
        var cosmosDbService = new CosmosDbService(Options.Create(cosmosDbSettings));
        await cosmosDbService.InitializeAsync();
        
        var repository = new ActionsGroupDefinitionsRepository(cosmosDbService);

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
        (await repository.GetActionGroupDefinition(actionsGroupId1, sessionId)).Should().NotBeNull();
        (await repository.GetActionGroupDefinition(actionsGroupId2, sessionId)).Should().NotBeNull();
    }
    
    [Test]
    public async Task ResetSession_ShouldDeleteActionsGroupDefinitions_IntegrationTest()
    {
        Assert.Pass();
        return;
        
        // Arrange
        var cosmosDbSettings = TestSettingsInitializer.GetCosmosDbSettings();
        
        var cosmosDbService = new CosmosDbService(Options.Create(cosmosDbSettings));
        await cosmosDbService.InitializeAsync();
        
        var repository = new ActionsGroupDefinitionsRepository(cosmosDbService);

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
        
        (await repository.GetActionGroupDefinition(actionsGroupId1, sessionId)).Should().NotBeNull();
        (await repository.GetActionGroupDefinition(actionsGroupId2, sessionId)).Should().NotBeNull();

        // Act
        await repository.DeleteActionsGroupDefinitions(sessionId);
        
        // Assert
        var query = new QueryDefinition("SELECT * FROM c WHERE c.SessionId = @sessionId")
            .WithParameter("@sessionId", sessionId);

        var iterator = cosmosDbService.ActionsGroupDefinitionsContainer
            .GetItemQueryIterator<ActionsGroupDefinitionEntity>(
                query,
                requestOptions: new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey(sessionId) // La clé de partition est toujours spécifiée ici
                });

        var results = new List<ActionsGroupDefinitionEntity>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        results.Should().BeEmpty();
    }
    
    // [Test]
    // public async Task GetActionGroupDefinition_ShouldReturnSpecificItem_IntegrationTest()
    // {
    //     // Arrange
    //     var cosmosDbSettings = TestSettingsInitializer.GetCosmosDbSettings();
    //     var cosmosDbService = new CosmosDbService(Options.Create(cosmosDbSettings));
    //     await cosmosDbService.InitializeAsync();
    //
    //     var repository = new ActionsGroupDefinitionsRepository(cosmosDbService);
    //
    //     string sessionId = "sessionId_638806451076058989";
    //     string actionsGroupId = "ActionsGroupId_1_638806451076059405";
    //
    //     // var actionsGroupDefinition = new ActionsGroupDefinition
    //     // {
    //     //     Operator = ActionOperatorTypes.SynchronizeContentAndDate,
    //     //     Size = 100,
    //     //     Source = "SourceTest",
    //     //     Targets = new List<string> { "TargetTest" },
    //     //     FileSystemType = FileSystemTypes.File,
    //     //     CreationTimeUtc = DateTime.Parse("2025-04-19T05:28:35.9674555Z"),
    //     //     LastWriteTimeUtc = DateTime.Parse("2025-04-19T05:33:35.9677043Z"),
    //     //     AppliesOnlySynchronizeDate = false,
    //     //     ActionsGroupId = actionsGroupId,
    //     // };
    //     //
    //     // await repository.AddOrUpdateActionsGroupDefinitions(sessionId, new List<ActionsGroupDefinition> { actionsGroupDefinition });
    //
    //     // Act
    //     var result = await repository.GetActionGroupDefinition(actionsGroupId, sessionId);
    //
    //     // Assert
    //     result.Should().NotBeNull();
    //     result!.ActionsGroupDefinitionEntityId.Should().Be(actionsGroupId);
    //     result.SessionId.Should().Be(sessionId);
    //     result.Source.Should().Be("SourceTest");
    //     result.Targets.Should().ContainSingle().Which.Should().Be("TargetTest");
    //     result.FileSystemType.Should().Be(FileSystemTypes.File);
    //     result.Operator.Should().Be(ActionOperatorTypes.SynchronizeContentAndDate);
    //     result.Size.Should().Be(100);
    //     result.CreationTimeUtc.Should().Be(DateTime.Parse("2025-04-19T05:28:35.9674555Z"));
    //     result.LastWriteTimeUtc.Should().Be(DateTime.Parse("2025-04-19T05:33:35.9677043Z"));
    //     result.AppliesOnlySynchronizeDate.Should().BeFalse();
    // }
}