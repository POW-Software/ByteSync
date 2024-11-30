using ByteSync.Services.Updates;
using ByteSync.TestsCommon;
using NUnit.Framework.Legacy;
using PowSoftware.Common.Business.Versions;

namespace ByteSync.Client.IntegrationTests.Services.Updates;

[TestFixture]
public class TestUpdateReplacer : AbstractTester
{
    [Test]
    public async Task Test_1()
    {
        CreateTestDirectory();

        string unzipLocation;
        string assemblyLocation;

        var unzipDirectory = TestFileSystemUtils.CreateSubTestDirectory(TestDirectory, "Unzip");
        unzipLocation = unzipDirectory.FullName;
        var assemblyDirectory = TestFileSystemUtils.CreateSubTestDirectory(TestDirectory, "Assembly");
        assemblyLocation = assemblyDirectory.FullName;

        SoftwareVersionFile softwareVersionFile = new SoftwareVersionFile();
        softwareVersionFile.ExecutableFileName = "MyProgram.exe";
        softwareVersionFile.Platform = Platform.Windows;
        
        // Génération du contenu de UnzipDirectory
        TestFileSystemUtils.CreateSubTestFile(unzipDirectory, "MyProgram.exe", "Content");

        UpdateReplacer updateReplacer = new UpdateReplacer(softwareVersionFile);
        await updateReplacer.ReplaceFilesAsync(unzipLocation, assemblyLocation);
        
        unzipDirectory.Refresh();
        assemblyDirectory.Refresh();
        
        ClassicAssert.AreEqual(0, unzipDirectory.GetFiles().Length);
        ClassicAssert.AreEqual(1, assemblyDirectory.GetFiles().Length);
        ClassicAssert.AreEqual(1, assemblyDirectory.GetFiles().Count(f => f.Name.Equals("MyProgram.exe")));
        
        ClassicAssert.AreEqual(1, updateReplacer.MovedFiles.Count);
        ClassicAssert.AreEqual(0, updateReplacer.MovedDirectories.Count);
        ClassicAssert.AreEqual(0, updateReplacer.RenamedFiles.Count);
        ClassicAssert.AreEqual(0, updateReplacer.RenamedDirectories.Count);
        
        await updateReplacer.DeleteBackupData();
        unzipDirectory.Refresh();
        assemblyDirectory.Refresh();
        ClassicAssert.AreEqual(1, assemblyDirectory.GetFiles().Length);
        ClassicAssert.AreEqual(1, assemblyDirectory.GetFiles().Count(f => f.Name.Equals("MyProgram.exe")));
    }
    
    [Test]
    public async Task Test_2()
    {
        CreateTestDirectory();

        string unzipLocation;
        string assemblyLocation;

        var unzipDirectory = TestFileSystemUtils.CreateSubTestDirectory(TestDirectory, "Unzip");
        unzipLocation = unzipDirectory.FullName;
        var assemblyDirectory = TestFileSystemUtils.CreateSubTestDirectory(TestDirectory, "Assembly");
        assemblyLocation = assemblyDirectory.FullName;

        SoftwareVersionFile softwareVersionFile = new SoftwareVersionFile();
        softwareVersionFile.ExecutableFileName = "MyProgram.exe";
        softwareVersionFile.Platform = Platform.Windows;
        
        // Génération du contenu de UnzipDirectory
        TestFileSystemUtils.CreateSubTestFile(unzipDirectory, "SubDir1\\MyProgram.exe", "Content");

        UpdateReplacer updateReplacer = new UpdateReplacer(softwareVersionFile);
        await updateReplacer.ReplaceFilesAsync(unzipLocation, assemblyLocation);
        
        unzipDirectory.Refresh();
        assemblyDirectory.Refresh();
        
        ClassicAssert.AreEqual(0, unzipDirectory.GetFiles().Length);
        ClassicAssert.AreEqual(1, unzipDirectory.GetDirectories().Length);
        ClassicAssert.AreEqual(0, unzipDirectory.GetDirectories()[0].GetFiles().Length);
        ClassicAssert.AreEqual(1, assemblyDirectory.GetFiles().Length);
        ClassicAssert.AreEqual(1, assemblyDirectory.GetFiles().Count(f => f.Name.Equals("MyProgram.exe")));
        
        ClassicAssert.AreEqual(1, updateReplacer.MovedFiles.Count);
        ClassicAssert.AreEqual(0, updateReplacer.MovedDirectories.Count);
        ClassicAssert.AreEqual(0, updateReplacer.RenamedFiles.Count);
        ClassicAssert.AreEqual(0, updateReplacer.RenamedDirectories.Count);
        
        await updateReplacer.DeleteBackupData();
        unzipDirectory.Refresh();
        assemblyDirectory.Refresh();
        ClassicAssert.AreEqual(1, assemblyDirectory.GetFiles().Length);
        ClassicAssert.AreEqual(1, assemblyDirectory.GetFiles().Count(f => f.Name.Equals("MyProgram.exe")));
    }
    
