using ByteSync.Common.Business.Actions;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Azure.Cosmos;

namespace ByteSync.ServerCommon.Repositories;

public class ActionsGroupDefinitionsRepository : IActionsGroupDefinitionsRepository
{
    private readonly ICosmosDbService _cosmosDbService;

    public ActionsGroupDefinitionsRepository(ICosmosDbService cosmosDbService)
    {
        _cosmosDbService = cosmosDbService;
    }
    
    public async Task AddOrUpdateActionsGroupDefinitions(
        string sessionId,
        List<ActionsGroupDefinition> synchronizationActionsDefinitions)
    {
        const int maxConcurrentOperations = 100;

        var container = _cosmosDbService.ActionsGroupDefinitionsContainer;
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
    
    public async Task<ActionsGroupDefinitionEntity> GetActionGroupDefinition(string actionsGroupId, string sessionId)
    {
        var container = _cosmosDbService.ActionsGroupDefinitionsContainer;

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
        var container = _cosmosDbService.ActionsGroupDefinitionsContainer;

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
}