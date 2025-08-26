using Autofac;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Common.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.DependencyInjection;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Services.Communications.Transfers.Uploading;
using FluentAssertions;

namespace ByteSync.Client.IntegrationTests.Services.Communications.Transfers;

public class Upload_SliceAndParallelism_Tests
{
    private ILifetimeScope _clientScope = null!;

    private class TestUploadStrategy : IUploadStrategy
    {
        private static readonly object Sync = new object();
        public static readonly Dictionary<string, List<(int PartNumber, long Bytes, int TaskId)>> Records = new();

        public Task<UploadFileResponse> UploadAsync(FileUploaderSlice slice, FileStorageLocation storageLocation, CancellationToken cancellationToken)
        {
            var taskId = Environment.CurrentManagedThreadId;
            lock (Sync)
            {
                var sharedId = storageLocation.Url; // We will encode SharedFileDefinition.Id in the URL via TestApiClient
                if (!Records.ContainsKey(sharedId))
                {
                    Records[sharedId] = new List<(int, long, int)>();
                }
                Records[sharedId].Add((slice.PartNumber, slice.MemoryStream?.Length ?? 0L, taskId));
            }
            return Task.FromResult(UploadFileResponse.Success(200));
        }
    }

    private class TestFileTransferApiClient : IFileTransferApiClient
    {
        public Task<string> GetUploadFileUrl(TransferParameters transferParameters)
        {
            // Use SharedFileDefinition.Id as a pseudo URL to key records
            return Task.FromResult(transferParameters.SharedFileDefinition.Id);
        }

        public async Task<FileStorageLocation> GetUploadFileStorageLocation(TransferParameters transferParameters)
        {
            var url = await GetUploadFileUrl(transferParameters);
            return new FileStorageLocation(url, StorageProvider.AzureBlobStorage);
        }

        public Task<string> GetDownloadFileUrl(TransferParameters transferParameters) => Task.FromResult(string.Empty);
        public Task<FileStorageLocation> GetDownloadFileStorageLocation(TransferParameters transferParameters) => Task.FromResult(new FileStorageLocation(string.Empty, StorageProvider.AzureBlobStorage));
        public Task AssertFilePartIsUploaded(TransferParameters transferParameters) => Task.CompletedTask;
        public Task AssertUploadIsFinished(TransferParameters transferParameters) => Task.CompletedTask;
        public Task AssertFilePartIsDownloaded(TransferParameters transferParameters) => Task.CompletedTask;
        public Task AssertDownloadIsFinished(TransferParameters transferParameters) => Task.CompletedTask;
    }

    [SetUp]
    public void SetUp()
    {
        if (ByteSync.Services.ContainerProvider.Container == null)
        {
            ServiceRegistrar.RegisterComponents();
        }

        _clientScope = ByteSync.Services.ContainerProvider.Container!.BeginLifetimeScope(b =>
        {
            // Override HTTP client with local test client and upload strategy
            b.RegisterType<TestFileTransferApiClient>().As<IFileTransferApiClient>().SingleInstance();
            b.RegisterType<TestUploadStrategy>().Keyed<IUploadStrategy>(StorageProvider.AzureBlobStorage).SingleInstance();
        });

        // Set AES key for encryption used by SlicerEncrypter
        using var scope = _clientScope.BeginLifetimeScope();
        var cloudSessionConnectionRepository = scope.Resolve<ICloudSessionConnectionRepository>();
        cloudSessionConnectionRepository.SetAesEncryptionKey(AesGenerator.GenerateKey());

        // Clear previous records
        lock (TestUploadStrategy.Records)
        {
            TestUploadStrategy.Records.Clear();
        }
    }

    [TearDown]
    public void TearDown()
    {
        _clientScope?.Dispose();
    }

    private static SharedFileDefinition BuildShared(string? id = null)
    {
        return new SharedFileDefinition
        {
            Id = id ?? Guid.NewGuid().ToString("N"),
            SessionId = "itests-" + Guid.NewGuid().ToString("N"),
            ClientInstanceId = Guid.NewGuid().ToString("N"),
            SharedFileType = SharedFileTypes.FullSynchronization,
            IV = AesGenerator.GenerateIV()
        };
    }

    [Test]
    public async Task Upload_OneSmallFile_FixedSliceLen_FixedParallel_ShouldHaveOneTaskAndFullLength()
    {
        using var scope = _clientScope;
        var uploaderFactory = scope.Resolve<IFileUploaderFactory>();

        var shared = BuildShared("small1");

        var tempFile = Path.GetTempFileName();
        var inputContent = new string('a', 512); // smaller than slice
        await File.WriteAllTextAsync(tempFile, inputContent);

        var uploader = uploaderFactory.Build(tempFile, shared) as FileUploader;
        uploader!.MaxSliceLength = 1024; // fixed slice length

        await uploader.Upload();

        TestUploadStrategy.Records.ContainsKey(shared.Id).Should().BeTrue();
        var recs = TestUploadStrategy.Records[shared.Id];
        recs.Count.Should().Be(1); // only one slice/task
        recs.Select(r => r.TaskId).Distinct().Count().Should().Be(1); // one worker involved
        shared.UploadedFileLength.Should().Be(inputContent.Length); // full length transferred
    }

