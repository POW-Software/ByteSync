using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Services;

// public class CosmosDbService : ICosmosDbService
// {
//     private readonly CosmosDbSettings _settings;
//     
//     public CosmosClient Client { get; }
//     
//     public Container ActionsGroupDefinitionsContainer { get; }
//
//     public CosmosDbService(IOptions<CosmosDbSettings> cosmosDbSettings)
//     {
//         _settings = cosmosDbSettings.Value;
//
//         Client = new CosmosClient(_settings.ConnectionString, new CosmosClientOptions
//         {
//             AllowBulkExecution = true
//         });
//
//         var database = Client.GetDatabase(_settings.DatabaseName);
//         ActionsGroupDefinitionsContainer = database.GetContainer("ActionsGroupDefinitions");
//     }
//
//     public async Task InitializeAsync()
//     {
//         var database = await Client.CreateDatabaseIfNotExistsAsync(_settings.DatabaseName);
//         await database.Database.CreateContainerIfNotExistsAsync(new ContainerProperties
//         {
//             Id = "ActionsGroupDefinitions",
//             PartitionKeyPath = "/SessionId"
//         });
//     }
// }