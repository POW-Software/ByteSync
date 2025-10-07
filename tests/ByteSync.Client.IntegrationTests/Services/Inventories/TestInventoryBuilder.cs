using Autofac;
using ByteSync.Business;
using ByteSync.Business.DataNodes;
using ByteSync.Business.Inventories;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Client.IntegrationTests.TestHelpers.Business;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Helpers;
using ByteSync.Factories;
using ByteSync.Interfaces;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.FileSystems;
using ByteSync.Models.Inventories;
using ByteSync.Services.Inventories;
using ByteSync.Services.Sessions;
using ByteSync.TestsCommon;
using FluentAssertions;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging;
using Moq;

#pragma warning disable 168

namespace ByteSync.Client.IntegrationTests.Services.Inventories;

public class TestInventoryBuilder : IntegrationTest
{
    [SetUp]
    public void SetUp()
    {
        // RegisterType<DeltaManager, IDeltaManager>();
        RegisterType<CloudSessionLocalDataManager, ICloudSessionLocalDataManager>();
        
        // RegisterType<TemporaryFileManagerFactory, ITemporaryFileManagerFactory>();
        // RegisterType<TemporaryFileManager, ITemporaryFileManager>();
        RegisterType<ComparisonResultPreparer>();
        
        // RegisterType<SynchronizationActionHandler>();
        BuildMoqContainer();
        
        var contextHelper = new TestContextGenerator(Container);
        contextHelper.GenerateSession();
        contextHelper.GenerateCurrentEndpoint();
        var testDirectory = _testDirectoryService.CreateTestDirectory();
        
        var mockEnvironmentService = Container.Resolve<Mock<IEnvironmentService>>();
        mockEnvironmentService.Setup(m => m.AssemblyFullName).Returns(IOUtils.Combine(testDirectory.FullName, "Assembly", "Assembly.exe"));
        
        var mockLocalApplicationDataManager = Container.Resolve<Mock<ILocalApplicationDataManager>>();
        mockLocalApplicationDataManager.Setup(m => m.ApplicationDataPath).Returns(IOUtils.Combine(testDirectory.FullName,
            "ApplicationDataPath"));
        
        
        // var contextHelper = new TestContextGenerator(Container);
        // contextHelper.GenerateSession();
        // _currentEndPoint = contextHelper.GenerateCurrentEndpoint();
        //
        //
        // var mockEnvironmentService = Container.Resolve<Mock<IEnvironmentService>>();
        // mockEnvironmentService.Setup(m => m.AssemblyFullName).Returns(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "Assembly", "Assembly.exe"));
        //
        // var mockLocalApplicationDataManager = Container.Resolve<Mock<ILocalApplicationDataManager>>();
        // mockLocalApplicationDataManager.Setup(m => m.ApplicationDataPath).Returns(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, 
        //     "ApplicationDataPath"));
        //
        // _synchronizationActionHandler = Container.Resolve<SynchronizationActionHandler>();
    }
    
    [Test]
    public async Task Test_Empty()
    {
        InventoryBuilder inventoryBuilder;
        Inventory inventory;
        
        DirectoryInfo rootA;
        
        rootA = new DirectoryInfo(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "rootA"));
        rootA.Create();
        
        inventoryBuilder = BuildInventoryBuilder();
        
        inventory = inventoryBuilder.Inventory;
        
