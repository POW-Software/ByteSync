using Autofac;
using ByteSync.Business.Updates;
using ByteSync.Interfaces.Repositories.Updates;
using ByteSync.Services.Updates;
using ByteSync.TestsCommon;
using FluentAssertions;
using Moq;

namespace ByteSync.Client.IntegrationTests.Services.Updates;

public class TestUpdateExistingFilesBackuper : IntegrationTest
{
    private UpdateExistingFilesBackuper _backuper;
    
    [SetUp]
    public void SetUp()
    {
        RegisterType<UpdateExistingFilesBackuper>();
        BuildMoqContainer();
        
        _testDirectoryService.CreateTestDirectory();
        
        var mockUpdateRepository = Container.Resolve<Mock<IUpdateRepository>>();
        mockUpdateRepository.Setup(r => r.UpdateData).Returns(new UpdateData 
        { 
            ApplicationBaseDirectory = _testDirectoryService.TestDirectory.FullName 
        });
        
        _backuper = Container.Resolve<UpdateExistingFilesBackuper>();
    }
    
    [Test]
    public async Task BackupExistingFilesAsync_ShouldBackupFilesWithByteSyncInName()
    {
        // Arrange
        _testDirectoryService.CreateSubTestFile("ByteSync.exe", "exeContent");
        _testDirectoryService.CreateSubTestFile("ByteSync.dll", "dllContent");
        _testDirectoryService.CreateSubTestFile("regular.txt", "textContent");
        
        // Act
        await _backuper.BackupExistingFilesAsync(CancellationToken.None);
        
        // Assert
        _backuper.BackedUpFileSystemInfos.Count.Should().Be(2);
        
        // Verify that the original files no longer exist
        File.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, "ByteSync.exe")).Should().BeFalse();
        File.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, "ByteSync.dll")).Should().BeFalse();
        
        // Verify that the backup files exist
        File.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, $"ByteSync.exe.{UpdateConstants.BAK_EXTENSION}0")).Should().BeTrue();
        File.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, $"ByteSync.dll.{UpdateConstants.BAK_EXTENSION}0")).Should().BeTrue();
        
        // Verify that the regular file was not affected
        File.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, "regular.txt")).Should().BeTrue();
    }
    
    [Test]
    public async Task BackupExistingFilesAsync_ShouldIgnoreSpecificFileTypes()
    {
        // Arrange
        _testDirectoryService.CreateSubTestFile("ByteSync.log", "logContent");
        _testDirectoryService.CreateSubTestFile("ByteSync.dat", "datContent");
        _testDirectoryService.CreateSubTestFile("ByteSync.xml", "xmlContent");
        _testDirectoryService.CreateSubTestFile("ByteSync.json", "jsonContent");
        _testDirectoryService.CreateSubTestFile("ByteSync.zip", "zipContent");
        _testDirectoryService.CreateSubTestFile("ByteSync.exe", "exeContent");
        
        // Act
        await _backuper.BackupExistingFilesAsync(CancellationToken.None);
        
        // Assert
        _backuper.BackedUpFileSystemInfos.Count.Should().Be(1);
        
        // Verify that only ByteSync.exe was renamed
        File.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, "ByteSync.exe")).Should().BeFalse();
        File.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, $"ByteSync.exe.{UpdateConstants.BAK_EXTENSION}0")).Should().BeTrue();
        
        // Verify that the other files remain intact
        File.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, "ByteSync.log")).Should().BeTrue();
        File.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, "ByteSync.dat")).Should().BeTrue();
        File.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, "ByteSync.xml")).Should().BeTrue();
        File.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, "ByteSync.json")).Should().BeTrue();
        File.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, "ByteSync.zip")).Should().BeTrue();
    }
    
    [Test]
    public async Task BackupExistingFilesAsync_ShouldIgnoreUninstallerFiles()
    {
        // Arrange
        _testDirectoryService.CreateSubTestFile("unins000.exe", "uninstallerContent");
        _testDirectoryService.CreateSubTestFile("ByteSync.exe", "exeContent");
        
        // Act
        await _backuper.BackupExistingFilesAsync(CancellationToken.None);
        
        // Assert
        _backuper.BackedUpFileSystemInfos.Count.Should().Be(1);
        
        // Verify that only ByteSync.exe was renamed
        File.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, "ByteSync.exe")).Should().BeFalse();
        File.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, $"ByteSync.exe.{UpdateConstants.BAK_EXTENSION}0")).Should().BeTrue();
        
        // Verify that the unins000.exe file remains intact
        File.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, "unins000.exe")).Should().BeTrue();
    }
    
    [Test]
    public async Task BackupExistingFilesAsync_ShouldHandleDirectories()
    {
        // Arrange
        var contentsDir = _testDirectoryService.TestDirectory.CreateSubdirectory("Contents");
        var bytesyncAppDir = _testDirectoryService.TestDirectory.CreateSubdirectory("ByteSync.app");
        var ignoredDir = _testDirectoryService.TestDirectory.CreateSubdirectory("IgnoredDirectory");
        
        await File.WriteAllTextAsync(Path.Combine(contentsDir.FullName, "test.txt"), "contentsFile");
        await File.WriteAllTextAsync(Path.Combine(bytesyncAppDir.FullName, "app.txt"), "appFile");
        await File.WriteAllTextAsync(Path.Combine(ignoredDir.FullName, "ignored.txt"), "ignoredFile");
        
        // Act
        await _backuper.BackupExistingFilesAsync(CancellationToken.None);
        
        // Assert
        _backuper.BackedUpFileSystemInfos.Count.Should().Be(2); // Contents and ByteSync.app
        
        // Verify that the Contents and ByteSync.app directories were renamed
        Directory.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, "Contents")).Should().BeFalse();
        Directory.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, $"Contents.{UpdateConstants.BAK_EXTENSION}0")).Should().BeTrue();
        
        Directory.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, "ByteSync.app")).Should().BeFalse();
        Directory.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, $"ByteSync.app.{UpdateConstants.BAK_EXTENSION}0")).Should().BeTrue();
        
        // Verify that IgnoredDirectory remains intact
        Directory.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, "IgnoredDirectory")).Should().BeTrue();
    }
}
