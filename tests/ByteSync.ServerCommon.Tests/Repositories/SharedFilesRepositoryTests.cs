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

[TestFixture]
public class SharedFilesRepositoryTests
{
    private SharedFilesRepository _repository;
    private CacheRepository<SharedFileData> _cacheRepository;
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
            
        _cacheRepository = new CacheRepository<SharedFileData>(_redisInfrastructureService);
        
        _repository = new SharedFilesRepository(_redisInfrastructureService, _cacheRepository);
    }
    
    [Test]
    public async Task AddOrUpdate_IntegrationTest()
    {
        // Arrange
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
        await _repository.AddOrUpdate(sharedFileDefinition, updateHandler);

        // Assert
        var cacheKey = _redisInfrastructureService.ComputeCacheKey(EntityType.SharedFile, sharedFileDefinition.Id);
        var value = await _cacheRepository.Get(cacheKey);
        value.Should().NotBeNull();
        value.Should().BeOfType<SharedFileData>();
        value.SharedFileDefinition.Should().BeEquivalentTo(sharedFileDefinition);
    }
    
    [Test]
    public async Task Forget_IntegrationTest()
    {
        // Arrange
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
        await _repository.AddOrUpdate(sharedFileDefinition, updateHandler);
        await _repository.Forget(sharedFileDefinition);

        // Assert
        var cacheKey = _redisInfrastructureService.ComputeCacheKey(EntityType.SharedFile, sharedFileDefinition.Id);
        var value = await _cacheRepository.Get(cacheKey);
        value.Should().BeNull();
    }
    
    [Test]
    public async Task Clear_IntegrationTest()
    {
        // Arrange
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
        await _repository.AddOrUpdate(sharedFileDefinition, updateHandler);
        await _repository.Clear(sharedFileDefinition.SessionId);

        // Assert
        var cacheKey = _redisInfrastructureService.ComputeCacheKey(EntityType.SharedFile, sharedFileDefinition.Id);
        var value = await _cacheRepository.Get(cacheKey);
        value.Should().BeNull();
    }
}
