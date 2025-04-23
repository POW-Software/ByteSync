using ByteSync.Common.Business.Actions;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Factories;
using ByteSync.ServerCommon.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Repositories;
using ByteSync.ServerCommon.Services;
using ByteSync.ServerCommon.Tests.Helpers;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Tests.Repositories;

[TestFixture]
public class TrackingActionRepositoryTests
{
    private TrackingActionRepository _repository;
    private IRedisInfrastructureService _redisInfrastructureService;
    private ISynchronizationRepository _synchronizationRepository;
    private ICacheRepository<TrackingActionEntity> _cacheRepository;
    private ICacheRepository<SynchronizationEntity> _synchronizationCacheRepository;

    [SetUp]
    public void SetUp()
    {
        var redisSettings = TestSettingsInitializer.GetRedisSettings();
        var cacheKeyFactory = new CacheKeyFactory(Options.Create(redisSettings));
        var loggerFactoryMock = A.Fake<ILoggerFactory>();
        var loggerMock = A.Fake<ILogger<TrackingActionRepository>>();
        _synchronizationRepository = new SynchronizationRepository(
            new RedisInfrastructureService(Options.Create(redisSettings), cacheKeyFactory, loggerFactoryMock),
            new CacheRepository<SynchronizationEntity>(new RedisInfrastructureService(Options.Create(redisSettings), cacheKeyFactory, loggerFactoryMock)),
            new CacheRepository<TrackingActionEntity>(new RedisInfrastructureService(Options.Create(redisSettings), cacheKeyFactory, loggerFactoryMock)));
        _redisInfrastructureService = new RedisInfrastructureService(Options.Create(redisSettings), cacheKeyFactory, loggerFactoryMock);
        _cacheRepository = new CacheRepository<TrackingActionEntity>(_redisInfrastructureService);
        _synchronizationCacheRepository = new CacheRepository<SynchronizationEntity>(_redisInfrastructureService);
        _repository = new TrackingActionRepository(_redisInfrastructureService, _synchronizationRepository, _cacheRepository, 
            _synchronizationCacheRepository, loggerMock);
    }

    [Test]
    public async Task AddOrUpdate_WhenAllUpdatesSucceed_ShouldReturnUpdatedEntities()
    {
        // Arrange
        var sessionId = "session_" + DateTime.Now.Ticks;
        var actionsGroupIds = new List<string> { "group1", "group2" };
        var synchronizationEntity = new SynchronizationEntity { SessionId = sessionId };
        
        List<ActionsGroupDefinition> actionsGroupDefinitions =
        [
            new()
            {
                ActionsGroupId = "group1",
            },

            new()
            {
                ActionsGroupId = "group2",
            }
        ];
        
        await _synchronizationRepository.AddSynchronization(synchronizationEntity, actionsGroupDefinitions);

        Func<TrackingActionEntity, SynchronizationEntity, TrackingActionUpdateHandlerResult> updateHandler = (_, _) =>
        {
            return new TrackingActionUpdateHandlerResult(true);
        };

        // Act
        var result = await _repository.AddOrUpdate(sessionId, actionsGroupIds, updateHandler);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.TrackingActionEntities.Should().HaveCount(actionsGroupIds.Count);
        result.TrackingActionEntities.Should().AllSatisfy(entity =>
        {
            entity.Should().NotBeNull();
            entity.ActionsGroupId.Should().NotBeNullOrEmpty();
            actionsGroupDefinitions.Any(a => a.ActionsGroupId == entity.ActionsGroupId).Should().BeTrue();
        });
        result.SynchronizationEntity.Should().BeEquivalentTo(synchronizationEntity);
    }
}
