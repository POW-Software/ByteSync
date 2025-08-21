using Autofac;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.DependencyInjection;
using ByteSync.Factories;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Services.Storage;
using ByteSync.ServerCommon.Interfaces.Services.Storage.Factories;
using ByteSync.ServerCommon.Services.Storage;
using ByteSync.ServerCommon.Services.Storage.Factories;
using ByteSync.Client.IntegrationTests.TestHelpers.Http;

namespace ByteSync.Client.IntegrationTests.Services;

public class R2DownloadResume_Tests
{
    private ILifetimeScope _clientScope = null!;

    [SetUp]
    public void SetUp()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (ByteSync.Services.ContainerProvider.Container == null)
        {
            ServiceRegistrar.RegisterComponents();
        }

        _clientScope = ByteSync.Services.ContainerProvider.Container!.BeginLifetimeScope(b =>
        {
            // Simulate one GET 500 during download by replacing IHttpClientFactory used by strategies
            b.RegisterInstance(new FlakyCounter(putFailures: 0, getFailures: 1)).As<IFlakyCounter>().SingleInstance();
            b.RegisterType<FlakyHttpClientFactory>().As<IHttpClientFactory>().SingleInstance();

            b.RegisterType<CloudflareR2ClientFactory>().As<ICloudflareR2ClientFactory>().SingleInstance();
            b.RegisterType<CloudflareR2Service>().As<ICloudflareR2Service>().SingleInstance();
            b.Register(_ => GlobalTestSetup.Container.Resolve<Microsoft.Extensions.Options.IOptions<ByteSync.ServerCommon.Business.Settings.CloudflareR2Settings>>())
                .As<Microsoft.Extensions.Options.IOptions<ByteSync.ServerCommon.Business.Settings.CloudflareR2Settings>>();
            b.RegisterType<R2FileTransferApiClient>().As<IFileTransferApiClient>().SingleInstance();
        });

        // Set AES key for encryption/decryption used by SlicerEncrypter
        using var scope = _clientScope.BeginLifetimeScope();
        var cloudSessionConnectionRepository = scope.Resolve<ByteSync.Interfaces.Repositories.ICloudSessionConnectionRepository>();
        cloudSessionConnectionRepository.SetAesEncryptionKey(AesGenerator.GenerateKey());
    }

    [TearDown]
    public void TearDown()
    {
        _clientScope.Dispose();
    }

    [Test]
    [Category("Cloud")]
    public async Task Download_WithTransientFailure_Should_Retry_And_Succeed()
    {
        // ReSharper disable once UseAwaitUsing
        using var scope = _clientScope;
        var uploaderFactory = scope.Resolve<IFileUploaderFactory>();
        var downloaderFactory = scope.Resolve<IFileDownloaderFactory>();
        var r2Service = scope.Resolve<ICloudflareR2Service>();
        var sharedActionsGroupRepository = scope.Resolve<ByteSync.Interfaces.Repositories.ISharedActionsGroupRepository>();
        var sessionService = scope.Resolve<ByteSync.Interfaces.Services.Sessions.ISessionService>();
        var connectionService = scope.Resolve<ByteSync.Interfaces.Services.Communications.IConnectionService>();

        var shared = new SharedFileDefinition
        {
            SessionId = "itests-" + Guid.NewGuid().ToString("N"),
            ClientInstanceId = Guid.NewGuid().ToString("N"),
            SharedFileType = SharedFileTypes.FullSynchronization,
            IV = AesGenerator.GenerateIV()
        };

        // Minimal session/endPoint/context so DownloadTargetBuilder can resolve destinations
        connectionService.CurrentEndPoint = new ByteSync.Common.Business.EndPoints.ByteSyncEndpoint
        {
            ClientInstanceId = shared.ClientInstanceId,
            ClientId = shared.ClientInstanceId,
            Version = "itests",
            IpAddress = "127.0.0.1",
            OSPlatform = Common.Business.Misc.OSPlatforms.Windows
        };
        await sessionService.SetSessionStatus(ByteSync.Business.Sessions.SessionStatus.Preparation);

        var sag = new ByteSync.Business.Actions.Shared.SharedActionsGroup
        {
            ActionsGroupId = Guid.NewGuid().ToString("N"),
            SynchronizationType = Common.Business.Actions.SynchronizationTypes.Full,
            Source = new ByteSync.Business.Actions.Shared.SharedDataPart
            {
                ClientInstanceId = shared.ClientInstanceId,
                RootPath = Path.GetTempPath(),
                InventoryPartType = Common.Business.Inventories.FileSystemTypes.File,
                Name = "itests",
                InventoryCodeAndId = "itests"
            },
            PathIdentity = new ByteSync.Business.Inventories.PathIdentity
            {
                FileSystemType = Common.Business.Inventories.FileSystemTypes.File,
                LinkingKeyValue = "itests"
            }
        };
        sag.Targets.Add(new ByteSync.Business.Actions.Shared.SharedDataPart
        {
            ClientInstanceId = shared.ClientInstanceId,
            RootPath = Path.GetTempFileName(),
            InventoryPartType = Common.Business.Inventories.FileSystemTypes.File,
            Name = "itests",
            InventoryCodeAndId = "itests"
        });
        shared.ActionsGroupIds = [sag.ActionsGroupId];
        sharedActionsGroupRepository.SetSharedActionsGroups([sag]);

        // First upload a file so we can download it
        var inputContent = new string('z', 1_000_000);
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, inputContent);

        var uploader = uploaderFactory.Build(tempFile, shared);
        (uploader as ByteSync.Services.Communications.Transfers.FileUploader)!.MaxSliceLength = 1 * 1024 * 1024;
        await uploader.Upload();

        var downloader = downloaderFactory.Build(shared);
        var expectedKeyPrefix = shared.SessionId + "_" + shared.ClientInstanceId + "_synchronization_" + shared.Id + ".part";
        var objects = await r2Service.GetAllObjects(CancellationToken.None);
        var partsCount = objects.Count(o => o.Key.StartsWith(expectedKeyPrefix));
        await downloader.StartDownload();
        for (int i = 1; i <= partsCount; i++)
        {
            await downloader.PartsCoordinator.AddAvailablePartAsync(i);
        }
        await downloader.PartsCoordinator.SetAllPartsKnownAsync(partsCount);
        await downloader.WaitForFileFullyExtracted();
        (downloader as ByteSync.Services.Communications.Transfers.FileDownloader)?.CleanupResources();

        // Cleanup
        var prefix = shared.SessionId + "_" + shared.ClientInstanceId + "_";
        var all = await r2Service.GetAllObjects(CancellationToken.None);
        foreach (var kvp in all.Where(o => o.Key.StartsWith(prefix)))
        {
            await r2Service.DeleteObjectByKey(kvp.Key, CancellationToken.None);
        }
    }
}


