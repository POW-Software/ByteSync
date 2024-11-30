using ByteSync.TestsCommon;

namespace ByteSync.Client.IntegrationTests.Services.Updates;

[TestFixture]
public class TestUpdateExtractor : AbstractTester
{
    /*
    [Test]
    public async Task TestUnzipWindows()
    {
        
        UpdateExtractor updateExtractor;

        CreateTestDirectory();
        var sourceDir = _testDirectoryService.CreateSubTestDirectory("Source");
        var unzippedDir = _testDirectoryService.CreateSubTestDirectory("Unzipped");
        var zipFile = new FileInfo(IOUtils.Combine(TestDirectory.FullName, "zipFile.zip"));

        // Création de contenu
        TestFileSystemUtils.CreateSubTestFile(sourceDir, "file1.txt", "content1");
        TestFileSystemUtils.CreateSubTestFile(sourceDir, "file2.txt", "content22");
        TestFileSystemUtils.CreateSubTestFile(sourceDir, "file3.txt", "content333");
        
        Zipper.CreateZip(sourceDir.FullName, zipFile.FullName);

        SoftwareVersionFile softwareVersionFile = new SoftwareVersionFile();
        softwareVersionFile.Platform = Platform.Windows;
        softwareVersionFile.FileName = zipFile.Name;
        updateExtractor = new UpdateExtractor();
        await updateExtractor.ExtractAsync(softwareVersionFile, zipFile.FullName, unzippedDir.FullName);
        
        var unzippedFiles = Enumerable.ToList(unzippedDir.GetFiles("*", SearchOption.AllDirectories));
        Assert.Equals(3, unzippedFiles.Count);
        ClassicAssert.IsTrue(unzippedFiles.Count(f => f.Name.Equals("file1.txt") && f.Length == "content1".Length) == 1);
        ClassicAssert.IsTrue(unzippedFiles.Count(f => f.Name.Equals("file2.txt") && f.Length == "content22".Length) == 1);
        ClassicAssert.IsTrue(unzippedFiles.Count(f => f.Name.Equals("file3.txt") && f.Length == "content333".Length) == 1);
    }
    
    // 01/09/2022 : Plus d'actualité après les changements sur le process de déploiement Mac effectués cet été
    // [Test]
    // public async Task TestUnzipMacOs()
    // {
    //     
    //     UpdateExtractor updateExtractor;
    //
    //     CreateTestDirectory();
    //     var sourceDir = _testDirectoryService.CreateSubTestDirectory("Source");
    //     var unzippedDir = _testDirectoryService.CreateSubTestDirectory("Unzipped");
    //     var zipFile = new FileInfo(IOUtils.Combine(TestDirectory.FullName, "zipFile.zip"));
    //
    //     // Création de contenu
    //     TestFileSystemUtils.CreateSubTestFile(sourceDir, "file1.txt", "content1");
    //     TestFileSystemUtils.CreateSubTestFile(sourceDir, "file2.txt", "content22");
    //     TestFileSystemUtils.CreateSubTestFile(sourceDir, "file3.txt", "content333");
    //     
    //     Zipper.CreateZip(sourceDir.FullName, zipFile.FullName);
    //
    //     SoftwareVersionFile softwareVersionFile = new SoftwareVersionFile();
    //     softwareVersionFile.Platform = Platform.Osx;
    //     softwareVersionFile.FileName = zipFile.Name;
    //     updateExtractor = new UpdateExtractor();
    //     await updateExtractor.ExtractAsync(softwareVersionFile, zipFile.FullName, unzippedDir.FullName);
    //     
    //     var unzippedFiles = unzippedDir.GetFiles("*", SearchOption.AllDirectories).ToList();
    //     Assert.Equals(3, unzippedFiles.Count);
    //     ClassicAssert.IsTrue(unzippedFiles.Count(f => f.Name.Equals("file1.txt") && f.Length == "content1".Length) == 1);
    //     ClassicAssert.IsTrue(unzippedFiles.Count(f => f.Name.Equals("file2.txt") && f.Length == "content22".Length) == 1);
    //     ClassicAssert.IsTrue(unzippedFiles.Count(f => f.Name.Equals("file3.txt") && f.Length == "content333".Length) == 1);
    // }
    
    [Test]
    public async Task TestUntarLinux_Directory_DoNotKeepParent()
    {
        
        UpdateExtractor updateExtractor;

        CreateTestDirectory();
        var sourceDir = _testDirectoryService.CreateSubTestDirectory("Source");
        var unzippedDir = _testDirectoryService.CreateSubTestDirectory("Unzipped");
        var zipFile = new FileInfo(IOUtils.Combine(TestDirectory.FullName, "tarFile.tar.gz"));

        // Création de contenu
        TestFileSystemUtils.CreateSubTestFile(sourceDir, "file1.txt", "content1");
        TestFileSystemUtils.CreateSubTestFile(sourceDir, "file2.txt", "content22");
        TestFileSystemUtils.CreateSubTestFile(sourceDir, "file3.txt", "content333");
        
        Zipper.CreateTarGz(sourceDir.FullName, zipFile.FullName, false);

        SoftwareVersionFile softwareVersionFile = new SoftwareVersionFile();
        softwareVersionFile.Platform = Platform.Linux;
        softwareVersionFile.FileName = zipFile.Name;
        updateExtractor = new UpdateExtractor();
        await updateExtractor.ExtractAsync(softwareVersionFile, zipFile.FullName, unzippedDir.FullName);
        
        var unzippedFiles = Enumerable.ToList(unzippedDir.GetFiles("*", SearchOption.TopDirectoryOnly));
        Assert.Equals(3, unzippedFiles.Count);
        ClassicAssert.IsTrue(unzippedFiles.Count(f => f.Name.Equals("file1.txt") && f.Length == "content1".Length) == 1);
        ClassicAssert.IsTrue(unzippedFiles.Count(f => f.Name.Equals("file2.txt") && f.Length == "content22".Length) == 1);
        ClassicAssert.IsTrue(unzippedFiles.Count(f => f.Name.Equals("file3.txt") && f.Length == "content333".Length) == 1);
        
        Assert.Equals(0, unzippedDir.GetDirectories().Length);
    }
    
    [Test]
    public async Task TestUntarLinux_Directory_KeepParent()
    {
        UpdateExtractor updateExtractor;

        CreateTestDirectory();
        var sourceDir = _testDirectoryService.CreateSubTestDirectory("Source");
        var unzippedDir = _testDirectoryService.CreateSubTestDirectory("Unzipped");
        var zipFile = new FileInfo(IOUtils.Combine(TestDirectory.FullName, "tarFile.tar.gz"));

        // Création de contenu
        TestFileSystemUtils.CreateSubTestFile(sourceDir, "file1.txt", "content1");
        TestFileSystemUtils.CreateSubTestFile(sourceDir, "file2.txt", "content22");
        TestFileSystemUtils.CreateSubTestFile(sourceDir, "file3.txt", "content333");
        
        Zipper.CreateTarGz(sourceDir.FullName, zipFile.FullName, true);

        SoftwareVersionFile softwareVersionFile = new SoftwareVersionFile();
        softwareVersionFile.Platform = Platform.Linux;
        softwareVersionFile.FileName = zipFile.Name;
        updateExtractor = new UpdateExtractor();
        await updateExtractor.ExtractAsync(softwareVersionFile, zipFile.FullName, unzippedDir.FullName);
        
        var unzippedFiles = Enumerable.ToList(unzippedDir.GetFiles("*", SearchOption.TopDirectoryOnly));
        Assert.Equals(0, unzippedFiles.Count);
        Assert.Equals(1, unzippedDir.GetDirectories().Length);

        var dir = unzippedDir.GetDirectories()[0];
        unzippedFiles = Enumerable.ToList(dir.GetFiles("*", SearchOption.TopDirectoryOnly));
        Assert.Equals(3, unzippedFiles.Count);
        Assert.Equals(3, unzippedFiles.Count);
        ClassicAssert.IsTrue(unzippedFiles.Count(f => f.Name.Equals("file1.txt") && f.Length == "content1".Length) == 1);
        ClassicAssert.IsTrue(unzippedFiles.Count(f => f.Name.Equals("file2.txt") && f.Length == "content22".Length) == 1);
        ClassicAssert.IsTrue(unzippedFiles.Count(f => f.Name.Equals("file3.txt") && f.Length == "content333".Length) == 1);
    }
    
    [Test]
    public async Task TestUntarLinux_File_DoNotKeepParent()
    {
        
        UpdateExtractor updateExtractor;

        CreateTestDirectory();
        var sourceDir = _testDirectoryService.CreateSubTestDirectory("Source");
        var unzippedDir = _testDirectoryService.CreateSubTestDirectory("Unzipped");
        var zipFile = new FileInfo(IOUtils.Combine(TestDirectory.FullName, "tarFile.tar.gz"));

        // Création de contenu
        var file1 = TestFileSystemUtils.CreateSubTestFile(sourceDir, "file1.txt", "content1");

        Zipper.CreateTarGz(file1.FullName, zipFile.FullName, false);

        SoftwareVersionFile softwareVersionFile = new SoftwareVersionFile();
        softwareVersionFile.Platform = Platform.Linux;
        softwareVersionFile.FileName = zipFile.Name;
        updateExtractor = new UpdateExtractor();
        await updateExtractor.ExtractAsync(softwareVersionFile, zipFile.FullName, unzippedDir.FullName);
        
        var unzippedFiles = Enumerable.ToList(unzippedDir.GetFiles("*", SearchOption.TopDirectoryOnly));
        Assert.Equals(1, unzippedFiles.Count);
        ClassicAssert.IsTrue(unzippedFiles.Count(f => f.Name.Equals("file1.txt") && f.Length == "content1".Length) == 1);

        Assert.Equals(0, unzippedDir.GetDirectories().Length);
    }
    
    [Test]
    public async Task TestUntarLinux_File_KeepParent()
    {
        
        UpdateExtractor updateExtractor;

        CreateTestDirectory();
        var sourceDir = _testDirectoryService.CreateSubTestDirectory("Source");
        var unzippedDir = _testDirectoryService.CreateSubTestDirectory("Unzipped");
        var zipFile = new FileInfo(IOUtils.Combine(TestDirectory.FullName, "tarFile.tar.gz"));

        // Création de contenu
        var file1 = TestFileSystemUtils.CreateSubTestFile(sourceDir, "file1.txt", "content1");

        Zipper.CreateTarGz(file1.FullName, zipFile.FullName, true);

        SoftwareVersionFile softwareVersionFile = new SoftwareVersionFile();
        softwareVersionFile.Platform = Platform.Linux;
        softwareVersionFile.FileName = zipFile.Name;
        updateExtractor = new UpdateExtractor();
        await updateExtractor.ExtractAsync(softwareVersionFile, zipFile.FullName, unzippedDir.FullName);
        
        var unzippedFiles = Enumerable.ToList(unzippedDir.GetFiles("*", SearchOption.TopDirectoryOnly));
        Assert.Equals(0, unzippedFiles.Count);
        Assert.Equals(1, unzippedDir.GetDirectories().Length);
        
        unzippedFiles = Enumerable.ToList(unzippedDir.GetDirectories()[0].GetFiles("*", SearchOption.TopDirectoryOnly));
        Assert.Equals(1, unzippedFiles.Count);
        ClassicAssert.IsTrue(unzippedFiles.Count(f => f.Name.Equals("file1.txt") && f.Length == "content1".Length) == 1);
    }
    
    [Test]
    public async Task TestUnzipException()
    {
        UpdateExtractor updateExtractor;

        CreateTestDirectory();
        var sourceDir = _testDirectoryService.CreateSubTestDirectory("Source");
        var unzippedDir = _testDirectoryService.CreateSubTestDirectory("Unzipped");
        var zipFile = new FileInfo(IOUtils.Combine(TestDirectory.FullName, "zipFile.tar.gz")); // !!! tar.gz incompatible avec Platform.Windows;

        // Création de contenu
        TestFileSystemUtils.CreateSubTestFile(sourceDir, "file1.txt", "content1");
        TestFileSystemUtils.CreateSubTestFile(sourceDir, "file2.txt", "content22");
        TestFileSystemUtils.CreateSubTestFile(sourceDir, "file3.txt", "content333");
        
        Zipper.CreateZip(sourceDir.FullName, zipFile.FullName);

        SoftwareVersionFile softwareVersionFile = new SoftwareVersionFile();
        softwareVersionFile.Platform = Platform.Windows;
        softwareVersionFile.FileName = zipFile.Name;
        updateExtractor = new UpdateExtractor();
        var exception = Assert.ThrowsAsync<Exception>(() => updateExtractor.ExtractAsync(softwareVersionFile, zipFile.FullName, unzippedDir.FullName));
        ClassicAssert.IsNotNull(exception);
        ClassicAssert.IsTrue(exception.Message.Contains("cannot extract", StringComparison.InvariantCultureIgnoreCase));
    }
    */
}