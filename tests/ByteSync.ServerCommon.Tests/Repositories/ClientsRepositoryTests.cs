using ByteSync.Common.Business.EndPoints;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Factories;
using ByteSync.ServerCommon.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Repositories;
using ByteSync.ServerCommon.Services;
using ByteSync.ServerCommon.Tests.Helpers;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Tests.Repositories;

public class ClientsRepositoryTests
{
    private IRedisInfrastructureService _redisInfrastructureService;
    private CacheRepository<Client> _cacheRepository;
    private IClientsGroupIdFactory _clientsGroupIdFactory;
    private ClientsRepository _repository;

    [SetUp]
    public void Setup()
    {
        var redisSettings = TestSettingsInitializer.GetRedisSettings();
        var cacheKeyFactory = new CacheKeyFactory(Options.Create(redisSettings));
        var loggerFactoryMock = A.Fake<ILoggerFactory>();
        _redisInfrastructureService = new RedisInfrastructureService(Options.Create(redisSettings), cacheKeyFactory, loggerFactoryMock);
        _cacheRepository = new CacheRepository<Client>(_redisInfrastructureService);
        _clientsGroupIdFactory = A.Fake<IClientsGroupIdFactory>();
        _repository = new ClientsRepository(_redisInfrastructureService, _cacheRepository, _clientsGroupIdFactory);
    }

    [Test]
    public async Task Get_WithByteSyncEndpoint_ShouldReturnClient()
    {
        // Arrange
        var clientInstanceId = "testClient_" + DateTime.Now.Ticks;
        var byteSyncEndpoint = new ByteSyncEndpoint { ClientInstanceId = clientInstanceId };
        
        var client = new Client
        {
            ClientInstanceId = clientInstanceId
        };

        var cacheKey = _redisInfrastructureService.ComputeCacheKey(EntityType.Client, clientInstanceId);
        await _cacheRepository.Save(cacheKey, client);

        // Act
        var result = await _repository.Get(byteSyncEndpoint);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(client);
    }

    [Test]
    public async Task Get_WithNonExistingClientId_ShouldReturnNull()
    {
        // Arrange
        var nonExistingClientId = "nonExistingClient_" + DateTime.Now.Ticks;
        var byteSyncEndpoint = new ByteSyncEndpoint { ClientInstanceId = nonExistingClientId };

        // Act
        var result = await _repository.Get(byteSyncEndpoint);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetClientsWithoutConnectionId_ShouldReturnEmptySet()
    {
        // Act
        var result = await _repository.GetClientsWithoutConnectionId();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Test]
    public async Task RemoveClient_ShouldDeleteClientFromCache()
    {
        // Arrange
        var clientInstanceId = "testClient_" + DateTime.Now.Ticks;
        var client = new Client
        {
            ClientInstanceId = clientInstanceId
        };

        var cacheKey = _redisInfrastructureService.ComputeCacheKey(EntityType.Client, clientInstanceId);
        await _cacheRepository.Save(cacheKey, client);
        
        // Vérification que le client est présent dans le cache avant de le supprimer
        var beforeDelete = await _cacheRepository.Get(cacheKey);
        beforeDelete.Should().NotBeNull();

        // Act
        await _repository.RemoveClient(client);

        // Assert
        var afterDelete = await _cacheRepository.Get(cacheKey);
        afterDelete.Should().BeNull();
    }
}
