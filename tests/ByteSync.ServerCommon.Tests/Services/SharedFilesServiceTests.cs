using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services.Storage;
using ByteSync.ServerCommon.Services;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Tests.Services;

[TestFixture]
public class SharedFilesServiceTests
{
    private ISharedFilesRepository _sharedFilesRepository;
    private IAzureBlobStorageService _azureBlobStorageService;
    private ICloudflareR2Service _cloudflareR2Service;
    private ILogger<SharedFilesService> _logger;

    [SetUp]
    public void SetUp()
    {
        _sharedFilesRepository = A.Fake<ISharedFilesRepository>();
        _azureBlobStorageService = A.Fake<IAzureBlobStorageService>();
        _cloudflareR2Service = A.Fake<ICloudflareR2Service>();
        _logger = A.Fake<ILogger<SharedFilesService>>();
    }

    [Test]
    [TestCase(StorageProvider.AzureBlobStorage)]
    [TestCase(StorageProvider.CloudflareR2)]
    public async Task AssertFilePartIsDownloaded_WhenPartBecomesFullyDownloaded_DeletesObject_And_Unregisters_WhenRetentionDisabled(StorageProvider storageProvider)
    {
        // Arrange
        var appSettings = Options.Create(new AppSettings { RetainFilesAfterTransfer = false });
        var service = new SharedFilesService(_sharedFilesRepository, _azureBlobStorageService, _cloudflareR2Service, _logger, appSettings);

        var partNumber = 1;
        var recipients = new List<string> { "A", "B" };
        var sharedFileDefinition = new SharedFileDefinition { Id = "file-1" };
        var preExisting = new SharedFileData(sharedFileDefinition, recipients, storageProvider)
        {
            TotalParts = 2,
            UploadedPartsNumbers = new HashSet<int> { 1, 2 },
            DownloadedBy = new Dictionary<int, HashSet<string>>
            {
                { 1, new HashSet<string> { "A" } },
                { 2, new HashSet<string> { "A", "B" } }
            }
        };

        // Intercept AddOrUpdate to apply the update handler to our preExisting state
        A.CallTo(() => _sharedFilesRepository.AddOrUpdate(sharedFileDefinition, A<Func<SharedFileData?, SharedFileData>>._))
            .Invokes(call =>
            {
                var handler = call.GetArgument<Func<SharedFileData?, SharedFileData>>(1)!;
                handler(preExisting);
            })
            .Returns(Task.CompletedTask);

        var client = new Client { ClientInstanceId = "B" };
        var parameters = new TransferParameters
        {
            SessionId = "session-1",
            SharedFileDefinition = sharedFileDefinition,
            PartNumber = partNumber,
            StorageProvider = storageProvider
        };

        // Act
        await service.AssertFilePartIsDownloaded(client, parameters);

        // Assert
        if (storageProvider == StorageProvider.AzureBlobStorage)
        {
            A.CallTo(() => _azureBlobStorageService.DeleteObject(sharedFileDefinition, partNumber))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _cloudflareR2Service.DeleteObject(A<SharedFileDefinition>._, A<int>._))
                .MustNotHaveHappened();
        }
        else
        {
            A.CallTo(() => _cloudflareR2Service.DeleteObject(sharedFileDefinition, partNumber))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _azureBlobStorageService.DeleteObject(A<SharedFileDefinition>._, A<int>._))
                .MustNotHaveHappened();
        }

        A.CallTo(() => _sharedFilesRepository.Forget(sharedFileDefinition))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task AssertFilePartIsDownloaded_WhenRetentionEnabled_DoesNotDeleteObject_ButUnregistersWhenFullyDownloaded()
    {
        // Arrange
        var appSettings = Options.Create(new AppSettings { RetainFilesAfterTransfer = true });
        var service = new SharedFilesService(_sharedFilesRepository, _azureBlobStorageService, _cloudflareR2Service, _logger, appSettings);

        var partNumber = 1;
        var recipients = new List<string> { "A", "B" };
        var sharedFileDefinition = new SharedFileDefinition { Id = "file-2" };
        var preExisting = new SharedFileData(sharedFileDefinition, recipients, StorageProvider.AzureBlobStorage)
        {
            TotalParts = 1,
            UploadedPartsNumbers = new HashSet<int> { 1 },
            DownloadedBy = new Dictionary<int, HashSet<string>>
            {
                { 1, new HashSet<string> { "A" } }
            }
        };

        A.CallTo(() => _sharedFilesRepository.AddOrUpdate(sharedFileDefinition, A<Func<SharedFileData?, SharedFileData>>._))
            .Invokes(call =>
            {
                var handler = call.GetArgument<Func<SharedFileData?, SharedFileData>>(1)!;
                handler(preExisting);
            })
            .Returns(Task.CompletedTask);

        var client = new Client { ClientInstanceId = "B" };
        var parameters = new TransferParameters
        {
            SessionId = "session-2",
            SharedFileDefinition = sharedFileDefinition,
            PartNumber = partNumber,
            StorageProvider = StorageProvider.AzureBlobStorage
        };

        // Act
        await service.AssertFilePartIsDownloaded(client, parameters);

        // Assert
        A.CallTo(() => _azureBlobStorageService.DeleteObject(A<SharedFileDefinition>._, A<int>._))
            .MustNotHaveHappened();
        A.CallTo(() => _cloudflareR2Service.DeleteObject(A<SharedFileDefinition>._, A<int>._))
            .MustNotHaveHappened();

        A.CallTo(() => _sharedFilesRepository.Forget(sharedFileDefinition))
            .MustHaveHappenedOnceExactly();

        // Also assert the in-memory state evolved to be fully downloaded for the part
        preExisting.IsPartFullyDownloaded(partNumber).Should().BeTrue();
        preExisting.IsFullyDownloaded.Should().BeTrue();
    }

    [Test]
    public async Task AssertFilePartIsDownloaded_WhenPartNotFullyDownloaded_DoesNotDeleteOrUnregister()
    {
        // Arrange
        var appSettings = Options.Create(new AppSettings { RetainFilesAfterTransfer = false });
        var service = new SharedFilesService(_sharedFilesRepository, _azureBlobStorageService, _cloudflareR2Service, _logger, appSettings);

        var partNumber = 1;
        var recipients = new List<string> { "A", "B" };
        var sharedFileDefinition = new SharedFileDefinition { Id = "file-3" };
        var preExisting = new SharedFileData(sharedFileDefinition, recipients, StorageProvider.AzureBlobStorage)
        {
            TotalParts = 2,
            UploadedPartsNumbers = new HashSet<int> { 1, 2 },
            DownloadedBy = new Dictionary<int, HashSet<string>>()
        };

        A.CallTo(() => _sharedFilesRepository.AddOrUpdate(sharedFileDefinition, A<Func<SharedFileData?, SharedFileData>>._))
            .Invokes(call =>
            {
                var handler = call.GetArgument<Func<SharedFileData?, SharedFileData>>(1)!;
                handler(preExisting);
            })
            .Returns(Task.CompletedTask);

        var client = new Client { ClientInstanceId = "A" };
        var parameters = new TransferParameters
        {
            SessionId = "session-3",
            SharedFileDefinition = sharedFileDefinition,
            PartNumber = partNumber,
            StorageProvider = StorageProvider.AzureBlobStorage
        };

        // Act
        await service.AssertFilePartIsDownloaded(client, parameters);

        // Assert
        A.CallTo(() => _azureBlobStorageService.DeleteObject(A<SharedFileDefinition>._, A<int>._))
            .MustNotHaveHappened();
        A.CallTo(() => _cloudflareR2Service.DeleteObject(A<SharedFileDefinition>._, A<int>._))
            .MustNotHaveHappened();
        A.CallTo(() => _sharedFilesRepository.Forget(A<SharedFileDefinition>._))
            .MustNotHaveHappened();

        preExisting.IsPartFullyDownloaded(partNumber).Should().BeFalse();
        preExisting.IsFullyDownloaded.Should().BeFalse();
    }
}


