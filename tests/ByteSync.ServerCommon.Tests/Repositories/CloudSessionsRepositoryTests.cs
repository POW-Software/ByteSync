using ByteSync.ServerCommon.Business.Auth;
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

public class CloudSessionsRepositoryTests
{
    [Test]
    public async Task GetSessionMember_WithValidData_ReturnsCorrectSessionMember()
    {
        // Arrange
        var redisSettings = TestSettingsInitializer.GetRedisSettings();
        var cacheKeyFactory = new CacheKeyFactory(Options.Create(redisSettings));
        var loggerFactoryMock = A.Fake<ILoggerFactory>();
        var redisInfrastructureService = new RedisInfrastructureService(Options.Create(redisSettings), cacheKeyFactory, loggerFactoryMock);
        var cacheRepository = new CacheRepository<CloudSessionData>(redisInfrastructureService);
        var repository = new CloudSessionsRepository(redisInfrastructureService, cacheRepository);

        var sessionId = "testSession_" + DateTime.Now.Ticks;
        var clientInstanceId = "testClient_" + DateTime.Now.Ticks;
        
        var sessionMember = new SessionMemberData
        {
            ClientInstanceId = clientInstanceId
        };
        
        var cloudSessionData = new CloudSessionData
        {
            SessionId = sessionId,
            SessionMembers = new List<SessionMemberData> { sessionMember }
        };

        // Save the session data first
        var cacheKey = redisInfrastructureService.ComputeCacheKey(EntityType.Session, sessionId);
        await cacheRepository.Save(cacheKey, cloudSessionData);

        // Act
        var result = await repository.GetSessionMember(sessionId, clientInstanceId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(sessionMember);
    }

    [Test]
    public async Task GetSessionMember_WithClient_ReturnsCorrectSessionMember()
    {
        // Arrange
        var redisSettings = TestSettingsInitializer.GetRedisSettings();
        var cacheKeyFactory = new CacheKeyFactory(Options.Create(redisSettings));
        var loggerFactoryMock = A.Fake<ILoggerFactory>();
        var redisInfrastructureService = new RedisInfrastructureService(Options.Create(redisSettings), cacheKeyFactory, loggerFactoryMock);
        var cacheRepository = new CacheRepository<CloudSessionData>(redisInfrastructureService);
        var repository = new CloudSessionsRepository(redisInfrastructureService, cacheRepository);

        var sessionId = "testSession_" + DateTime.Now.Ticks;
        var clientInstanceId = "testClient_" + DateTime.Now.Ticks;
        
        var client = new Client
        {
            ClientInstanceId = clientInstanceId
        };
        
        var sessionMember = new SessionMemberData
        {
            ClientInstanceId = clientInstanceId
        };
        
        var cloudSessionData = new CloudSessionData
        {
            SessionId = sessionId,
            SessionMembers = new List<SessionMemberData> { sessionMember }
        };

        // Save the session data first
        var cacheKey = redisInfrastructureService.ComputeCacheKey(EntityType.Session, sessionId);
        await cacheRepository.Save(cacheKey, cloudSessionData);

        // Act
        var result = await repository.GetSessionMember(sessionId, client);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(sessionMember);
    }

    [Test]
    public async Task GetSessionPreMember_WithValidData_ReturnsCorrectPreSessionMember()
    {
        // Arrange
        var redisSettings = TestSettingsInitializer.GetRedisSettings();
        var cacheKeyFactory = new CacheKeyFactory(Options.Create(redisSettings));
        var loggerFactoryMock = A.Fake<ILoggerFactory>();
        var redisInfrastructureService = new RedisInfrastructureService(Options.Create(redisSettings), cacheKeyFactory, loggerFactoryMock);
        var cacheRepository = new CacheRepository<CloudSessionData>(redisInfrastructureService);
        var repository = new CloudSessionsRepository(redisInfrastructureService, cacheRepository);

        var sessionId = "testSession_" + DateTime.Now.Ticks;
        var clientInstanceId = "testClient_" + DateTime.Now.Ticks;
        
        var preSessionMember = new SessionMemberData
        {
            ClientInstanceId = clientInstanceId
        };
        
        var cloudSessionData = new CloudSessionData
        {
            SessionId = sessionId,
            PreSessionMembers = new List<SessionMemberData> { preSessionMember }
        };

        // Save the session data first
        var cacheKey = redisInfrastructureService.ComputeCacheKey(EntityType.Session, sessionId);
        await cacheRepository.Save(cacheKey, cloudSessionData);

        // Act
        var result = await repository.GetSessionPreMember(sessionId, clientInstanceId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(preSessionMember);
    }

    [Test]
    public async Task AddCloudSession_IntegrationTest()
    {
        // Arrange
        var redisSettings = TestSettingsInitializer.GetRedisSettings();
        var cacheKeyFactory = new CacheKeyFactory(Options.Create(redisSettings));
        var loggerFactoryMock = A.Fake<ILoggerFactory>();
        var redisInfrastructureService = new RedisInfrastructureService(Options.Create(redisSettings), cacheKeyFactory, loggerFactoryMock);
        var cacheRepository = new CacheRepository<CloudSessionData>(redisInfrastructureService);
        var repository = new CloudSessionsRepository(redisInfrastructureService, cacheRepository);

        var sessionId = "testSession_" + DateTime.Now.Ticks;
        
        var cloudSessionData = new CloudSessionData
        {
            SessionId = "temporary", // This will be overwritten by the generateSessionIdHandler
            SessionMembers = new List<SessionMemberData>(),
            PreSessionMembers = new List<SessionMemberData>()
        };

        Func<string> generateSessionIdHandler = () => sessionId;
        var transaction = redisInfrastructureService.OpenTransaction();

        // Act
        var result = await repository.AddCloudSession(cloudSessionData, generateSessionIdHandler, transaction);
        await transaction.ExecuteAsync();

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be(sessionId);
        
        var cacheKey = redisInfrastructureService.ComputeCacheKey(EntityType.Session, sessionId);
        var value = await cacheRepository.Get(cacheKey);
        value.Should().NotBeNull();
        value.Should().BeOfType<CloudSessionData>();
        value.SessionId.Should().Be(sessionId);
    }
}
