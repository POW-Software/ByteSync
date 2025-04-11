using ByteSync.Common.Business.Actions;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Factories;
using ByteSync.ServerCommon.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Misc;
using ByteSync.ServerCommon.Repositories;
using ByteSync.ServerCommon.Services;
using ByteSync.ServerCommon.Tests.Helpers;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RedLockNet;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Tests.Repositories;

[TestFixture]
public class TrackingActionRepositoryTests
{
    private TrackingActionRepository _repository;
    private IRedisInfrastructureService _redisInfrastructureService;
    private ISynchronizationRepository _synchronizationRepository;
    private ITrackingActionEntityFactory _trackingActionEntityFactory;
    private ICacheRepository<TrackingActionEntity> _cacheRepository;
    private ILogger<TrackingActionRepository> _logger;
    // private IRedLock _lockMock;
    private ITransaction _transactionMock;
    private ActionsGroupDefinitionsRepository _actionsGroupDefinitionsRepository;

    [SetUp]
    public void SetUp()
    {
        var redisSettings = TestSettingsInitializer.GetRedisSettings();
        var cacheKeyFactory = new CacheKeyFactory(Options.Create(redisSettings));
        var loggerFactoryMock = A.Fake<ILoggerFactory>();
        var loggerMock = A.Fake<ILogger<TrackingActionRepository>>();
        var cosmosDbSettings = TestSettingsInitializer.GetCosmosDbSettings();
        ByteSyncDbContext byteSyncDbContext = new ByteSyncDbContext(Options.Create(cosmosDbSettings));
        byteSyncDbContext.InitializeCosmosDb().Wait();
        _actionsGroupDefinitionsRepository = new ActionsGroupDefinitionsRepository(byteSyncDbContext);
        var trackingActionEntityFactory = new TrackingActionEntityFactory(_actionsGroupDefinitionsRepository);
        var synchronizationRepository = new SynchronizationRepository(
            new RedisInfrastructureService(Options.Create(redisSettings), cacheKeyFactory, loggerFactoryMock),
            new CacheRepository<SynchronizationEntity>(new RedisInfrastructureService(Options.Create(redisSettings), cacheKeyFactory, loggerFactoryMock)),
            _actionsGroupDefinitionsRepository);
        _redisInfrastructureService = new RedisInfrastructureService(Options.Create(redisSettings), cacheKeyFactory, loggerFactoryMock);
        _cacheRepository = new CacheRepository<TrackingActionEntity>(_redisInfrastructureService);
        _repository = new TrackingActionRepository(_redisInfrastructureService, synchronizationRepository, trackingActionEntityFactory, _cacheRepository, 
            loggerMock);
        
        
        // _redisInfrastructureService = A.Fake<IRedisInfrastructureService>();
        // _synchronizationRepository = A.Fake<ISynchronizationRepository>();
        // _trackingActionEntityFactory = A.Fake<ITrackingActionEntityFactory>();
        // _cacheRepository = A.Fake<ICacheRepository<TrackingActionEntity>>();
        // _logger = A.Fake<ILogger<TrackingActionRepository>>();
        // _lockMock = A.Fake<IRedLock>();
        // _transactionMock = A.Fake<ITransaction>();
        //
        // _repository = new TrackingActionRepository(
        //     _redisInfrastructureService, 
        //     _synchronizationRepository, 
        //     _trackingActionEntityFactory, 
        //     _cacheRepository, 
        //     _logger);
        //
        // // Configuration des mocks communs
        // A.CallTo(() => _redisInfrastructureService.AcquireLockAsync(A<CacheKey>._))
        //     .Returns(_lockMock);
        // A.CallTo(() => _redisInfrastructureService.OpenTransaction())
        //     .Returns(_transactionMock);
    }

    [Test]
    public void EntityType_ShouldReturnTrackingAction()
    {
        // Assert
        _repository.EntityType.Should().Be(EntityType.TrackingAction);
    }

