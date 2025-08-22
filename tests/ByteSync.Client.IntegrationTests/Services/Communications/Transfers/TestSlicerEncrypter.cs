using Autofac;
using ByteSync.Business.Communications;
using ByteSync.Business.Communications.Downloading;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Common.Helpers;
using ByteSync.Factories;
using ByteSync.Interfaces.Controls.Encryptions;
using ByteSync.Interfaces.Repositories;
using ByteSync.Services.Encryptions;
using ByteSync.TestsCommon;
using FluentAssertions;
using ICSharpCode.SharpZipLib.Zip;
using Moq;

#pragma warning disable 168

namespace ByteSync.Client.IntegrationTests.Services.Communications.Transfers;

public class TestSlicerEncrypter : IntegrationTest
{
    Random _random = new Random();

    public TestSlicerEncrypter()
    {
        
    }

    [SetUp]
    public void SetUp()
    {
        RegisterType<MergerDecrypterFactory>();
        RegisterType<SlicerEncrypter>();
        RegisterType<MergerDecrypter, IMergerDecrypter>();
        BuildMoqContainer();

        _testDirectoryService.CreateTestDirectory();
    }
    
    [Theory]
    [TestCase("")]
    [TestCase("simpleContent")]
    public async Task EncryptDecrypt_ShouldWorkProperly_WithASimpleContent(string fileContent)
    {
        var inputFileInfo = _testDirectoryService.CreateFileInDirectory(_testDirectoryService.TestDirectory, "input.txt", fileContent);

        var mockCloudSessionConnectionRepository = Container.Resolve<Mock<ICloudSessionConnectionRepository>>();
        mockCloudSessionConnectionRepository.Setup(m => m.GetAesEncryptionKey()).Returns(AesGenerator.GenerateKey());
        
        var sharedFileDefinition = new SharedFileDefinition();
        sharedFileDefinition.IV = AesGenerator.GenerateIV();
        
        string resultFullName = IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "result.txt");
        var localSharedFile = new LocalSharedFile(sharedFileDefinition, resultFullName);
        
        DownloadTarget downloadTarget = new DownloadTarget(sharedFileDefinition, localSharedFile, [resultFullName]);
        
        using var slicerEncrypter = Container.Resolve<SlicerEncrypter>();
        slicerEncrypter.Initialize(inputFileInfo, sharedFileDefinition);
        
        var mergerDecrypterFactory = Container.Resolve<MergerDecrypterFactory>();
        var mergerDecrypter = mergerDecrypterFactory.Build(resultFullName, downloadTarget, new CancellationTokenSource());

        await SliceAndEncrypt(slicerEncrypter, downloadTarget);
        downloadTarget.MemoryStreams.Count.Should().Be(1);
        
        for (int i = 0; i < downloadTarget.MemoryStreams.Count; i++)
        {
            await mergerDecrypter.MergeAndDecrypt();
        }

