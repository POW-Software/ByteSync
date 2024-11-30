using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Repositories;
using ByteSync.ServerCommon.Services;
using ByteSync.ServerCommon.Tests.Helpers;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Tests.Repositories;

public class SharedFilesRepositoryTests
{
    public SharedFilesRepositoryTests()
    {

    }
    
    [Test]
    public void Test()
    {
        Assert.Pass();
    }
    
    [Test]
    public async Task AddOrUpdate_IntegrationTest()
    {
        // Arrange
        var redisSettings = TestSettingsInitializer.GetRedisSettings();
        var loggerFactoryMock = A.Fake<ILoggerFactory>();
        var cacheService = new CacheService(Options.Create(redisSettings), loggerFactoryMock);
        var repository = new SharedFilesRepository(cacheService);

        var sharedFileDefinition = new SharedFileDefinition
        {
            SessionId = "testSession_" + DateTime.Now.Ticks,
            Id = "testSharedFile_" + DateTime.Now.Ticks,
        };

        Func<SharedFileData?, SharedFileData> updateHandler = sharedFileData =>
        {
            sharedFileData = new SharedFileData(sharedFileDefinition, new List<string>());

            return sharedFileData;
        };

        // Act
        await repository.AddOrUpdate(sharedFileDefinition, updateHandler);

        // Assert
        // Verify that the data was updated correctly in the repository
    }
}