        inventoryBuilder.AddInventoryPart(rootA.FullName);
        await inventoryBuilder.BuildBaseInventoryAsync(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName,
            Guid.NewGuid().ToString() + ".inv"));
        inventory = inventoryBuilder.Inventory;
        
        inventory.InventoryParts.Count.Should().Be(1);
        inventory.InventoryParts.Count.Should().Be(1);
        inventory.InventoryParts[0].RootPath.Should().Be(rootA.FullName);
        inventory.InventoryParts[0].InventoryPartType.Should().Be(FileSystemTypes.Directory);
    }
    
    [Test]
    public async Task Test_Empty_2()
    {
        InventoryBuilder inventoryBuilder;
        Inventory inventory;
        
        DirectoryInfo rootA;
        
        rootA = new DirectoryInfo(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "rootA"));
        
        //rootA.Create();
        
        inventoryBuilder = BuildInventoryBuilder();
        
        inventory = inventoryBuilder.Inventory;
        
        // ClassicAssert.AreEqual(null, inventory);
        
        bool isException;
        try
        {
            // todo Here, before, it used to crash because the directory doesn't exist and therefore isn't found
            
            inventoryBuilder.AddInventoryPart(rootA.FullName);
            await inventoryBuilder.BuildBaseInventoryAsync(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName,
                Guid.NewGuid().ToString() + ".inv"));
            isException = false;
        }
        catch
        {
            isException = true;
        }
        
        isException.Should().BeTrue();
    }
    
    [Test]
    public async Task Test_3()
    {
        InventoryBuilder inventoryBuilder;
        Inventory inventory;
        
        DirectoryInfo rootA, rootAa, rootAb, unzipDir;
        
        FileDescription fileDescription;
        
        rootA = new DirectoryInfo(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "rootA"));
        rootA.Create();
        
        _testDirectoryService.CreateFileInDirectory(rootA.FullName, "fileA1.txt", "FileA1Content");
        _testDirectoryService.CreateFileInDirectory(rootA.FullName, "fileA2.txt", "FileA2Content");
        
        rootAa = rootA.CreateSubdirectory("rootAa");
        _testDirectoryService.CreateFileInDirectory(rootAa.FullName, "fileAa1.txt", "FileAa1Content_special");
        
        rootAb = rootA.CreateSubdirectory("rootAb");
        _testDirectoryService.CreateFileInDirectory(rootAb.FullName, "fileAb1.txt", "FileAb1Content");
        
        //rootA.Create();
        
        var inventoryFilePath = IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, $"inventory_{Guid.NewGuid()}.zip");
        inventoryBuilder = BuildInventoryBuilder();
        
        inventory = inventoryBuilder.Inventory;
        
        inventoryBuilder.AddInventoryPart(rootA.FullName);
        await inventoryBuilder.BuildBaseInventoryAsync(inventoryFilePath);
        
        File.Exists(inventoryFilePath).Should().BeTrue();
        
        unzipDir = new DirectoryInfo(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "unzip"));
        unzipDir.Create();
        
        var fastZip = new FastZip();
        fastZip.ExtractZip(inventoryFilePath, unzipDir.FullName, null);
        
        unzipDir.GetFiles("*", SearchOption.AllDirectories).Length.Should().Be(1);
        File.Exists(IOUtils.Combine(unzipDir.FullName, $"inventory.json")).Should().BeTrue();
        
        inventory = inventoryBuilder.Inventory;
        inventory.InventoryParts.Count.Should().Be(1);
        inventory.InventoryParts[0].DirectoryDescriptions.Count.Should().Be(2);
        inventory.InventoryParts[0].FileDescriptions.Count.Should().Be(4);
        
        fileDescription = inventory.InventoryParts[0].FileDescriptions.First(fd => fd.Name.Equals("fileAa1.txt"));
        fileDescription.RelativePath.Should().Be(@"/rootAa/fileAa1.txt");
        fileDescription.Size.Should().Be("FileAa1Content_special".Length);
        (DateTime.UtcNow - fileDescription.LastWriteTimeUtc < TimeSpan.FromSeconds(5)).Should().BeTrue(); // todo CreationTimeUtc
        
        //bool isException;
        //try
        //{
        //    inventoryBuilder.AddInventoryPart(rootA.FullName);
        //    inventoryBuilder.Build();
        //    isException = false;
        //}
        //catch
        //{
        //    isException = true;
        //}
        
        //isException.Should().BeTrue();
    }
    
    [Test]
    public async Task Test_SourceIsFile()
    {
        InventoryBuilder inventoryBuilder;
        Inventory inventory;
        
        DirectoryInfo sourceA, sourceB, dir1, rootAb, unzipDir;
        FileInfo fileInfo;
        
        sourceB = new DirectoryInfo(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "sourceB"));
        sourceB.Create();
        dir1 = sourceB.CreateSubdirectory("Dir1");
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(dir1.FullName, "file1.txt", "file1Content").FullName);
        fileInfo.LastWriteTimeUtc = new DateTime(2021, 1, 2);
        
        // Source: fileInfo file2.txt file
        var inventoryBFilePath = IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, $"inventoryB.zip");
        inventoryBuilder = BuildInventoryBuilder();
        inventoryBuilder.AddInventoryPart(fileInfo.FullName);
        await inventoryBuilder.BuildBaseInventoryAsync(inventoryBFilePath);
        
        File.Exists(inventoryBFilePath).Should().BeTrue();
        
        unzipDir = new DirectoryInfo(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "unzip"));
        unzipDir.Create();
        
        var fastZip = new FastZip();
        fastZip.ExtractZip(inventoryBFilePath, unzipDir.FullName, null);
        
        unzipDir.GetFiles("*", SearchOption.AllDirectories).Length.Should().Be(1);
        File.Exists(IOUtils.Combine(unzipDir.FullName, $"inventory.json")).Should().BeTrue();
        
        inventory = inventoryBuilder.Inventory;
        inventory.InventoryParts.Count.Should().Be(1);
        inventory.InventoryParts[0].DirectoryDescriptions.Count.Should().Be(0);
        inventory.InventoryParts[0].FileDescriptions.Count.Should().Be(1);
        inventory.InventoryParts[0].FileDescriptions[0].RelativePath.Should().Be("/file1.txt");
        inventory.InventoryParts[0].FileDescriptions[0].Name.Should().Be("file1.txt");
        inventory.InventoryParts[0].FileDescriptions[0].Size.Should().Be("file1Content".Length);
        inventory.InventoryParts[0].FileDescriptions[0].LastWriteTimeUtc.Should().Be(new DateTime(2021, 1, 2));
    }
    
    [Test]
    public async Task Test_Cancel()
    {
        InventoryBuilder inventoryBuilder;
        Inventory inventory;
        
        DirectoryInfo sourceA, dir1, rootAb, unzipDir;
        FileInfo fileInfo;
        
        sourceA = new DirectoryInfo(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "sourceA"));
        sourceA.Create();
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(sourceA.FullName, "fileA.txt", "file1Content").FullName);
        dir1 = sourceA.CreateSubdirectory("Dir1");
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(dir1.FullName, "fileA1.txt", "file1Content").FullName);
        
        
        var inventoryAFilePath = IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, $"inventoryA.zip");
        
        var sessionSettings = SessionSettings.BuildDefault();
        
        inventoryBuilder = BuildInventoryBuilder(sessionSettings);
        inventoryBuilder.AddInventoryPart(sourceA.FullName);
        
        await FluentActions.Invoking(() => inventoryBuilder.BuildBaseInventoryAsync(inventoryAFilePath, new CancellationToken(true)))
            .Should().ThrowAsync<TaskCanceledException>();
        
        // try
        // {
        //     await inventoryBuilder.BuildBaseInventoryAsync(inventoryAFilePath, new CancellationToken(true));
        // }
        
        File.Exists(inventoryAFilePath).Should().BeFalse();
        
        await inventoryBuilder.BuildBaseInventoryAsync(inventoryAFilePath, new CancellationToken(false));
        
        unzipDir = new DirectoryInfo(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "unzip"));
        unzipDir.Create();
        
        var fastZip = new FastZip();
        fastZip.ExtractZip(inventoryAFilePath, unzipDir.FullName, null);
        
        unzipDir.GetFiles("*", SearchOption.AllDirectories).Length.Should().Be(1);
        File.Exists(IOUtils.Combine(unzipDir.FullName, $"inventory.json")).Should().BeTrue();
        
        inventory = inventoryBuilder.Inventory!;
        inventory.InventoryParts.Count.Should().Be(1);
        inventory.InventoryParts[0].DirectoryDescriptions.Count.Should().Be(1);
        inventory.InventoryParts[0].FileDescriptions.Count.Should().Be(2);
    }
    
    [Test]
    [Platform(Exclude = "Linux")]
    [TestCase(true, 0)]
    [TestCase(false, 2)]
    public async Task Test_HiddenFiles_Windows(bool excludeHiddenFiles, int expectedHiddenFiles)
    {
        InventoryBuilder inventoryBuilder;
        Inventory inventory;
        
        DirectoryInfo sourceA, dir1, rootAb, unzipDir;
        FileInfo fileInfo;
        
        sourceA = new DirectoryInfo(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "sourceA"));
        sourceA.Create();
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(sourceA.FullName, "fileA.txt", "file1Content").FullName);
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(sourceA.FullName, "fileA_hidden.txt", "file1Content").FullName);
        File.SetAttributes(fileInfo.FullName, FileAttributes.Hidden);
        
        dir1 = sourceA.CreateSubdirectory("Dir1");
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(dir1.FullName, "fileA1.txt", "file1Content").FullName);
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(dir1.FullName, "fileA1_hidden.txt", "file1Content").FullName);
        File.SetAttributes(fileInfo.FullName, FileAttributes.Hidden);
        
        
        var inventoryAFilePath = IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, $"inventoryA.zip");
        
        var sessionSettings = SessionSettings.BuildDefault();
        sessionSettings.ExcludeHiddenFiles = excludeHiddenFiles;
        
        inventoryBuilder = BuildInventoryBuilder(sessionSettings);
        inventoryBuilder.AddInventoryPart(sourceA.FullName);
        await inventoryBuilder.BuildBaseInventoryAsync(inventoryAFilePath);
        
        File.Exists(inventoryAFilePath).Should().BeTrue();
        
        unzipDir = new DirectoryInfo(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "unzip"));
        unzipDir.Create();
        
        var fastZip = new FastZip();
        fastZip.ExtractZip(inventoryAFilePath, unzipDir.FullName, null);
        
        unzipDir.GetFiles("*", SearchOption.AllDirectories).Length.Should().Be(1);
        File.Exists(IOUtils.Combine(unzipDir.FullName, $"inventory.json")).Should().BeTrue();
        
        inventory = inventoryBuilder.Inventory!;
        inventory.InventoryParts.Count.Should().Be(1);
        inventory.InventoryParts[0].DirectoryDescriptions.Count.Should().Be(1);
        inventory.InventoryParts[0].FileDescriptions.Count.Should().Be(2 + expectedHiddenFiles);
    }
    
    [Test]
    [Platform(Exclude = "Win")]
    [TestCase(true, 0)]
    [TestCase(false, 2)]
    public async Task Test_HiddenFiles_Linux(bool excludeHiddenFiles, int expectedHiddenFiles)
    {
        InventoryBuilder inventoryBuilder;
        Inventory inventory;
        
        DirectoryInfo sourceA, dir1, rootAb, unzipDir;
        FileInfo fileInfo;
        
        sourceA = new DirectoryInfo(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "sourceA"));
        sourceA.Create();
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(sourceA.FullName, "fileA.txt", "file1Content").FullName);
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(sourceA.FullName, ".fileA_hidden.txt", "file1Content")
            .FullName);
        
        dir1 = sourceA.CreateSubdirectory("Dir1");
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(dir1.FullName, "fileA1.txt", "file1Content").FullName);
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(dir1.FullName, ".fileA1_hidden.txt", "file1Content").FullName);
        
        
        var inventoryAFilePath = IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, $"inventoryA.zip");
        
        var sessionSettings = SessionSettings.BuildDefault();
        sessionSettings.ExcludeHiddenFiles = excludeHiddenFiles;
        
        inventoryBuilder = BuildInventoryBuilder(sessionSettings);
        inventoryBuilder.AddInventoryPart(sourceA.FullName);
        await inventoryBuilder.BuildBaseInventoryAsync(inventoryAFilePath);
        
        File.Exists(inventoryAFilePath).Should().BeTrue();
        
        unzipDir = new DirectoryInfo(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "unzip"));
        unzipDir.Create();
        
        var fastZip = new FastZip();
        fastZip.ExtractZip(inventoryAFilePath, unzipDir.FullName, null);
        
        unzipDir.GetFiles("*", SearchOption.AllDirectories).Length.Should().Be(1);
        File.Exists(IOUtils.Combine(unzipDir.FullName, $"inventory.json")).Should().BeTrue();
        
        inventory = inventoryBuilder.Inventory!;
        inventory.InventoryParts.Count.Should().Be(1);
        inventory.InventoryParts[0].DirectoryDescriptions.Count.Should().Be(1);
        inventory.InventoryParts[0].FileDescriptions.Count.Should().Be(2 + expectedHiddenFiles);
    }
    
    /*
    [Test]
    [TestCase(true, 2, 0)]
    [TestCase(false, 8, 2, ExcludePlatform = "Linux")]
    [TestCase(false, 6, 2, ExcludePlatform = "Win")]
    public async Task Test_SystemFiles(bool excludeSystemFiles, int expectedSystemFiles, int expectedDesktopIniFiles)
    {
        InventoryBuilder inventoryBuilder;
        Inventory inventory;
    
        DirectoryInfo sourceA,dir1, rootAb, unzipDir;
        FileInfo fileInfo;
    
        sourceA = new DirectoryInfo(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "sourceA"));
        sourceA.Create();
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(sourceA.FullName, "fileA.txt", "file1Content").FullName);
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(sourceA.FullName, "desktop.ini", "file1Content").FullName);
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(sourceA.FullName, "thumbs.db", "file1Content").FullName);
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(sourceA.FullName, ".DS_Store", "file1Content").FullName);
    
        dir1 = sourceA.CreateSubdirectory("Dir1");
        fileInfo =new FileInfo(_testDirectoryService.CreateFileInDirectory(dir1.FullName, "fileA1.txt", "file1Content").FullName);
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(dir1.FullName, "desktop.ini", "file1Content").FullName);
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(dir1.FullName, "thumbs.db", "file1Content").FullName);
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(dir1.FullName, ".DS_Store", "file1Content").FullName);
    
    
        string inventoryAFilePath = IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, $"inventoryA.zip");
    
        var sessionSettings = SessionSettings.BuildDefault();
        sessionSettings.ExcludeSystemFiles = excludeSystemFiles;
    
        inventoryBuilder = BuildInventoryBuilder(sessionSettings);
        inventoryBuilder.AddInventoryPart(sourceA.FullName);
        await inventoryBuilder.BuildBaseInventoryAsync(inventoryAFilePath);
    
        File.Exists(inventoryAFilePath).Should().BeTrue();
    
        unzipDir = new DirectoryInfo(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "unzip"));
        unzipDir.Create();
    
        var fastZip = new FastZip();
        fastZip.ExtractZip(inventoryAFilePath, unzipDir.FullName, null);
    
        unzipDir.GetFiles("*", SearchOption.AllDirectories).Length.Should().Be(1);
        File.Exists(IOUtils.Combine(unzipDir.FullName, $"inventory.json")).Should().BeTrue();
    
        inventory = inventoryBuilder.Inventory!;
        inventory.InventoryParts.Count.Should().Be(1);
        inventory.InventoryParts[0].DirectoryDescriptions.Count.Should().Be(1);
        inventory.InventoryParts[0].FileDescriptions.Count.Should().Be(expectedSystemFiles);
    
        inventory.InventoryParts[0].FileDescriptions.Count(fd => fd.Name.Equals("desktop.ini")).Should().Be(expectedDesktopIniFiles);
    }
    */
    
    [Test]
    [Platform(Exclude = "Linux")]
    [TestCase(true, true, 0)]
    [TestCase(false, true, 4)]
    [TestCase(true, false, 1)]
    [TestCase(false, false, 7)]
    public async Task Test_HiddenAndSystemFiles_Windows(bool excludeHiddenFiles, bool excludeSystemFiles, int expectedAdditionalFiles)
    {
        InventoryBuilder inventoryBuilder;
        Inventory inventory;
        
        DirectoryInfo sourceA, dir1, rootAb, unzipDir;
        FileInfo fileInfo;
        
        sourceA = new DirectoryInfo(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "sourceA"));
        sourceA.Create();
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(sourceA.FullName, "fileA.txt", "file1Content").FullName);
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(sourceA.FullName, "fileB.txt", "file1Content").FullName);
        File.SetAttributes(fileInfo.FullName, FileAttributes.Hidden);
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(sourceA.FullName, "fileC.txt", "file1Content").FullName);
        File.SetAttributes(fileInfo.FullName, FileAttributes.Hidden);
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(sourceA.FullName, "fileD.txt", "file1Content").FullName);
        File.SetAttributes(fileInfo.FullName, FileAttributes.Hidden);
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(sourceA.FullName, "fileE.txt", "file1Content").FullName);
        File.SetAttributes(fileInfo.FullName, FileAttributes.Hidden);
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(sourceA.FullName, "desktop.ini", "file1Content").FullName);
        
        dir1 = sourceA.CreateSubdirectory("Dir1");
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(dir1.FullName, "fileA1.txt", "file1Content").FullName);
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(dir1.FullName, "desktop.ini", "file1Content").FullName);
        File.SetAttributes(fileInfo.FullName, FileAttributes.Hidden);
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(dir1.FullName, "thumbs.db", "file1Content").FullName);
        File.SetAttributes(fileInfo.FullName, FileAttributes.Hidden);
        
        
        var inventoryAFilePath = IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, $"inventoryA.zip");
        
        var sessionSettings = SessionSettings.BuildDefault();
        sessionSettings.ExcludeHiddenFiles = excludeHiddenFiles;
        sessionSettings.ExcludeSystemFiles = excludeSystemFiles;
        
        inventoryBuilder = BuildInventoryBuilder(sessionSettings, null, null, OSPlatforms.Windows);
        inventoryBuilder.AddInventoryPart(sourceA.FullName);
        await inventoryBuilder.BuildBaseInventoryAsync(inventoryAFilePath);
        
        File.Exists(inventoryAFilePath).Should().BeTrue();
        
        unzipDir = new DirectoryInfo(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "unzip"));
        unzipDir.Create();
        
        var fastZip = new FastZip();
        fastZip.ExtractZip(inventoryAFilePath, unzipDir.FullName, null);
        
        unzipDir.GetFiles("*", SearchOption.AllDirectories).Length.Should().Be(1);
        File.Exists(IOUtils.Combine(unzipDir.FullName, $"inventory.json")).Should().BeTrue();
        
        inventory = inventoryBuilder.Inventory!;
        inventory.InventoryParts.Count.Should().Be(1);
        inventory.InventoryParts[0].DirectoryDescriptions.Count.Should().Be(1);
        inventory.InventoryParts[0].FileDescriptions.Count.Should().Be(2 + expectedAdditionalFiles);
    }
    
    [Test]
    [TestCase(true, true, 0)]
    [TestCase(false, true, 4)]
    [TestCase(true, false, 1)]
    [TestCase(false, false, 7)]
    public async Task Test_HiddenAndSystemFiles_Linux(bool excludeHiddenFiles, bool excludeSystemFiles, int expectedAdditionalFiles)
    {
        InventoryBuilder inventoryBuilder;
        Inventory inventory;
        
        DirectoryInfo sourceA, dir1, rootAb, unzipDir;
        FileInfo fileInfo;
        
        sourceA = new DirectoryInfo(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "sourceA"));
        sourceA.Create();
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(sourceA.FullName, "fileA.txt", "file1Content").FullName);
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(sourceA.FullName, ".fileB.txt", "file1Content").FullName);
        
        // File.SetAttributes(fileInfo.FullName, FileAttributes.Hidden);
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(sourceA.FullName, ".fileC.txt", "file1Content").FullName);
        
        // File.SetAttributes(fileInfo.FullName, FileAttributes.Hidden);
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(sourceA.FullName, ".fileD.txt", "file1Content").FullName);
        
        // File.SetAttributes(fileInfo.FullName, FileAttributes.Hidden);
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(sourceA.FullName, ".fileE.txt", "file1Content").FullName);
        
        // File.SetAttributes(fileInfo.FullName, FileAttributes.Hidden);
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(sourceA.FullName, "desktop.ini", "file1Content").FullName);
        
        dir1 = sourceA.CreateSubdirectory("Dir1");
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(dir1.FullName, "fileA1.txt", "file1Content").FullName);
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(dir1.FullName, ".desktop.ini", "file1Content").FullName);
        
        // File.SetAttributes(fileInfo.FullName, FileAttributes.Hidden);
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(dir1.FullName, ".thumbs.db", "file1Content").FullName);
        
        // File.SetAttributes(fileInfo.FullName, FileAttributes.Hidden);
        
        
        var inventoryAFilePath = IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, $"inventoryA.zip");
        
        var sessionSettings = SessionSettings.BuildDefault();
        sessionSettings.ExcludeHiddenFiles = excludeHiddenFiles;
        sessionSettings.ExcludeSystemFiles = excludeSystemFiles;
        
        inventoryBuilder = BuildInventoryBuilder(sessionSettings, null, null, OSPlatforms.Linux);
        inventoryBuilder.AddInventoryPart(sourceA.FullName);
        await inventoryBuilder.BuildBaseInventoryAsync(inventoryAFilePath);
        
        File.Exists(inventoryAFilePath).Should().BeTrue();
        
        unzipDir = new DirectoryInfo(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "unzip"));
        unzipDir.Create();
        
        var fastZip = new FastZip();
        fastZip.ExtractZip(inventoryAFilePath, unzipDir.FullName, null);
        
        unzipDir.GetFiles("*", SearchOption.AllDirectories).Length.Should().Be(1);
        File.Exists(IOUtils.Combine(unzipDir.FullName, $"inventory.json")).Should().BeTrue();
        
        inventory = inventoryBuilder.Inventory!;
        inventory.InventoryParts.Count.Should().Be(1);
        inventory.InventoryParts[0].DirectoryDescriptions.Count.Should().Be(1);
        inventory.InventoryParts[0].FileDescriptions.Count.Should().Be(2 + expectedAdditionalFiles);
    }
    
    [Test]
    [Platform(Exclude = "Win")]
    public async Task Test_ReparsePoint()
    {
        InventoryBuilder inventoryBuilder;
        Inventory inventory;
        
        DirectoryInfo sourceA, dir1, rootAb, unzipDir;
        FileInfo fileInfo;
        
        // CreateTestDirectory();
        sourceA = new DirectoryInfo(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "sourceA"));
        sourceA.Create();
        
        var symLinkDest = _testDirectoryService.CreateSubTestDirectory("symLinkDest");
        
        // https://stackoverflow.com/questions/58038683/allow-mklink-for-a-non-admin-user
        // On Windows, you need to put the computer in developer mode for CreateSymbolicLink to work
        // Settings => Developer mode
        
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(sourceA.FullName, "fileA.txt", "file1Content").FullName);
        File.CreateSymbolicLink(IOUtils.Combine(sourceA.FullName, "fileA_reparsePoint.txt"), symLinkDest.FullName);
        
        dir1 = sourceA.CreateSubdirectory("Dir1");
        fileInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(dir1.FullName, "fileA1.txt", "file1Content").FullName);
        File.CreateSymbolicLink(IOUtils.Combine(dir1.FullName, "fileA_reparsePoint.txt"), symLinkDest.FullName);
        
        
        var inventoryAFilePath = IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, $"inventoryA.zip");
        
        var sessionSettings = SessionSettings.BuildDefault();
        
        inventoryBuilder = BuildInventoryBuilder(sessionSettings);
        inventoryBuilder.AddInventoryPart(sourceA.FullName);
        await inventoryBuilder.BuildBaseInventoryAsync(inventoryAFilePath);
        
        File.Exists(inventoryAFilePath).Should().BeTrue();
        
        unzipDir = new DirectoryInfo(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "unzip"));
        unzipDir.Create();
        
        var fastZip = new FastZip();
        fastZip.ExtractZip(inventoryAFilePath, unzipDir.FullName, null);
        
        unzipDir.GetFiles("*", SearchOption.AllDirectories).Length.Should().Be(1);
        File.Exists(IOUtils.Combine(unzipDir.FullName, $"inventory.json")).Should().BeTrue();
        
        inventory = inventoryBuilder.Inventory!;
        inventory.InventoryParts.Count.Should().Be(1);
        inventory.InventoryParts[0].DirectoryDescriptions.Count.Should().Be(1);
        inventory.InventoryParts[0].FileDescriptions.Count.Should().Be(2);
    }
    
    [Test]
    public async Task Test_GetBuildingStageData()
    {
        InventoryBuilder inventoryBuilder;
        Inventory inventory;
        
        DirectoryInfo sourceA, dir1, rootAb, unzipDir;
        FileInfo fileAInfo, fileA1Info;
        
        sourceA = new DirectoryInfo(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "sourceA"));
        sourceA.Create();
        fileAInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(sourceA.FullName, "fileA.txt", "fileAContent").FullName);
        dir1 = sourceA.CreateSubdirectory("Dir1");
        fileA1Info = new FileInfo(_testDirectoryService.CreateFileInDirectory(dir1.FullName, "fileA1.txt", "fileA1Content").FullName);
        
        
        var inventoryAFilePath = IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, $"inventoryA.zip");
        
        var sessionSettings = SessionSettings.BuildDefault();
        
        inventoryBuilder = BuildInventoryBuilder(sessionSettings);
        inventoryBuilder.AddInventoryPart(sourceA.FullName);
        await inventoryBuilder.BuildBaseInventoryAsync(inventoryAFilePath);
        
        File.Exists(inventoryAFilePath).Should().BeTrue();
        
        unzipDir = new DirectoryInfo(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "unzip"));
        unzipDir.Create();
        
        var fastZip = new FastZip();
        fastZip.ExtractZip(inventoryAFilePath, unzipDir.FullName, null);
        
        unzipDir.GetFiles("*", SearchOption.AllDirectories).Length.Should().Be(1);
        File.Exists(IOUtils.Combine(unzipDir.FullName, $"inventory.json")).Should().BeTrue();
        
        inventory = inventoryBuilder.Inventory!;
        inventory.InventoryParts.Count.Should().Be(1);
        inventory.InventoryParts[0].DirectoryDescriptions.Count.Should().Be(1);
        inventory.InventoryParts[0].FileDescriptions.Count.Should().Be(2);
    }
    
    [Test]
    public async Task Test_AnalysisError()
    {
        InventoryBuilder inventoryBuilder;
        Inventory inventory;
        
        DirectoryInfo sourceA, sourceB, dir1, rootAb, unzipDir;
        FileInfo fileAInfo, fileA1Info;
        
        var comparisonResultPreparer = Container.Resolve<ComparisonResultPreparer>();
        ComparisonResult comparisonResult;
        
        sourceA = new DirectoryInfo(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "sourceA"));
        sourceA.Create();
        fileAInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(sourceA.FullName, "fileA.txt", "fileAContent").FullName);
        var blockingStream = new FileStream(fileAInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.None);
        dir1 = sourceA.CreateSubdirectory("Dir1");
        fileA1Info = new FileInfo(_testDirectoryService.CreateFileInDirectory(dir1.FullName, "fileA1.txt", "fileA1Content").FullName);
        
        
        sourceB = new DirectoryInfo(IOUtils.Combine(_testDirectoryService.TestDirectory.FullName, "sourceB"));
        sourceB.Create();
        fileAInfo = new FileInfo(_testDirectoryService.CreateFileInDirectory(sourceB.FullName, "fileA.txt", "fileAContentOnB").FullName);
        dir1 = sourceA.CreateSubdirectory("Dir1");
        fileA1Info = new FileInfo(_testDirectoryService.CreateFileInDirectory(dir1.FullName, "fileA1.txt", "fileA1ContentOnB").FullName);
        
        var inventoryDataA = new InventoryData(sourceA);
        var inventoryDataB = new InventoryData(sourceB);
        
        
        var sessionSettings = SessionSettings.BuildDefault();
        sessionSettings.AnalysisMode = AnalysisModes.Smart;
        sessionSettings.DataType = DataTypes.Files;
        sessionSettings.MatchingMode = MatchingModes.Tree;
        sessionSettings.LinkingCase = LinkingCases.Insensitive;
        sessionSettings.ExcludeHiddenFiles = true;
        sessionSettings.ExcludeSystemFiles = true;
        comparisonResult = await comparisonResultPreparer.BuildAndCompare(sessionSettings, inventoryDataA, inventoryDataB);
        
        comparisonResult.ComparisonItems.Count.Should().Be(2);
        var comparisonItem = comparisonResult.ComparisonItems.Single(ci => ci.PathIdentity.FileName.Equals("fileA.txt"));
        
        var contentIdentity = comparisonItem.ContentIdentities.Single(ci => ci.IsPresentIn(inventoryDataA.Inventory));
        contentIdentity.HasAnalysisError.Should().BeTrue();
        contentIdentity.FileSystemDescriptions.Count.Should().Be(1);
        var fileDescription = (FileDescription)contentIdentity.FileSystemDescriptions.Single();
        fileDescription.AnalysisErrorType.Should().Be("IOException");
        fileDescription.AnalysisErrorDescription!.Should().StartWith("The process cannot access the file");
        fileDescription.SignatureGuid.Should().BeNull();
        contentIdentity.Core!.SignatureHash.Should().BeNull();
        
        contentIdentity = comparisonItem.ContentIdentities.Single(ci => ci.IsPresentIn(inventoryDataB.Inventory));
        contentIdentity.HasAnalysisError.Should().BeFalse();
        contentIdentity.FileSystemDescriptions.Count.Should().Be(1);
        fileDescription = (FileDescription)contentIdentity.FileSystemDescriptions.Single();
        fileDescription.AnalysisErrorDescription.Should().BeNull();
        fileDescription.AnalysisErrorType.Should().BeNull();
        fileDescription.SignatureGuid.IsNotEmpty(true).Should().BeTrue();
        contentIdentity.Core!.SignatureHash.IsNotEmpty(true).Should().BeTrue();
        
        // this assertion allows the blockingStream to be kept, even in Release mode
        blockingStream.CanRead.Should().BeTrue();
        
        blockingStream.Dispose();
    }
    
    private InventoryBuilder BuildInventoryBuilder(SessionSettings? sessionSettings = null,
        InventoryProcessData? inventoryProcessData = null, ByteSyncEndpoint? byteSyncEndpoint = null,
        OSPlatforms osPlatform = OSPlatforms.Windows)
    {
        if (sessionSettings == null)
        {
            sessionSettings = new SessionSettings();
        }
        
        if (inventoryProcessData == null)
        {
            inventoryProcessData = new InventoryProcessData();
        }
        
        if (byteSyncEndpoint == null)
        {
            byteSyncEndpoint = new ByteSyncEndpoint();
        }
        
        var sessionMemberInfo = new SessionMember
        {
            Endpoint = byteSyncEndpoint,
            PositionInList = 0,
            PrivateData = new()
            {
                MachineName = "MachineA"
            }
        };
        
        var dataNode = new DataNode
        {
            Id = Guid.NewGuid().ToString(),
            ClientInstanceId = sessionMemberInfo.ClientInstanceId,
            Code = "Code"
        };
        
        var loggerMock = new Mock<ILogger<InventoryBuilder>>();
        
        return new InventoryBuilder(sessionMemberInfo, dataNode, sessionSettings, inventoryProcessData,
            osPlatform, FingerprintModes.Rsync, loggerMock.Object,
            new InventoryFileAnalyzerFactory(),
            new InventorySaverFactory(),
            new InventoryIndexerFactory());
    }
}