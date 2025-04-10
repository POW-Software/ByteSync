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

[TestFixture]
public class CloudSessionsRepositoryTests
{
    private CloudSessionsRepository _repository;
    private CacheRepository<CloudSessionData> _cacheRepository;
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
            
        _cacheRepository = new CacheRepository<CloudSessionData>(_redisInfrastructureService);
        
        _repository = new CloudSessionsRepository(_redisInfrastructureService, _cacheRepository);
    }

    [Test]
    public async Task GetSessionMember_WithValidData_ReturnsCorrectSessionMember()
    {
        // Arrange
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
        var cacheKey = _redisInfrastructureService.ComputeCacheKey(EntityType.Session, sessionId);
        await _cacheRepository.Save(cacheKey, cloudSessionData);

        // Act
        var result = await _repository.GetSessionMember(sessionId, clientInstanceId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(sessionMember);
    }

    [Test]
    public async Task GetSessionMember_WithClient_ReturnsCorrectSessionMember()
    {
        // Arrange
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
        var cacheKey = _redisInfrastructureService.ComputeCacheKey(EntityType.Session, sessionId);
        await _cacheRepository.Save(cacheKey, cloudSessionData);

        // Act
        var result = await _repository.GetSessionMember(sessionId, client);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(sessionMember);
    }

    [Test]
    public async Task GetSessionPreMember_WithValidData_ReturnsCorrectPreSessionMember()
    {
        // Arrange
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
        var cacheKey = _redisInfrastructureService.ComputeCacheKey(EntityType.Session, sessionId);
        await _cacheRepository.Save(cacheKey, cloudSessionData);

        // Act
        var result = await _repository.GetSessionPreMember(sessionId, clientInstanceId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(preSessionMember);
    }

    [Test]
    public async Task AddCloudSession_IntegrationTest()
    {
        // Arrange
        var sessionId = "testSession_" + DateTime.Now.Ticks;
        
        var cloudSessionData = new CloudSessionData
        {
            SessionId = "temporary", // This will be overwritten by the generateSessionIdHandler
            SessionMembers = new List<SessionMemberData>(),
            PreSessionMembers = new List<SessionMemberData>()
        };

        Func<string> generateSessionIdHandler = () => sessionId;
        var transaction = _redisInfrastructureService.OpenTransaction();

        // Act
        var result = await _repository.AddCloudSession(cloudSessionData, generateSessionIdHandler, transaction);
        await transaction.ExecuteAsync();

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be(sessionId);
        
        var cacheKey = _redisInfrastructureService.ComputeCacheKey(EntityType.Session, sessionId);
        var value = await _cacheRepository.Get(cacheKey);
        value.Should().NotBeNull();
        value.Should().BeOfType<CloudSessionData>();
        value.SessionId.Should().Be(sessionId);
    }
}
