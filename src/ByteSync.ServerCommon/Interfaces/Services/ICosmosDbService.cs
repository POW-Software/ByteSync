using Microsoft.Azure.Cosmos;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface ICosmosDbService
{
    CosmosClient Client { get; }
    Container ActionsGroupDefinitionsContainer { get; }
}