    [Test]
    public async Task Test_3()
    {
        CreateTestDirectory();

        string unzipLocation;
        string assemblyLocation;

        var unzipDirectory = TestFileSystemUtils.CreateSubTestDirectory(TestDirectory, "Unzip");
        unzipLocation = unzipDirectory.FullName;
        var assemblyDirectory = TestFileSystemUtils.CreateSubTestDirectory(TestDirectory, "Assembly");
        assemblyLocation = assemblyDirectory.FullName;

        SoftwareVersionFile softwareVersionFile = new SoftwareVersionFile();
        softwareVersionFile.ExecutableFileName = "MyProgram.exe";
        softwareVersionFile.Platform = Platform.Windows;
        
        // Génération du contenu de UnzipDirectory
        TestFileSystemUtils.CreateSubTestFile(unzipDirectory, "SubDir1\\MyProgram.exe", "newContent");
        
        // Génération du contenu de AssemblyDirectory
        TestFileSystemUtils.CreateSubTestFile(assemblyDirectory, "MyProgram.exe", "previousContent");

        UpdateReplacer updateReplacer = new UpdateReplacer(softwareVersionFile);
        await updateReplacer.ReplaceFilesAsync(unzipLocation, assemblyLocation);
        
        unzipDirectory.Refresh();
        assemblyDirectory.Refresh();
        
        ClassicAssert.AreEqual(0, unzipDirectory.GetFiles().Length);
        ClassicAssert.AreEqual(1, unzipDirectory.GetDirectories().Length);
        ClassicAssert.AreEqual(0, unzipDirectory.GetDirectories()[0].GetFiles().Length);
        ClassicAssert.AreEqual(2, assemblyDirectory.GetFiles().Length);
        ClassicAssert.AreEqual(1, assemblyDirectory.GetFiles().Count(f => f.Name.Equals("MyProgram.exe")));
        ClassicAssert.AreEqual(1, assemblyDirectory.GetFiles().Count(f => f.Name.Equals("MyProgram.exe.pow_upd_bak0")));
        
        ClassicAssert.AreEqual(1, updateReplacer.MovedFiles.Count);
        ClassicAssert.AreEqual(0, updateReplacer.MovedDirectories.Count);
        ClassicAssert.AreEqual(1, updateReplacer.RenamedFiles.Count);
        ClassicAssert.AreEqual(0, updateReplacer.RenamedDirectories.Count);

        await updateReplacer.DeleteBackupData();
        unzipDirectory.Refresh();
        assemblyDirectory.Refresh();
        ClassicAssert.AreEqual(1, assemblyDirectory.GetFiles().Length);
        ClassicAssert.AreEqual(1, assemblyDirectory.GetFiles().Count(f => f.Name.Equals("MyProgram.exe")));
    }
}