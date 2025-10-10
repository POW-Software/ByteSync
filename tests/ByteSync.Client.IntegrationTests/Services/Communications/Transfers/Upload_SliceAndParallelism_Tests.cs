using Autofac;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.DependencyInjection;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.Http;
using ByteSync.Interfaces.Factories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Services;
using FluentAssertions;

namespace ByteSync.Client.IntegrationTests.Services.Communications.Transfers;

public class Upload_SliceAndParallelism_Tests
{
    private ILifetimeScope _clientScope = null!;
    
    // Helper to inject a fixed adaptive controller for deterministic tests
    private ILifetimeScope BeginAdaptiveScope(int chunkSizeBytes, int parallelism)
    {
        return _clientScope.BeginLifetimeScope(b =>
        {
            b.RegisterInstance<IAdaptiveUploadController>(new FixedAdaptiveUploadController(chunkSizeBytes, parallelism))
                .SingleInstance();
        });
    }
    
    // Test doubles moved to TestHelpers/UploadTestDoubles.cs
    
    [SetUp]
    public void SetUp()
    {
        // Ensure enough thread-pool threads for stable parallel worker scheduling in tests
        ThreadPool.GetMinThreads(out var worker, out var io);
        if (worker < 8 || io < 8)
        {
            ThreadPool.SetMinThreads(Math.Max(worker, 8), Math.Max(io, 8));
        }
        
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (ContainerProvider.Container == null)
        {
            ServiceRegistrar.RegisterComponents();
        }
        
        _clientScope = ContainerProvider.Container!.BeginLifetimeScope(b =>
        {
            // Override HTTP client with local test client and upload strategy
            b.RegisterType<TestFileTransferApiClient>().As<IFileTransferApiClient>().SingleInstance();
            b.RegisterType<TestUploadStrategy>().Keyed<IUploadStrategy>(StorageProvider.AzureBlobStorage).SingleInstance();
        });
        
        // Set AES key for encryption used by SlicerEncrypter
        using var scope = _clientScope.BeginLifetimeScope();
        var cloudSessionConnectionRepository = scope.Resolve<ICloudSessionConnectionRepository>();
        cloudSessionConnectionRepository.SetAesEncryptionKey(AesGenerator.GenerateKey());
        
        // Clear previous records and concurrency counters
        TestUploadStrategy.Reset();
    }
    
    [TearDown]
    public void TearDown()
    {
        _clientScope.Dispose();
    }
    
    private static SharedFileDefinition BuildShared(string? id = null)
    {
        return new SharedFileDefinition
        {
            Id = id ?? Guid.NewGuid().ToString("N"),
            SessionId = "tests-" + Guid.NewGuid().ToString("N"),
            ClientInstanceId = Guid.NewGuid().ToString("N"),
            SharedFileType = SharedFileTypes.FullSynchronization,
            IV = AesGenerator.GenerateIV()
        };
    }
    
    [Test]
    public async Task Upload_OneSmallFile_FixedSliceLen_FixedParallel_ShouldHaveOneTaskAndFullLength()
    {
        await using var scope = BeginAdaptiveScope(1024, 2);
        var uploaderFactory = scope.Resolve<IFileUploaderFactory>();
        
        var shared = BuildShared("small1");
        
        var tempFile = Path.GetTempFileName();
        var inputContent = new string('a', 512); // smaller than slice
        await File.WriteAllTextAsync(tempFile, inputContent);
        
        var uploader = uploaderFactory.Build(tempFile, shared);
        
        await uploader.Upload();
        
        TestUploadStrategy.Records.ContainsKey(shared.Id).Should().BeTrue();
        var recs = TestUploadStrategy.Records[shared.Id];
        recs.Count.Should().Be(1); // only one slice
        TestUploadStrategy.MaxInFlight.Should().Be(1); // one worker involved
        shared.UploadedFileLength.Should().Be(inputContent.Length); // full length transferred
    }
    
