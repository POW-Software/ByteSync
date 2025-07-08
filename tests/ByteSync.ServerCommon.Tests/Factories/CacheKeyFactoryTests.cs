using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Factories;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Tests.Factories;

public class CacheKeyFactoryTests
{
    private CacheKeyFactory _cacheKeyFactory;
    private IOptions<RedisSettings> _redisSettings;
    private const string TestPrefix = "test-prefix";
    
    [SetUp]
    public void Setup()
    {
        _redisSettings = A.Fake<IOptions<RedisSettings>>();
        A.CallTo(() => _redisSettings.Value).Returns(new RedisSettings { Prefix = TestPrefix });
        
        _cacheKeyFactory = new CacheKeyFactory(_redisSettings);
    }
    
    [Test]
    [TestCase(EntityType.Session, "session123", "Session")]
    [TestCase(EntityType.Inventory, "inv456", "Inventory")]
    [TestCase(EntityType.Synchronization, "sync789", "Synchronization")]
    [TestCase(EntityType.SharedFile, "shared123", "SharedFile")]
    [TestCase(EntityType.SessionSharedFiles, "sessionFiles123", "SessionSharedFiles")]
    [TestCase(EntityType.TrackingAction, "action123", "TrackingAction")]
    [TestCase(EntityType.Client, "client123", "Client")]
    [TestCase(EntityType.ClientSoftwareVersionSettings, "version123", "ClientSoftwareVersionSettings")]
    [TestCase(EntityType.CloudSessionProfile, "profile123", "CloudSessionProfile")]
    [TestCase(EntityType.Lobby, "lobby123", "Lobby")]
    [TestCase(EntityType.Announcement, "msg123", "Announcement")]
    public void Create_ShouldGenerateCacheKey_WithCorrectFormat(EntityType entityType, string entityId, string expectedEntityTypeName)
    {
        // Arrange
        var expectedCacheKeyValue = $"{TestPrefix}:{expectedEntityTypeName}:{entityId}";
        
        // Act
        var result = _cacheKeyFactory.Create(entityType, entityId);
        
        // Assert
        result.Should().NotBeNull();
        result.EntityType.Should().Be(entityType);
        result.EntityId.Should().Be(entityId);
        result.Value.Should().Be(expectedCacheKeyValue);
    }
    
    [Test]
    public void Create_WithDifferentPrefix_ShouldUseConfiguredPrefix()
    {
        // Arrange
        const string customPrefix = "custom-prefix";
        var customRedisSettings = A.Fake<IOptions<RedisSettings>>();
        A.CallTo(() => customRedisSettings.Value).Returns(new RedisSettings { Prefix = customPrefix });
        
        var factory = new CacheKeyFactory(customRedisSettings);
        const string entityId = "123";
        const EntityType entityType = EntityType.Session;
        
        // Act
        var result = factory.Create(entityType, entityId);
        
        // Assert
        result.Value.Should().StartWith(customPrefix);
        result.Value.Should().Be($"{customPrefix}:Session:{entityId}");
    }
    
    [Test]
    public void Create_WithInvalidEntityType_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        const string entityId = "123";
        const EntityType invalidEntityType = (EntityType)999;
        
        // Act & Assert
        FluentActions.Invoking(() => _cacheKeyFactory.Create(invalidEntityType, entityId))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("entityType");
    }
}
