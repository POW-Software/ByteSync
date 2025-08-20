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

namespace ByteSync.Client.IntegrationTests.Services;

public class R2UploadDownload_Tests
{
    private IContainer _clientContainer = null!;
    private ILifetimeScope _clientScope = null!;

    [SetUp]
    public void SetUp()
    {
        _clientContainer = ServiceRegistrar.RegisterComponents();

        _clientScope = _clientContainer.BeginLifetimeScope(b =>
        {
            b.RegisterType<CloudflareR2ClientFactory>().As<ICloudflareR2ClientFactory>().SingleInstance();
            b.RegisterType<CloudflareR2Service>().As<ICloudflareR2Service>().SingleInstance();
            b.Register(ctx => GlobalTestSetup.Container.Resolve<Microsoft.Extensions.Options.IOptions<ByteSync.ServerCommon.Business.Settings.CloudflareR2Settings>>())
                .As<Microsoft.Extensions.Options.IOptions<ByteSync.ServerCommon.Business.Settings.CloudflareR2Settings>>();
            b.RegisterType<R2FileTransferApiClient>().As<IFileTransferApiClient>().SingleInstance();
        });
    }

    [TearDown]
    public void TearDown()
    {
        _clientScope?.Dispose();
        _clientContainer?.Dispose();
    }

    [Test]
    [Explicit]
    [Category("Cloud")]
    public async Task Upload_Then_Download_Should_Succeed_With_Small_Chunks()
    {
        using var scope = _clientScope;
        var uploaderFactory = scope.Resolve<IFileUploaderFactory>();
        var downloaderFactory = scope.Resolve<IFileDownloaderFactory>();
        var r2Service = scope.Resolve<ICloudflareR2Service>();

        var shared = new SharedFileDefinition
        {
            SessionId = "itests-" + Guid.NewGuid().ToString("N"),
            ClientInstanceId = Guid.NewGuid().ToString("N"),
            SharedFileType = SharedFileTypes.FullSynchronization,
            IV = AesGenerator.GenerateIV()
        };

        var inputContent = new string('x', 1_500_000);
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, inputContent);

        var uploader = uploaderFactory.Build(tempFile, shared);
        (uploader as ByteSync.Services.Communications.Transfers.FileUploader)!.MaxSliceLength = 1 * 1024 * 1024;
        await uploader.Upload();

        var downloader = downloaderFactory.Build(shared);
        await downloader.WaitForFileFullyExtracted();
        (downloader as ByteSync.Services.Communications.Transfers.FileDownloader)?.CleanupResources();

        // Cleanup uploaded objects for this test
        var prefix = shared.SessionId + "_" + shared.ClientInstanceId + "_";
        var all = await r2Service.GetAllObjects(CancellationToken.None);
        foreach (var kvp in all.Where(o => o.Key.StartsWith(prefix)))
        {
            await r2Service.DeleteObjectByKey(kvp.Key, CancellationToken.None);
        }
    }
}