    [Test]
    public async Task GetOrBuild_WhenEntityExists_ShouldReturnExistingEntity()
    {
        // Arrange
        var sessionId = "session123" + DateTime.Now.Ticks;
        var actionsGroupId = "group456" + DateTime.Now.Ticks;
        var cacheKey = new CacheKey
        {
            EntityType = EntityType.TrackingAction,
            EntityId = $"{sessionId}_{actionsGroupId}",
            Value = $"test:TrackingAction:{sessionId}_{actionsGroupId}"
        };
        var existingEntity = new TrackingActionEntity { ActionsGroupId = actionsGroupId };

        // A.CallTo(() => _redisInfrastructureService.ComputeCacheKey(EntityType.TrackingAction, $"{sessionId}_{actionsGroupId}"))
        //     .Returns(cacheKey);
        // A.CallTo(() => _cacheRepository.Get(cacheKey, A<ITransaction>._))
        //     .Returns(existingEntity);

        List<ActionsGroupDefinition> actionsGroupDefinitions = new List<ActionsGroupDefinition>
        {
            new ActionsGroupDefinition
            {
                ActionsGroupId = actionsGroupId,
            }
        };
        await _actionsGroupDefinitionsRepository.AddOrUpdateActionsGroupDefinitions(sessionId, actionsGroupDefinitions);
        
        // Act
        var result = await _repository.GetOrBuild(sessionId, actionsGroupId);

        // Assert
        result.Should().BeEquivalentTo(existingEntity);
        // A.CallTo(() => _redisInfrastructureService.AcquireLockAsync(cacheKey)).MustHaveHappenedOnceExactly();
        // A.CallTo(() => _trackingActionEntityFactory.Create(A<string>._, A<string>._)).MustNotHaveHappened();
    }
    
    [Test]
    public async Task GetOrBuild_WhenEntityDoesNotExist_ShouldCreateAndSaveNewEntity()
    {
        // Arrange
        var sessionId = "session123";
        var actionsGroupId = "group456";
        var cacheKey = new CacheKey
        {
            EntityType = EntityType.TrackingAction,
            EntityId = $"{sessionId}_{actionsGroupId}",
            Value = $"test:TrackingAction:{sessionId}_{actionsGroupId}"
        };
        var newEntity = new TrackingActionEntity { ActionsGroupId = actionsGroupId };

        A.CallTo(() => _redisInfrastructureService.ComputeCacheKey(EntityType.TrackingAction, $"{sessionId}_{actionsGroupId}"))
            .Returns(cacheKey);
        A.CallTo(() => _cacheRepository.Get(cacheKey, A<ITransaction>._))
            .Returns(default(TrackingActionEntity));
        A.CallTo(() => _trackingActionEntityFactory.Create(sessionId, actionsGroupId))
            .Returns(newEntity);

        // Act
        var result = await _repository.GetOrBuild(sessionId, actionsGroupId);

        // Assert
        result.Should().Be(newEntity);
        A.CallTo(() => _trackingActionEntityFactory.Create(sessionId, actionsGroupId)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _cacheRepository.Save(cacheKey, newEntity, null, null)).MustHaveHappenedOnceExactly();
    }
    