        (await File.ReadAllTextAsync(resultFullName)).Should().Be(fileContent);
    }
    
    [Theory]
    [TestCase(64, 0)]
    [TestCase(64, 63)]
    [TestCase(64, 64)]
    [TestCase(64, 65)]
    [TestCase(64, 70)]
    [TestCase(64, 127)]
    [TestCase(64, 128)]
    [TestCase(64, 129)]
    [TestCase(64, 1000)]
    [TestCase(128, 100000)]
    [TestCase(null, 1000000)]
    // [TestCase(null, 80 * 1024 * 1024)]
    public async Task EncryptDecrypt_ShouldWorkProperly_WithDifferentSliceAndContentLengths(int? sliceLength, int contentLength)
    {
        // bool canContinue;
        // MemoryStream memoryStream;
        // int slices;

        string fileContent = TestFileSystemUtils.GenerateRandomTextContent(contentLength);
        var inputFileInfo = _testDirectoryService.CreateFileInDirectory(_testDirectoryService.TestDirectory, "input.txt", fileContent);
        
        var mockCloudSessionConnectionRepository = Container.Resolve<Mock<ICloudSessionConnectionRepository>>();
        mockCloudSessionConnectionRepository.Setup(m => m.GetAesEncryptionKey()).Returns(AesGenerator.GenerateKey());
        
        var sharedFileDefinition = new SharedFileDefinition();
        sharedFileDefinition.IV = AesGenerator.GenerateIV();
        
        string resultFullName = IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "result.txt");
        var localSharedFile = new LocalSharedFile(sharedFileDefinition, resultFullName);
        
        DownloadTarget downloadTarget = new DownloadTarget(sharedFileDefinition, localSharedFile, [resultFullName]);
        
        using var slicerEncrypter = Container.Resolve<SlicerEncrypter>();
        slicerEncrypter.Initialize(inputFileInfo, sharedFileDefinition);
        
        var mergerDecrypterFactory = Container.Resolve<MergerDecrypterFactory>();
        var mergerDecrypter = mergerDecrypterFactory.Build(resultFullName, downloadTarget, new CancellationTokenSource());
        
        if (sliceLength != null)
        {
            slicerEncrypter.MaxSliceLength = sliceLength.Value;
        }
        await SliceAndEncrypt(slicerEncrypter, downloadTarget);
        downloadTarget.MemoryStreams.Count.Should().BeGreaterThan(0);
        
        for (int i = 0; i < downloadTarget.MemoryStreams.Count; i++)
        {
            await mergerDecrypter.MergeAndDecrypt();
        }

        (await File.ReadAllTextAsync(resultFullName)).Should().Be(fileContent);
    }

   
    [Theory]
    [TestCase(null)]
    [TestCase(1024)]
    public async Task EncryptDecrypt_ShouldWorkProperly_WithBinaryFile(int? sliceLength)
    {
        string fileContent = TestFileSystemUtils.GenerateRandomTextContent(100000);
        FileInfo fileInfo = _testDirectoryService.CreateFileInDirectory(_testDirectoryService.TestDirectory, "input.txt", fileContent);

        string zipFile = IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "input.zip");
        using (var zip = ZipFile.Create(zipFile))
        {
            zip.BeginUpdate();
            zip.Add(fileInfo.FullName);
            zip.CommitUpdate();
        }
        
        var inputFileInfo = new FileInfo(zipFile);

        var mockCloudSessionConnectionRepository = Container.Resolve<Mock<ICloudSessionConnectionRepository>>();
        mockCloudSessionConnectionRepository.Setup(m => m.GetAesEncryptionKey()).Returns(AesGenerator.GenerateKey());
        
        var sharedFileDefinition = new SharedFileDefinition();
        sharedFileDefinition.IV = AesGenerator.GenerateIV();
        
        string resultFullName = IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "result.zip");
        var localSharedFile = new LocalSharedFile(sharedFileDefinition, resultFullName);
        
        DownloadTarget downloadTarget = new DownloadTarget(sharedFileDefinition, localSharedFile, [resultFullName]);
        
        using var slicerEncrypter = Container.Resolve<SlicerEncrypter>();
        slicerEncrypter.Initialize(inputFileInfo, sharedFileDefinition);
        
        var mergerDecrypterFactory = Container.Resolve<MergerDecrypterFactory>();
        var mergerDecrypter = mergerDecrypterFactory.Build(resultFullName, downloadTarget, new CancellationTokenSource());

        if (sliceLength != null)
        {
            slicerEncrypter.MaxSliceLength = sliceLength.Value;
        }
        await SliceAndEncrypt(slicerEncrypter, downloadTarget);
        downloadTarget.MemoryStreams.Count.Should().BeGreaterThan(0);
        
        for (int i = 0; i < downloadTarget.MemoryStreams.Count; i++)
        {
            await mergerDecrypter.MergeAndDecrypt();
        }
        
        var unzipDir = _testDirectoryService.TestDirectory.CreateSubdirectory("unzip");
        FastZip fastZip = new FastZip();
        fastZip.ExtractZip(resultFullName, unzipDir.FullName, null);
        
        var unzippedFile = unzipDir.GetFiles("*", SearchOption.AllDirectories).Single();

        (await File.ReadAllTextAsync(unzippedFile.FullName)).Should().Be(fileContent);
    }
   
   /*

  [Test]
  [Explicit]
  public void EncryptDecrypt_LongPaths()
  {
      ClassicAssert.Fail("Non implémenté");
  }

  [Test]
  public void EncryptDecrypt_Different_Key()
  {
      CreateTestDirectory();

      FileInfo fileInfo = CreateFileInDirectory(TestDirectory, "myFirstTest.txt", "myFirstContent");

      MockObjectsGenerator generator = new MockObjectsGenerator(this);
      generator.GenerateCloudSessionManager();
      var sharedFileDefinition = generator.GenerateSharedFileDefinition();

      //var cloudSessionManager = CloudSessionManagerGenerator.Generate(TestDirectory);
      //var sharedFileDefinition = CloudSessionManagerGenerator.GenerateSharedFileDefinition(cloudSessionManager);
      SlicerEncrypter slicerEncrypter = BuildSlicerEncrypter(generator, fileInfo.FullName, sharedFileDefinition);
      var memoryStreams = SliceAndEncrypt(slicerEncrypter);

      string resultFullName = IOUtils.Combine(TestDirectory.FullName, "result.txt");
      generator.SetAesEncryptionKey(AesGenerator.GenerateKey());

      MergerDecrypter mergerDecrypter = BuildMergerDecrypter(generator, resultFullName, sharedFileDefinition, memoryStreams);
      var ex = ClassicAssert.ThrowsAsync<CryptographicException>(() => mergerDecrypter.MergeAndDecrypt());
  }

  [Test]
  public void EncryptDecrypt_Different_IV()
  {
      CreateTestDirectory();
      FileInfo fileInfo = CreateFileInDirectory(TestDirectory, "myFirstTest.txt", "myFirstContent");

      //byte[] key1;
      //byte[] iv1;
      //GenerateKeyAndIV(out key1, out iv1);

      MockObjectsGenerator mockObjectsGenerator = new MockObjectsGenerator(this);
      mockObjectsGenerator.GenerateCloudSessionManager();


      //var managerMock = CloudSessionManagerGenerator.GenerateCloudSessionManager();

      //var myCloudSessionManager = CloudSessionManagerGenerator.Generate(TestDirectory);
      var sharedFileDefinition = mockObjectsGenerator.GenerateSharedFileDefinition(); // CloudSessionManagerGenerator.GenerateSharedFileDefinition(myCloudSessionManager);
      SlicerEncrypter slicerEncrypter = BuildSlicerEncrypter(mockObjectsGenerator, fileInfo.FullName, sharedFileDefinition);
      var memoryStreams = SliceAndEncrypt(slicerEncrypter);

      string resultFullName = IOUtils.Combine(TestDirectory.FullName, "result.txt");

      //byte[] key2;
      //byte[] iv2;
      //GenerateKeyAndIV(out key2, out iv2);
      sharedFileDefinition.IV = AesGenerator.GenerateIV();
      MergerDecrypter mergerDecrypter = BuildMergerDecrypter(mockObjectsGenerator, resultFullName, sharedFileDefinition, memoryStreams);
      var ex = ClassicAssert.ThrowsAsync<CryptographicException>(() => mergerDecrypter.MergeAndDecrypt());

      //ClassicAssert.AreEqual("myFirstContent", File.ReadAllText(resultFullName));
  }

   */
    
    private async Task SliceAndEncrypt(SlicerEncrypter slicerEncrypter, DownloadTarget downloadTarget)
    {
        List<FileUploaderSlice> fileUploaderSlices = new List<FileUploaderSlice>();
        
        var newFileUploaderSlice = await slicerEncrypter.SliceAndEncrypt();

        if (newFileUploaderSlice != null)
        {
            fileUploaderSlices.Add(newFileUploaderSlice);
        }
        
        while (newFileUploaderSlice != null)
        {
            newFileUploaderSlice = await slicerEncrypter.SliceAndEncrypt();
            
            if (newFileUploaderSlice != null)
            {
                fileUploaderSlices.Add(newFileUploaderSlice);
            }
        }
        
        int cpt = 1;
        foreach (var fileUploaderSlice in fileUploaderSlices)
        {
            downloadTarget.MemoryStreams.Add(cpt, fileUploaderSlice.MemoryStream);
            cpt += 1;
        }
    }
}