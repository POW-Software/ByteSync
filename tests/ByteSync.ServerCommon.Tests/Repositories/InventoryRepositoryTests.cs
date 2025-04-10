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

[TestFixture]
public class InventoryRepositoryTests
{
    private InventoryRepository _repository;
    private CacheRepository<InventoryData> _cacheRepository;
    private RedisInfrastructureService _redisInfrastructureService;
    
    [SetUp]
    public void SetUp()
    {
        var redisSettings = TestSettingsInitializer.GetRedisSettings();
        var cacheKeyFactory = new CacheKeyFactory(Options.Create(redisSettings));
        var loggerFactoryMock = A.Fake<ILoggerFactory>();
        
        _redisInfrastructureService = new RedisInfrastructureService(
            Options.Create(redisSettings), 
            cacheKeyFactory, 
            loggerFactoryMock);
            
        _cacheRepository = new CacheRepository<InventoryData>(_redisInfrastructureService);
        
        _repository = new InventoryRepository(_redisInfrastructureService, _cacheRepository);
    }

    [Test]
    public async Task Get_IntegrationTest()
    {
        // Arrange
        string sessionId = "testSession_" + DateTime.Now.Ticks;
        var inventoryData = new InventoryData
        {
            SessionId = sessionId,
            InventoryMembers = [new() { ClientInstanceId = "client1" }]
        };
        
        var cacheKey = _redisInfrastructureService.ComputeCacheKey(EntityType.Inventory, sessionId);
        await _cacheRepository.Save(cacheKey, inventoryData);

        // Act
        var result = await _repository.Get(sessionId);

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
        
        var cacheKey = _redisInfrastructureService.ComputeCacheKey(EntityType.Inventory, sessionId);
        await _cacheRepository.Save(cacheKey, inventoryData);

        // Act
        var result = await _repository.GetInventoryMember(sessionId, clientId2);

        // Assert
        result.Should().NotBeNull();
        result.ClientInstanceId.Should().Be(clientId2);
    }
    
    [Test]
    public async Task GetInventoryMember_WhenMemberNotFound_ReturnsNull()
    {
        // Arrange
        string sessionId = "testSession_" + DateTime.Now.Ticks;
        var inventoryData = new InventoryData
        {
            SessionId = sessionId,
            InventoryMembers = [new() { ClientInstanceId = "client1" }]
        };
        
        var cacheKey = _redisInfrastructureService.ComputeCacheKey(EntityType.Inventory, sessionId);
        await _cacheRepository.Save(cacheKey, inventoryData);

        // Act
        var result = await _repository.GetInventoryMember(sessionId, "nonExistentClient");

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetInventoryMember_WhenInventoryNotFound_ReturnsNull()
    {
        // Arrange
        string nonExistentSessionId = "nonExistentSession_" + DateTime.Now.Ticks;

        // Act
        var result = await _repository.GetInventoryMember(nonExistentSessionId, "anyClient");

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task Save_IntegrationTest()
    {
        // Arrange
        string sessionId = "testSession_" + DateTime.Now.Ticks;
        var inventoryData = new InventoryData
        {
            SessionId = sessionId,
            InventoryMembers = [new() { ClientInstanceId = "client1" }]
        };

        var cacheKey = _redisInfrastructureService.ComputeCacheKey(EntityType.Inventory, sessionId);

        // Act
        await _repository.Save(cacheKey, inventoryData);
        var result = await _repository.Get(sessionId);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be(sessionId);
        result.InventoryMembers.Should().HaveCount(1);
        result.InventoryMembers[0].ClientInstanceId.Should().Be("client1");
    }
}
