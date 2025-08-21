using Autofac;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.DependencyInjection;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Services.Storage;
using ByteSync.ServerCommon.Interfaces.Services.Storage.Factories;
using ByteSync.ServerCommon.Services.Storage;
using ByteSync.ServerCommon.Services.Storage.Factories;
using ByteSync.Client.IntegrationTests.TestHelpers.Http;
using ByteSync.Interfaces.Repositories;
using ByteSync.ServerCommon.Business.Settings;

namespace ByteSync.Client.IntegrationTests.Services;

public class R2UploadResume_Tests
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
            // Override IHttpClientFactory used by strategies with a flaky handler to simulate 500 once
            b.RegisterInstance(new FlakyCounter(putFailures: 1, getFailures: 0)).As<IFlakyCounter>().SingleInstance();
            b.RegisterType<FlakyHttpClientFactory>().As<IHttpClientFactory>().SingleInstance();

            b.RegisterType<CloudflareR2ClientFactory>().As<ICloudflareR2ClientFactory>().SingleInstance();
            b.RegisterType<CloudflareR2Service>().As<ICloudflareR2Service>().SingleInstance();
            b.Register(_ => GlobalTestSetup.Container.Resolve<CloudflareR2Settings>())
                .As<Microsoft.Extensions.Options.IOptions<CloudflareR2Settings>>();
            b.RegisterType<R2FileTransferApiClient>().As<IFileTransferApiClient>().SingleInstance();
        });
        
        // Set AES key for encryption/decryption used by SlicerEncrypter
        using var scope = _clientScope.BeginLifetimeScope();
        var cloudSessionConnectionRepository = scope.Resolve<ICloudSessionConnectionRepository>();
        cloudSessionConnectionRepository.SetAesEncryptionKey(AesGenerator.GenerateKey());
    }

    [TearDown]
    public void TearDown()
    {
        _clientScope.Dispose();
    }

    [Test]
    [Category("Cloud")]
    public async Task Upload_WithTransientFailure_Should_Retry_And_Succeed()
    {
        // ReSharper disable once UseAwaitUsing
        using var scope = _clientScope;
        var uploaderFactory = scope.Resolve<IFileUploaderFactory>();
        var r2Service = scope.Resolve<ICloudflareR2Service>();

        var shared = new SharedFileDefinition
        {
            SessionId = "itests-" + Guid.NewGuid().ToString("N"),
            ClientInstanceId = Guid.NewGuid().ToString("N"),
            SharedFileType = SharedFileTypes.FullSynchronization,
            IV = AesGenerator.GenerateIV()
        };

        // Create ~3MB to ensure multiple parts
        var inputContent = new string('y', 3_000_000);
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, inputContent);

        var uploader = uploaderFactory.Build(tempFile, shared);
        (uploader as ByteSync.Services.Communications.Transfers.FileUploader)!.MaxSliceLength = 1 * 1024 * 1024;

        // We will not instrument the HTTP pipeline here; rely on real R2 stability.
        // A proper transient-fault simulation can be added with a DelegatingHandler injected into IHttpClientFactory.
        await uploader.Upload();

        // Cleanup
        var prefix = shared.SessionId + "_" + shared.ClientInstanceId + "_";
        var all = await r2Service.GetAllObjects(CancellationToken.None);
        foreach (var kvp in all.Where(o => o.Key.StartsWith(prefix)))
        {
            await r2Service.DeleteObjectByKey(kvp.Key, CancellationToken.None);
        }
    }
}


