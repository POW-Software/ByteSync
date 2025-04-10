using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Factories;
using ByteSync.ServerCommon.Repositories;
using ByteSync.ServerCommon.Services;
using ByteSync.ServerCommon.Tests.Helpers;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Tests.Repositories;

public class InventoryRepositoryTests
{
    [Test]
    public async Task Get_IntegrationTest()
    {
        // Arrange
        var (repository, cacheRepository, redisInfrastructureService) = SetupRepositoryAndDependencies();
        
        string sessionId = "testSession_" + DateTime.Now.Ticks;
        var inventoryData = new InventoryData
        {
            SessionId = sessionId,
            InventoryMembers = [new() { ClientInstanceId = "client1" }]
        };
        
        var cacheKey = redisInfrastructureService.ComputeCacheKey(EntityType.Inventory, sessionId);
        await cacheRepository.Save(cacheKey, inventoryData);

        // Act
        var result = await repository.Get(sessionId);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be(sessionId);
        result.InventoryMembers.Should().HaveCount(1);
        result.InventoryMembers[0].ClientInstanceId.Should().Be("client1");
    }

    [Test]
    public async Task GetInventoryMember_IntegrationTest()
    {
        // Arrange
        var (repository, cacheRepository, redisInfrastructureService) = SetupRepositoryAndDependencies();
        
        string sessionId = "testSession_" + DateTime.Now.Ticks;
        string clientId1 = "client1";
        string clientId2 = "client2";
        
        var inventoryData = new InventoryData
        {
            SessionId = sessionId,
            InventoryMembers =
            [
                new() { ClientInstanceId = clientId1 },
                new() { ClientInstanceId = clientId2 }
            ]
        };
        
        var cacheKey = redisInfrastructureService.ComputeCacheKey(EntityType.Inventory, sessionId);
        await cacheRepository.Save(cacheKey, inventoryData);

        // Act
        var result = await repository.GetInventoryMember(sessionId, clientId2);

        // Assert
        result.Should().NotBeNull();
        result.ClientInstanceId.Should().Be(clientId2);
    }
    
    [Test]
    public async Task GetInventoryMember_WhenMemberNotFound_ReturnsNull()
    {
        // Arrange
        var (repository, cacheRepository, redisInfrastructureService) = SetupRepositoryAndDependencies();
        
        string sessionId = "testSession_" + DateTime.Now.Ticks;
        var inventoryData = new InventoryData
        {
            SessionId = sessionId,
            InventoryMembers = [new() { ClientInstanceId = "client1" }]
        };
        
        var cacheKey = redisInfrastructureService.ComputeCacheKey(EntityType.Inventory, sessionId);
        await cacheRepository.Save(cacheKey, inventoryData);

        // Act
        var result = await repository.GetInventoryMember(sessionId, "nonExistentClient");

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetInventoryMember_WhenInventoryNotFound_ReturnsNull()
    {
        // Arrange
        var (repository, _, _) = SetupRepositoryAndDependencies();
        
        string nonExistentSessionId = "nonExistentSession_" + DateTime.Now.Ticks;

        // Act
        var result = await repository.GetInventoryMember(nonExistentSessionId, "anyClient");

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task Save_IntegrationTest()
    {
        // Arrange
        var (repository, _, redisInfrastructureService) = SetupRepositoryAndDependencies();
        
        string sessionId = "testSession_" + DateTime.Now.Ticks;
        var inventoryData = new InventoryData
        {
            SessionId = sessionId,
            InventoryMembers = [new() { ClientInstanceId = "client1" }]
        };

        var cacheKey = redisInfrastructureService.ComputeCacheKey(EntityType.Inventory, sessionId);

        // Act
        await repository.Save(cacheKey, inventoryData);
        var result = await repository.Get(sessionId);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be(sessionId);
        result.InventoryMembers.Should().HaveCount(1);
        result.InventoryMembers[0].ClientInstanceId.Should().Be("client1");
    }

    private (InventoryRepository, CacheRepository<InventoryData>, RedisInfrastructureService) SetupRepositoryAndDependencies()
    {
        var redisSettings = TestSettingsInitializer.GetRedisSettings();
        var cacheKeyFactory = new CacheKeyFactory(Options.Create(redisSettings));
        var loggerFactoryMock = A.Fake<ILoggerFactory>();
        
        var redisInfrastructureService = new RedisInfrastructureService(
            Options.Create(redisSettings), 
            cacheKeyFactory, 
            loggerFactoryMock);
            
        var cacheRepository = new CacheRepository<InventoryData>(redisInfrastructureService);
        var repository = new InventoryRepository(redisInfrastructureService, cacheRepository);
        
        return (repository, cacheRepository, redisInfrastructureService);
    }
}
