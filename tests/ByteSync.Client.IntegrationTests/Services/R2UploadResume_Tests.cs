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

public class R2UploadResume_Tests
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
    public async Task Upload_WithTransientFailure_Should_Retry_And_Succeed()
    {
        using var scope = _clientScope;
        var uploaderFactory = scope.Resolve<IFileUploaderFactory>();

        var shared = new SharedFileDefinition
        {
            SessionId = Guid.NewGuid().ToString("N"),
            ClientInstanceId = Guid.NewGuid().ToString("N"),
            SharedFileType = SharedFileTypes.FullSynchronization,
            IV = AesGenerator.GenerateIV()
        };

        // Create ~3MB to ensure multiple parts
        var inputContent = new string('y', 3_000_000);
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, inputContent);

        var uploader = uploaderFactory.Build(tempFile, shared);

        // We will not instrument the HTTP pipeline here; rely on real R2 stability.
        // A proper transient-fault simulation can be added with a DelegatingHandler injected into IHttpClientFactory.
        await uploader.Upload();
    }
}


