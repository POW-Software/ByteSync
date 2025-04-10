using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Repositories;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
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
    private IRedLock _lockMock;
    private ITransaction _transactionMock;

    [SetUp]
    public void SetUp()
    {
        _redisInfrastructureService = A.Fake<IRedisInfrastructureService>();
        _synchronizationRepository = A.Fake<ISynchronizationRepository>();
        _trackingActionEntityFactory = A.Fake<ITrackingActionEntityFactory>();
        _cacheRepository = A.Fake<ICacheRepository<TrackingActionEntity>>();
        _logger = A.Fake<ILogger<TrackingActionRepository>>();
        _lockMock = A.Fake<IRedLock>();
        _transactionMock = A.Fake<ITransaction>();

        _repository = new TrackingActionRepository(
            _redisInfrastructureService, 
            _synchronizationRepository, 
            _trackingActionEntityFactory, 
            _cacheRepository, 
            _logger);

        // Configuration des mocks communs
        A.CallTo(() => _redisInfrastructureService.AcquireLockAsync(A<CacheKey>._))
            .Returns(_lockMock);
        A.CallTo(() => _redisInfrastructureService.OpenTransaction())
            .Returns(_transactionMock);
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
        var sessionId = "session123";
        var actionsGroupId = "group456";
        var cacheKey = new CacheKey
        {
            EntityType = EntityType.TrackingAction,
            EntityId = $"{sessionId}_{actionsGroupId}",
            Value = $"test:TrackingAction:{sessionId}_{actionsGroupId}"
        };
        var existingEntity = new TrackingActionEntity { ActionsGroupId = actionsGroupId };

        A.CallTo(() => _redisInfrastructureService.ComputeCacheKey(EntityType.TrackingAction, $"{sessionId}_{actionsGroupId}"))
            .Returns(cacheKey);
        A.CallTo(() => _cacheRepository.Get(cacheKey, A<ITransaction>._))
            .Returns(existingEntity);

        // Act
        var result = await _repository.GetOrBuild(sessionId, actionsGroupId);

        // Assert
        result.Should().Be(existingEntity);
        A.CallTo(() => _redisInfrastructureService.AcquireLockAsync(cacheKey)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _trackingActionEntityFactory.Create(A<string>._, A<string>._)).MustNotHaveHappened();
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

    
    [Test]
    public async Task AddOrUpdate_ShouldReleaseAllLocksEvenOnFailure()
    {
        // Arrange
        var sessionId = "session123";
        var actionsGroupIds = new List<string> { "group1", "group2" };
        var syncEntity = new SynchronizationEntity { SessionId = sessionId };
        var lockMock1 = A.Fake<IAsyncDisposable>();
        var lockMock2 = A.Fake<IAsyncDisposable>();

        A.CallTo(() => _redisInfrastructureService.AcquireLockAsync(A<CacheKey>.That.Matches(c => c.EntityId == sessionId)))
            .Returns(_lockMock);
        A.CallTo(() => _redisInfrastructureService.AcquireLockAsync(A<CacheKey>.That.Matches(c => c.EntityId == $"{sessionId}_{actionsGroupIds[0]}")))
            .Returns(lockMock1);
        A.CallTo(() => _redisInfrastructureService.AcquireLockAsync(A<CacheKey>.That.Matches(c => c.EntityId == $"{sessionId}_{actionsGroupIds[1]}")))
            .Returns(lockMock2);
        A.CallTo(() => _synchronizationRepository.Get(sessionId))
            .Returns(syncEntity);
        A.CallTo(() => _cacheRepository.Get(A<string>._))
            .Returns(new TrackingActionEntity());

        // Simuler un échec de l'update
        Func<TrackingActionEntity, SynchronizationEntity, bool> updateHandler = (entity, sync) => false;

        // Act
        await _repository.AddOrUpdate(sessionId, actionsGroupIds, updateHandler);

        // Assert
        A.CallTo(() => lockMock1.DisposeAsync()).MustHaveHappenedOnceExactly();
        A.CallTo(() => lockMock2.DisposeAsync()).MustHaveHappenedOnceExactly();
    }
    
    [Test]
    public async Task AddOrUpdate_WithIntegrationTest()
    {
        // Configuration de l'intégration
        var redisSettings = TestSettingsInitializer.GetRedisSettings();
        var cacheKeyFactory = new CacheKeyFactory(Options.Create(redisSettings));
        var loggerFactoryMock = A.Fake<ILoggerFactory>();
        var logger = A.Fake<ILogger<TrackingActionRepository>>();
        A.CallTo(() => loggerFactoryMock.CreateLogger(A<string>._)).Returns(logger);

        var redisService = new RedisInfrastructureService(
            Options.Create(redisSettings),
            cacheKeyFactory,
            loggerFactoryMock);

        var cacheRepo = new CacheRepository<TrackingActionEntity>(redisService);
        var syncRepo = A.Fake<ISynchronizationRepository>();
        var factory = A.Fake<ITrackingActionEntityFactory>();

        var repository = new TrackingActionRepository(
            redisService,
            syncRepo,
            factory,
            cacheRepo,
            logger);

        // Données de test
        var sessionId = "testSession_" + DateTime.Now.Ticks;
        var actionsGroupId = "testActionsGroup_" + DateTime.Now.Ticks;
        var entity = new TrackingActionEntity
        {
            ActionsGroupId = actionsGroupId,
            SourceClientInstanceId = "source123",
            IsSourceSuccess = true,
            Size = 1024
        };
        entity.TargetClientInstanceIds.Add("target1");
        entity.TargetClientInstanceIds.Add("target2");

        var syncEntity = new SynchronizationEntity
        {
            SessionId = sessionId,
            LastSyncTime = DateTime.UtcNow
        };

        // Configure les mocks
        A.CallTo(() => syncRepo.Get(sessionId)).Returns(syncEntity);
        A.CallTo(() => factory.Create(sessionId, actionsGroupId)).Returns(entity);

        // Act - Récupère d'abord l'entité
        var retrievedEntity = await repository.GetOrBuild(sessionId, actionsGroupId);

        // Assert
        retrievedEntity.Should().NotBeNull();
        retrievedEntity.ActionsGroupId.Should().Be(actionsGroupId);

        // Vérifie qu'on peut ajouter un succès sur une cible
        retrievedEntity.AddSuccessOnTarget("target1");
        retrievedEntity.SuccessTargetClientInstanceIds.Should().Contain("target1");

        // Vérifie qu'on peut ajouter une erreur sur une cible
        retrievedEntity.AddErrorOnTarget("target2");
        retrievedEntity.ErrorTargetClientInstanceIds.Should().Contain("target2");

        // Test la propriété IsFinished
        retrievedEntity.IsFinished.Should().BeTrue();
        retrievedEntity.IsSuccess.Should().BeFalse(); // Car il y a une erreur sur target2
        retrievedEntity.IsError.Should().BeTrue();
        retrievedEntity.IsErrorOnTarget.Should().BeTrue();
    }
    
    [TearDown]
    public void Teardown()
    {
        _lockMock.Dispose();
    }
}
