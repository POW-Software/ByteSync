using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Services;

public class CosmosDbService : ICosmosDbService
{
    public CosmosClient Client { get; }
    
    public Container ActionsGroupDefinitionsContainer { get; }

    public CosmosDbService(IOptions<CosmosDbSettings> cosmosDbSettings)
    {
        var settings = cosmosDbSettings.Value;

        Client = new CosmosClient(settings.ConnectionString, new CosmosClientOptions
        {
            AllowBulkExecution = true
        });

        var database = Client.GetDatabase(settings.DatabaseName);
        ActionsGroupDefinitionsContainer = database.GetContainer("ActionsGroupDefinitions");
    }

    public async Task InitializeAsync()
    {
        var database = await Client.CreateDatabaseIfNotExistsAsync("YourDatabase");
        await database.Database.CreateContainerIfNotExistsAsync(new ContainerProperties
        {
            Id = "ActionsGroupDefinitions",
            PartitionKeyPath = "/SessionId"
        });
    }
}