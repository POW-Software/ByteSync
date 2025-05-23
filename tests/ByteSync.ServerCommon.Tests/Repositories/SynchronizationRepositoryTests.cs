﻿using ByteSync.Common.Business.Actions;
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

[TestFixture]
public class SynchronizationRepositoryTests
{
    private SynchronizationRepository _repository;
    private CacheRepository<SynchronizationEntity> _synchronizationEntityCacheRepository;
    private RedisInfrastructureService _redisInfrastructureService;
    private CacheRepository<TrackingActionEntity> _trackingActionEntityCacheRepository;

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

        _synchronizationEntityCacheRepository = new CacheRepository<SynchronizationEntity>(_redisInfrastructureService);
        _trackingActionEntityCacheRepository = new CacheRepository<TrackingActionEntity>(_redisInfrastructureService);
        
        _repository = new SynchronizationRepository(
            _redisInfrastructureService, 
            _synchronizationEntityCacheRepository, 
            _trackingActionEntityCacheRepository);
    }

    
    [Test]
    public async Task AddSynchronization_IntegrationTest()
    {
        // Arrange
        string sessionId = "testSession_" + DateTime.Now.Ticks;
        var synchronizationEntity = new SynchronizationEntity { SessionId = sessionId };
        
        var actionsGroupDefinitions = new List<ActionsGroupDefinition>
        {
            new() { ActionsGroupId = "group1" },
            new() { ActionsGroupId = "group2" }
        };

        // Act
        await _repository.AddSynchronization(synchronizationEntity, actionsGroupDefinitions);

        // Assert
        var cacheKey = _redisInfrastructureService.ComputeCacheKey(EntityType.Synchronization, sessionId);
        var savedEntity = await _synchronizationEntityCacheRepository.Get(cacheKey);
        
        savedEntity.Should().NotBeNull();
        savedEntity.Should().BeEquivalentTo(synchronizationEntity);
    }

    [Test]
    public async Task ResetSession_IntegrationTest()
    {
        // Arrange
        string sessionId = "testSession_" + DateTime.Now.Ticks;
        var synchronizationEntity = new SynchronizationEntity { SessionId = sessionId };
        
        var cacheKey = _redisInfrastructureService.ComputeCacheKey(EntityType.Synchronization, sessionId);
        await _synchronizationEntityCacheRepository.Save(cacheKey, synchronizationEntity);

        // Verify entity exists before resetting
        var entityBeforeReset = await _synchronizationEntityCacheRepository.Get(cacheKey);
        entityBeforeReset.Should().NotBeNull();

        // Act
        await _repository.ResetSession(sessionId);

        // Assert
        var entityAfterReset = await _synchronizationEntityCacheRepository.Get(cacheKey);
        entityAfterReset.Should().BeNull();
    }
}
