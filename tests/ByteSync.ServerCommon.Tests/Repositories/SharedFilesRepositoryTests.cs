using ByteSync.Common.Business.SharedFiles;
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

public class SharedFilesRepositoryTests
{
    public SharedFilesRepositoryTests()
    {

    }
    
    [Test]
    public async Task AddOrUpdate_IntegrationTest()
    {
        // Arrange
        var redisSettings = TestSettingsInitializer.GetRedisSettings();
        var cacheKeyFactory = new CacheKeyFactory(Options.Create(redisSettings));
        var loggerFactoryMock = A.Fake<ILoggerFactory>();
        var redisInfrastructureService = new RedisInfrastructureService(Options.Create(redisSettings), cacheKeyFactory, loggerFactoryMock);
        var cacheRepository = new CacheRepository<SharedFileData>(redisInfrastructureService);
        var repository = new SharedFilesRepository(redisInfrastructureService, cacheRepository);

        var sharedFileDefinition = new SharedFileDefinition
        {
            SessionId = "testSession_" + DateTime.Now.Ticks,
            Id = "testSharedFile_" + DateTime.Now.Ticks,
        };

        Func<SharedFileData?, SharedFileData> updateHandler = _ =>
        {
            var sharedFileData = new SharedFileData(sharedFileDefinition, new List<string>());

            return sharedFileData;
        };

        // Act
        await repository.AddOrUpdate(sharedFileDefinition, updateHandler);

        // Assert
        var cacheKey = redisInfrastructureService.ComputeCacheKey(EntityType.SharedFile, sharedFileDefinition.Id);
        var value = await cacheRepository.Get(cacheKey);
        value.Should().NotBeNull();
        value.Should().BeOfType<SharedFileData>();
        value.SharedFileDefinition.Should().BeEquivalentTo(sharedFileDefinition);
    }
    
    [Test]
    public async Task Forget_IntegrationTest()
    {
        // Arrange
        var redisSettings = TestSettingsInitializer.GetRedisSettings();
        var cacheKeyFactory = new CacheKeyFactory(Options.Create(redisSettings));
        var loggerFactoryMock = A.Fake<ILoggerFactory>();
        var redisInfrastructureService = new RedisInfrastructureService(Options.Create(redisSettings), cacheKeyFactory, loggerFactoryMock);
        var cacheRepository = new CacheRepository<SharedFileData>(redisInfrastructureService);
        var repository = new SharedFilesRepository(redisInfrastructureService, cacheRepository);

        var sharedFileDefinition = new SharedFileDefinition
        {
            SessionId = "testSession_" + DateTime.Now.Ticks,
            Id = "testSharedFile_" + DateTime.Now.Ticks,
        };

        Func<SharedFileData?, SharedFileData> updateHandler = _ =>
        {
            var sharedFileData = new SharedFileData(sharedFileDefinition, new List<string>());

            return sharedFileData;
        };

        // Act
        await repository.AddOrUpdate(sharedFileDefinition, updateHandler);
        await repository.Forget(sharedFileDefinition);

        // Assert
        var cacheKey = redisInfrastructureService.ComputeCacheKey(EntityType.SharedFile, sharedFileDefinition.Id);
        var value = await cacheRepository.Get(cacheKey);
        value.Should().BeNull();
    }
    
    [Test]
    public async Task Clear_IntegrationTest()
    {
        // Arrange
        var redisSettings = TestSettingsInitializer.GetRedisSettings();
        var cacheKeyFactory = new CacheKeyFactory(Options.Create(redisSettings));
        var loggerFactoryMock = A.Fake<ILoggerFactory>();
        var redisInfrastructureService = new RedisInfrastructureService(Options.Create(redisSettings), cacheKeyFactory, loggerFactoryMock);
        var cacheRepository = new CacheRepository<SharedFileData>(redisInfrastructureService);
        var repository = new SharedFilesRepository(redisInfrastructureService, cacheRepository);

        var sharedFileDefinition = new SharedFileDefinition
        {
            SessionId = "testSession_" + DateTime.Now.Ticks,
            Id = "testSharedFile_" + DateTime.Now.Ticks,
        };

        Func<SharedFileData?, SharedFileData> updateHandler = _ =>
        {
            var sharedFileData = new SharedFileData(sharedFileDefinition, new List<string>());

            return sharedFileData;
        };

        // Act
        await repository.AddOrUpdate(sharedFileDefinition, updateHandler);
        await repository.Clear(sharedFileDefinition.SessionId);

        // Assert
        var cacheKey = redisInfrastructureService.ComputeCacheKey(EntityType.SharedFile, sharedFileDefinition.Id);
        var value = await cacheRepository.Get(cacheKey);
        value.Should().BeNull();
    }
}