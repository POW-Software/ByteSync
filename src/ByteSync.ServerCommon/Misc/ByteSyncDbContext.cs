using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Entities;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Misc;

public class ByteSyncDbContext : DbContext
{
    private readonly CosmosDbSettings _cosmosDbSettings;
    
    public ByteSyncDbContext(IOptions<CosmosDbSettings> cosmosDbSettings)
    {
        _cosmosDbSettings = cosmosDbSettings.Value;
    }
    
    public CosmosDbSettings CosmosDbSettings => _cosmosDbSettings;
    
    public DbSet<ActionsGroupDefinitionEntity> ActionsGroupDefinitions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseCosmos(_cosmosDbSettings.ConnectionString, _cosmosDbSettings.DatabaseName);
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActionsGroupDefinitionEntity>()
            .ToContainer("ActionsGroupDefinitions")
            .HasPartitionKey(e => e.SessionId)
            .HasNoDiscriminator();
    }
    
    public async Task InitializeCosmosDb()
    {
        var client = new CosmosClient(_cosmosDbSettings.ConnectionString);
            
        var database = await client.CreateDatabaseIfNotExistsAsync(_cosmosDbSettings.DatabaseName);
        await database.Database.CreateContainerIfNotExistsAsync(
            new ContainerProperties
            {
                Id = "ActionsGroupDefinitions",
                PartitionKeyPath = "/SessionId"
            });
    }
}