    [Test]
    public async Task AddOrUpdate_WhenAllUpdatesSucceed_ShouldUpdateAllEntitiesAndSynchronization()
    {
        // Arrange
        var sessionId = "session123";
        var actionsGroupIds = new List<string> { "group1", "group2" };
        var syncEntity = new SynchronizationEntity { SessionId = sessionId };
        var trackingEntity1 = new TrackingActionEntity { ActionsGroupId = actionsGroupIds[0] };
        var trackingEntity2 = new TrackingActionEntity { ActionsGroupId = actionsGroupIds[1] };
        var syncCacheKey = new CacheKey
        {
            EntityType = EntityType.Synchronization,
            EntityId = sessionId,
            Value = $"test:Synchronization:{sessionId}"
        };
        var cacheKey1 = new CacheKey
        {
            EntityType = EntityType.TrackingAction,
            EntityId = $"{sessionId}_{actionsGroupIds[0]}",
            Value = $"test:TrackingAction:{sessionId}_{actionsGroupIds[0]}"
        };
        var cacheKey2 = new CacheKey
        {
            EntityType = EntityType.TrackingAction,
            EntityId = $"{sessionId}_{actionsGroupIds[1]}",
            Value = $"test:TrackingAction:{sessionId}_{actionsGroupIds[1]}"
        };
        

        bool updateHandlerResult = true;
        Func<TrackingActionEntity, SynchronizationEntity, bool> updateHandler = (_, _) => updateHandlerResult;

        A.CallTo(() => _redisInfrastructureService.ComputeCacheKey(EntityType.Synchronization, sessionId))
            .Returns(syncCacheKey);
        A.CallTo(() => _synchronizationRepository.Get(sessionId))
            .Returns(syncEntity);
        A.CallTo(() => _redisInfrastructureService.ComputeCacheKey(EntityType.TrackingAction, $"{sessionId}_{actionsGroupIds[0]}"))
            .Returns(cacheKey1);
        A.CallTo(() => _redisInfrastructureService.ComputeCacheKey(EntityType.TrackingAction, $"{sessionId}_{actionsGroupIds[1]}"))
            .Returns(cacheKey2);
        A.CallTo(() => _cacheRepository.Get(cacheKey1, A<ITransaction>._))
            .Returns(trackingEntity1);
        A.CallTo(() => _cacheRepository.Get(cacheKey2, A<ITransaction>._))
            .Returns(trackingEntity2);

        // Act
        var result = await _repository.AddOrUpdate(sessionId, actionsGroupIds, updateHandler);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.TrackingActionEntities.Should().Contain(trackingEntity1);
        result.TrackingActionEntities.Should().Contain(trackingEntity2);
        result.SynchronizationEntity.Should().Be(syncEntity);

        A.CallTo(() => _cacheRepository.Save(cacheKey1, trackingEntity1, _transactionMock, null)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _cacheRepository.Save(cacheKey2, trackingEntity2, _transactionMock, null)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _synchronizationRepository.Save(syncCacheKey, syncEntity, _transactionMock, null)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _transactionMock.ExecuteAsync(A<CommandFlags>._)).MustHaveHappenedOnceExactly();
    }

    
    [Test]
    public async Task AddOrUpdate_WhenUpdateFails_ShouldNotExecuteTransaction()
    {
        // Arrange
        var sessionId = "session123";
        var actionsGroupIds = new List<string> { "group1", "group2" };
        var syncEntity = new SynchronizationEntity { SessionId = sessionId };
        var trackingEntity1 = new TrackingActionEntity { ActionsGroupId = actionsGroupIds[0] };
        var syncCacheKey = new CacheKey
        {
            EntityType = EntityType.Synchronization,
            EntityId = sessionId,
            Value = $"test:Synchronization:{sessionId}"
        };
        var cacheKey1 = new CacheKey
        {
            EntityType = EntityType.TrackingAction,
            EntityId = $"{sessionId}_{actionsGroupIds[0]}",
            Value = $"test:TrackingAction:{sessionId}_{actionsGroupIds[0]}"
        };

        bool updateHandlerResult = false;
        Func<TrackingActionEntity, SynchronizationEntity, bool> updateHandler = (_, _) => updateHandlerResult;

        A.CallTo(() => _redisInfrastructureService.ComputeCacheKey(EntityType.Synchronization, sessionId))
            .Returns(syncCacheKey);
        A.CallTo(() => _synchronizationRepository.Get(sessionId))
            .Returns(syncEntity);
        A.CallTo(() => _redisInfrastructureService.ComputeCacheKey(EntityType.TrackingAction, $"{sessionId}_{actionsGroupIds[0]}"))
            .Returns(cacheKey1);
        A.CallTo(() => _cacheRepository.Get(cacheKey1, A<ITransaction>._))
            .Returns(trackingEntity1);

        // Act
        var result = await _repository.AddOrUpdate(sessionId, actionsGroupIds, updateHandler);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.TrackingActionEntities.Should().BeEmpty();
        result.SynchronizationEntity.Should().Be(syncEntity);

        A.CallTo(() => _transactionMock.ExecuteAsync(A<CommandFlags>._)).MustNotHaveHappened();
    }

    
    [TearDown]
    public void Teardown()
    {
        // _lockMock.Dispose();
    }
}
