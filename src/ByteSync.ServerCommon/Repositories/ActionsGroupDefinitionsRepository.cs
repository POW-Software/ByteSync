using ByteSync.Common.Business.Actions;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Misc;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace ByteSync.ServerCommon.Repositories;

public class ActionsGroupDefinitionsRepository : IActionsGroupDefinitionsRepository
{
    private readonly CosmosClient _cosmosClient;

    private readonly CosmosDbSettings _cosmosDbSettings;
    // private readonly ByteSyncDbContext _dbContext;

    public ActionsGroupDefinitionsRepository(ByteSyncDbContext dbContext, IOptions<CosmosDbSettings> cosmosDbSettings)
    {
        // _dbContext = dbContext;
        
        _cosmosDbSettings = cosmosDbSettings.Value;

        var clientOptions = new CosmosClientOptions
        {
            AllowBulkExecution = true
        };
        _cosmosClient = new CosmosClient(_cosmosDbSettings.ConnectionString, clientOptions);
    }
    
    private Container GetContainer()
    {
        return _cosmosClient.GetContainer(_cosmosDbSettings.DatabaseName, "ActionsGroupDefinitions");
    }
    
    public async Task AddOrUpdateActionsGroupDefinitions(
        string sessionId,
        List<ActionsGroupDefinition> synchronizationActionsDefinitions)
    {
        const int maxConcurrentOperations = 100;

        var container = GetContainer();
        var semaphore = new SemaphoreSlim(maxConcurrentOperations);
        var tasks = new List<Task>();

        foreach (var definition in synchronizationActionsDefinitions)
        {
            await semaphore.WaitAsync();

            var entity = new ActionsGroupDefinitionEntity
            {
                ActionsGroupDefinitionEntityId = definition.ActionsGroupId,
                Operator = definition.Operator,
                Size = definition.Size,
                CreationTimeUtc = definition.CreationTimeUtc,
                AppliesOnlySynchronizeDate = definition.AppliesOnlySynchronizeDate,
                LastWriteTimeUtc = definition.LastWriteTimeUtc,
                SessionId = sessionId,
                Source = definition.Source,
                Targets = definition.Targets,
                FileSystemType = definition.FileSystemType,
            };

            var task = container.UpsertItemAsync(entity, new PartitionKey(sessionId))
                .ContinueWith(t =>
                {
                    semaphore.Release();
                    if (t.IsFaulted)
                    {
                        // Log or handle the error if needed
                        Console.Error.WriteLine($"Failed to upsert item: {entity.Id} - {t.Exception}");
                    }
                });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }

    // public async Task AddOrUpdateActionsGroupDefinitions(string sessionId, List<ActionsGroupDefinition> synchronizationActionsDefinitions)
    // {
    //     var container = _cosmosClient.GetContainer("YourDatabase", "YourContainer");
    //
    //     var tasks = new List<Task>();
    //     foreach (var def in synchronizationActionsDefinitions)
    //     {
    //         var entity = new ActionsGroupDefinitionEntity
    //         {
    //             ActionsGroupDefinitionEntityId = def.ActionsGroupId,
    //             Operator = def.Operator,
    //             Size = def.Size,
    //             CreationTimeUtc = def.CreationTimeUtc,
    //             AppliesOnlySynchronizeDate = def.AppliesOnlySynchronizeDate,
    //             LastWriteTimeUtc = def.LastWriteTimeUtc,
    //             SessionId = sessionId,
    //             Source = def.Source,
    //             Targets = def.Targets,
    //             FileSystemType = def.FileSystemType,
    //             id = def.ActionsGroupId // obligatoire pour Cosmos
    //         };
    //
    //         tasks.Add(container.UpsertItemAsync(entity, new PartitionKey(sessionId)));
    //
    //         // Optionnel : limitation à un nombre max de tâches en parallèle
    //         if (tasks.Count >= 500)
    //         {
    //             await Task.WhenAll(tasks);
    //             tasks.Clear();
    //         }
    //     }
    //
    //     if (tasks.Count > 0)
    //         await Task.WhenAll(tasks);
    //     
    //     
    //     
    //     const int batchSize = 100;
    //
    //     for (int i = 0; i < synchronizationActionsDefinitions.Count; i += batchSize)
    //     {
    //         var batch = synchronizationActionsDefinitions
    //             .Skip(i)
    //             .Take(batchSize)
    //             .Select(definition => new ActionsGroupDefinitionEntity
    //             {
    //                 ActionsGroupDefinitionEntityId = definition.ActionsGroupId,
    //                 Operator = definition.Operator,
    //                 Size = definition.Size,
    //                 CreationTimeUtc = definition.CreationTimeUtc,
    //                 AppliesOnlySynchronizeDate = definition.AppliesOnlySynchronizeDate,
    //                 LastWriteTimeUtc = definition.LastWriteTimeUtc,
    //                 SessionId = sessionId,
    //                 Source = definition.Source,
    //                 Targets = definition.Targets,
    //                 FileSystemType = definition.FileSystemType,
    //             })
    //             .ToList();
    //         
    //         await _dbContext.ActionsGroupDefinitions.AddRangeAsync(batch);
    //         await _dbContext.SaveChangesAsync();
    //     }
    // }

    // public async Task<ActionsGroupDefinitionEntity> GetActionGroupDefinition(string actionsGroupId, string sessionId)
    // {
    //     return await _dbContext.ActionsGroupDefinitions
    //         .FirstAsync(e => e.ActionsGroupDefinitionEntityId == actionsGroupId && 
    //                          e.SessionId == sessionId);
    // }
    //
    public async Task<ActionsGroupDefinitionEntity> GetActionGroupDefinition(string actionsGroupId, string sessionId)
    {
        var container = GetContainer();

        try
        {
            var response = await container.ReadItemAsync<ActionsGroupDefinitionEntity>(
                id: actionsGroupId,
                partitionKey: new PartitionKey(sessionId));

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null; // ou throw une exception métier, selon ton besoin
        }
    }
    
    public async Task DeleteActionsGroupDefinitions(string sessionId)
    {
        var container = GetContainer();

        var query = new QueryDefinition("SELECT c.id FROM c WHERE c.SessionId = @sessionId")
            .WithParameter("@sessionId", sessionId);

        var iterator = container.GetItemQueryIterator<IdOnlyResult>(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(sessionId)
            });

        var deleteTasks = new List<Task>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            foreach (var item in response)
            {
                deleteTasks.Add(container.DeleteItemAsync<ActionsGroupDefinitionEntity>(
                    id: item.Id,
                    partitionKey: new PartitionKey(sessionId)));
            }
        }

        await Task.WhenAll(deleteTasks);
    }

    private class IdOnlyResult
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }
    
    // public async Task DeleteActionsGroupDefinitions(string sessionId)
    // {
    //     var entitiesToDelete = await _dbContext.ActionsGroupDefinitions
    //         .Where(e => e.SessionId == sessionId)
    //         .ToListAsync();
    //     
    //     _dbContext.ActionsGroupDefinitions.RemoveRange(entitiesToDelete);
    //     
    //     await _dbContext.SaveChangesAsync();
    // }
}