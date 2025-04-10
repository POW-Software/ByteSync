using ByteSync.Common.Business.Actions;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Factories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Repositories;
using ByteSync.ServerCommon.Services;
using ByteSync.ServerCommon.Tests.Helpers;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Tests.Repositories;

public class SynchronizationRepositoryTests
{
    [Test]
    public async Task AddSynchronization_IntegrationTest()
    {
        // Arrange
        var (repository, cacheRepository, redisInfrastructureService, actionsGroupDefRepo) = SetupRepositoryAndDependencies();
        
        string sessionId = "testSession_" + DateTime.Now.Ticks;
        var synchronizationEntity = new SynchronizationEntity { SessionId = sessionId };
        
        var actionsGroupDefinitions = new List<ActionsGroupDefinition>
        {
            new() { ActionsGroupId = "group1" },
            new() { ActionsGroupId = "group2" }
        };

        // Act
        await repository.AddSynchronization(synchronizationEntity, actionsGroupDefinitions);

        // Assert
        var cacheKey = redisInfrastructureService.ComputeCacheKey(EntityType.Synchronization, sessionId);
        var savedEntity = await cacheRepository.Get(cacheKey);
        
        savedEntity.Should().NotBeNull();
        savedEntity.Should().BeEquivalentTo(synchronizationEntity);
        
        A.CallTo(() => actionsGroupDefRepo.AddOrUpdateActionsGroupDefinitions(
            sessionId, 
            A<List<ActionsGroupDefinition>>.That.IsSameSequenceAs(actionsGroupDefinitions)))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task ResetSession_IntegrationTest()
    {
        // Arrange
        var (repository, cacheRepository, redisInfrastructureService, actionsGroupDefRepo) = SetupRepositoryAndDependencies();
        
        string sessionId = "testSession_" + DateTime.Now.Ticks;
        var synchronizationEntity = new SynchronizationEntity { SessionId = sessionId };
        
        var cacheKey = redisInfrastructureService.ComputeCacheKey(EntityType.Synchronization, sessionId);
        await cacheRepository.Save(cacheKey, synchronizationEntity);

        // Verify entity exists before resetting
        var entityBeforeReset = await cacheRepository.Get(cacheKey);
        entityBeforeReset.Should().NotBeNull();

        // Act
        await repository.ResetSession(sessionId);

        // Assert
        var entityAfterReset = await cacheRepository.Get(cacheKey);
        entityAfterReset.Should().BeNull();
        
        A.CallTo(() => actionsGroupDefRepo.DeleteActionsGroupDefinitions(sessionId))
            .MustHaveHappenedOnceExactly();
    }

    private (SynchronizationRepository, 
             CacheRepository<SynchronizationEntity>, 
             RedisInfrastructureService, 
             IActionsGroupDefinitionsRepository) SetupRepositoryAndDependencies()
    {
        var redisSettings = TestSettingsInitializer.GetRedisSettings();
        var cacheKeyFactory = new CacheKeyFactory(Options.Create(redisSettings));
        var loggerFactoryMock = A.Fake<ILoggerFactory>();
        
        var redisInfrastructureService = new RedisInfrastructureService(
            Options.Create(redisSettings), 
            cacheKeyFactory, 
            loggerFactoryMock);
            
        var cacheRepository = new CacheRepository<SynchronizationEntity>(redisInfrastructureService);
        var actionsGroupDefinitionsRepository = A.Fake<IActionsGroupDefinitionsRepository>();
        
        var repository = new SynchronizationRepository(
            redisInfrastructureService, 
            cacheRepository, 
            actionsGroupDefinitionsRepository);
        
        return (repository, cacheRepository, redisInfrastructureService, actionsGroupDefinitionsRepository);
    }
}