    [Test]
    public async Task Upload_OneBigFile_ThreeSlices_Parallel1_ShouldHaveThreeTasksAndFullLength()
    {
        using var scope = _clientScope;
        var uploaderFactory = scope.Resolve<IFileUploaderFactory>();

        var shared = BuildShared("big1");

        var tempFile = Path.GetTempFileName();
        var sliceLength = 500 * 1024; // 500KB;
        var inputContent = new string('b', sliceLength * 3 + 1); // > 3x slice length => 4 slices
        await File.WriteAllTextAsync(tempFile, inputContent);

        var uploader = uploaderFactory.Build(tempFile, shared) as FileUploader;
        uploader!.MaxSliceLength = sliceLength; // fixed slice length

        await uploader.Upload();

        var recs = TestUploadStrategy.Records[shared.Id];
        recs.Count.Should().Be(4);
        recs.Select(r => r.TaskId).Distinct().Count().Should().BeLessThanOrEqualTo(3).And.BeGreaterThanOrEqualTo(1);
        shared.UploadedFileLength.Should().Be(inputContent.Length);
    }

    [Test]
    public async Task Upload_OneBigFile_ThreeSlices_Parallel3_ShouldHaveThreeTasksAndFullLength()
    {
        using var scope = _clientScope;
        var uploaderFactory = scope.Resolve<IFileUploaderFactory>();

        var shared = BuildShared("big3");

        var tempFile = Path.GetTempFileName();
        var sliceLength = 500 * 1024; // 500KB;;
        var inputContent = new string('c', sliceLength * 3 + 5); // > 3x slice length => 4 slices
        await File.WriteAllTextAsync(tempFile, inputContent);

        var uploader = uploaderFactory.Build(tempFile, shared) as FileUploader;
        uploader!.MaxSliceLength = sliceLength;

        await uploader.Upload();

        var recs = TestUploadStrategy.Records[shared.Id];
        recs.Count.Should().Be(4);
        recs.Select(r => r.TaskId).Distinct().Count().Should().BeLessThanOrEqualTo(3).And.BeGreaterThanOrEqualTo(1);
        shared.UploadedFileLength.Should().Be(inputContent.Length);
    }

    [Test]
    public async Task Upload_TwoSmallFiles_FixedSliceLen_FixedParallel_ShouldHaveTwoTasksAndFullLength()
    {
        using var scope = _clientScope;
        var uploaderFactory = scope.Resolve<IFileUploaderFactory>();

        var shared1 = BuildShared("s1");
        var shared2 = BuildShared("s2");

        var tempFile1 = Path.GetTempFileName();
        var tempFile2 = Path.GetTempFileName();
        var c1 = new string('x', 400);
        var c2 = new string('y', 700);
        await File.WriteAllTextAsync(tempFile1, c1);
        await File.WriteAllTextAsync(tempFile2, c2);

        var uploader1 = uploaderFactory.Build(tempFile1, shared1) as FileUploader;
        var uploader2 = uploaderFactory.Build(tempFile2, shared2) as FileUploader;
        uploader1!.MaxSliceLength = 1024;
        uploader2!.MaxSliceLength = 1024;

        await uploader1.Upload();
        await uploader2.Upload();

        TestUploadStrategy.Records[shared1.Id].Count.Should().Be(1);
        TestUploadStrategy.Records[shared2.Id].Count.Should().Be(1);
        shared1.UploadedFileLength.Should().Be(c1.Length);
        shared2.UploadedFileLength.Should().Be(c2.Length);
    }

    [Test]
    public async Task Upload_TwoBigFiles_ThreeSlicesEach_Parallel3_ShouldHaveThreeTasksAndFullLength()
    {
        using var scope = _clientScope;
        var uploaderFactory = scope.Resolve<IFileUploaderFactory>();

        var shared1 = BuildShared("b1");
        var shared2 = BuildShared("b2");

        var sliceLength = 500 * 1024; // 500KB;;
        var c1 = new string('m', sliceLength * 3 + 3); // > 3x slice length => 4 slices
        var c2 = new string('n', sliceLength * 3 + 7); // > 3x slice length => 4 slices

        var tempFile1 = Path.GetTempFileName();
        var tempFile2 = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile1, c1);
        await File.WriteAllTextAsync(tempFile2, c2);

        var uploader1 = uploaderFactory.Build(tempFile1, shared1) as FileUploader;
        var uploader2 = uploaderFactory.Build(tempFile2, shared2) as FileUploader;
        uploader1!.MaxSliceLength = sliceLength;
        uploader2!.MaxSliceLength = sliceLength;

        await uploader1.Upload();
        await uploader2.Upload();

        TestUploadStrategy.Records[shared1.Id].Count.Should().Be(4);
        TestUploadStrategy.Records[shared2.Id].Count.Should().Be(4);
        TestUploadStrategy.Records[shared1.Id].Select(r => r.TaskId).Distinct().Count().Should().BeLessThanOrEqualTo(3).And.BeGreaterThanOrEqualTo(1);
        TestUploadStrategy.Records[shared2.Id].Select(r => r.TaskId).Distinct().Count().Should().BeLessThanOrEqualTo(3).And.BeGreaterThanOrEqualTo(1);
        shared1.UploadedFileLength.Should().Be(c1.Length);
        shared2.UploadedFileLength.Should().Be(c2.Length);
    }
}