    [Test]
    public async Task Upload_OneBigFile_MoreThanThreeSlices_FixedParallel2_ShouldHaveTwoTasksAndFullLength()
    {
        await using var scope = BeginAdaptiveScope(1024 * 1024, 2);
        var uploaderFactory = scope.Resolve<IFileUploaderFactory>();
        
        var shared = BuildShared("big1");
        
        var tempFile = Path.GetTempFileName();
        var sliceLength = 1 * 1024 * 1024; // fixed slice length
        var inputContent = new string('b', sliceLength * 3 + 1); // > 3x slice length => > 3 slices
        await File.WriteAllTextAsync(tempFile, inputContent);
        
        var uploader = uploaderFactory.Build(tempFile, shared);
        
        await uploader.Upload();
        
        var recs = TestUploadStrategy.Records[shared.Id];
        recs.Count.Should().BeGreaterThanOrEqualTo(4);
        TestUploadStrategy.MaxInFlight.Should().Be(2);
        shared.UploadedFileLength.Should().Be(inputContent.Length);
    }
    
    
    [Test]
    public async Task Upload_TwoSmallFiles_FixedSliceLen_FixedParallel_ShouldHaveTwoTasksAndFullLength()
    {
        await using var scope = BeginAdaptiveScope(1024, 2);
        var uploaderFactory = scope.Resolve<IFileUploaderFactory>();
        
        var shared1 = BuildShared("s1");
        var shared2 = BuildShared("s2");
        
        var tempFile1 = Path.GetTempFileName();
        var tempFile2 = Path.GetTempFileName();
        var c1 = new string('x', 400);
        var c2 = new string('y', 700);
        await File.WriteAllTextAsync(tempFile1, c1);
        await File.WriteAllTextAsync(tempFile2, c2);
        
        var uploader1 = uploaderFactory.Build(tempFile1, shared1);
        var uploader2 = uploaderFactory.Build(tempFile2, shared2);
        
        // Run both uploads concurrently to involve two workers
        await Task.WhenAll(uploader1.Upload(), uploader2.Upload());
        
        TestUploadStrategy.MaxInFlight.Should().Be(2);
        TestUploadStrategy.Records[shared1.Id].Count.Should().Be(1);
        TestUploadStrategy.Records[shared2.Id].Count.Should().Be(1);
        shared1.UploadedFileLength.Should().Be(c1.Length);
        shared2.UploadedFileLength.Should().Be(c2.Length);
    }
    
    [Test]
    public async Task Upload_TwoBigFiles_MoreThanThreeSlicesEach_FixedParallel3_ShouldHaveThreeTasksAndFullLength()
    {
        await using var scope = BeginAdaptiveScope(1024 * 1024, 3);
        var uploaderFactory = scope.Resolve<IFileUploaderFactory>();
        
        var shared1 = BuildShared("b1");
        var shared2 = BuildShared("b2");
        
        var sliceLength = 1 * 1024 * 1024; // fixed slice length
        var c1 = new string('m', sliceLength * 3 + 3); // > 3x slice length => > 3 slices
        var c2 = new string('n', sliceLength * 3 + 7); // > 3x slice length => > 3 slices
        
        var tempFile1 = Path.GetTempFileName();
        var tempFile2 = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile1, c1);
        await File.WriteAllTextAsync(tempFile2, c2);
        
        var uploader1 = uploaderFactory.Build(tempFile1, shared1);
        var uploader2 = uploaderFactory.Build(tempFile2, shared2);
        
        await uploader1.Upload();
        await uploader2.Upload();
        
        TestUploadStrategy.Records[shared1.Id].Count.Should().BeGreaterThanOrEqualTo(4);
        TestUploadStrategy.Records[shared2.Id].Count.Should().BeGreaterThanOrEqualTo(4);
        TestUploadStrategy.MaxInFlight.Should().Be(3);
        shared1.UploadedFileLength.Should().Be(c1.Length);
        shared2.UploadedFileLength.Should().Be(c2.Length);
    }
}