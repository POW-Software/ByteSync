using System.Runtime.InteropServices;
using Autofac;
using ByteSync.Business.Updates;
using ByteSync.Client.IntegrationTests.TestHelpers;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Updates;
using ByteSync.Services.Updates;
using ByteSync.TestsCommon;
using FluentAssertions;
using Moq;

namespace ByteSync.Client.IntegrationTests.Services.Updates;

public class TestDeleteUpdateBackupSnippetsService : IntegrationTest
{
    [Test]
    public async Task With_UpdateHelperServiceMock_ShouldWorkProperly()
    {
        // Arrange
        RegisterType<DeleteUpdateBackupSnippetsService>();
        BuildMoqContainer();
        
        _testDirectoryService.CreateTestDirectory();
        
        var mockUpdateHelperService = Container.Resolve<Mock<IUpdateHelperService>>();
        var testDirectoryService = Container.Resolve<ITestDirectoryService>();
        var deleteUpdateBackupSnippetsService = Container.Resolve<DeleteUpdateBackupSnippetsService>();
        
        mockUpdateHelperService.Setup(u => u.GetApplicationBaseDirectory()).Returns(testDirectoryService.TestDirectory);
        
        // Create test files and backup files
        testDirectoryService.CreateSubTestFile("file1.txt", "file1Content");
        testDirectoryService.CreateSubTestFile("file2.txt", "file2Content");
        testDirectoryService.CreateSubTestFile("file3.txt", "file3Content");
        
        testDirectoryService.CreateSubTestFile($"file4.txt.{UpdateConstants.BAK_EXTENSION}0", "file4Content");
        testDirectoryService.CreateSubTestFile($"file5.txt.{UpdateConstants.BAK_EXTENSION}1", "file5Content");
        testDirectoryService.CreateSubTestFile($"file6.txt.{UpdateConstants.BAK_EXTENSION}15", "file6Content");
        testDirectoryService.CreateSubTestFile($"file7.txt.{UpdateConstants.BAK_EXTENSION}15.ext", "file7Content");

        // Act
        await deleteUpdateBackupSnippetsService.DeleteBackupSnippetsAsync();
        
        // Assert
        testDirectoryService.TestDirectory.Refresh();
        var files = testDirectoryService.TestDirectory.GetFiles("*", SearchOption.AllDirectories);
        
        files.Length.Should().Be(4);
        files.Count(f => f.Name.StartsWith("file1.")).Should().Be(1);
        files.Count(f => f.Name.StartsWith("file2.")).Should().Be(1);
        files.Count(f => f.Name.StartsWith("file3.")).Should().Be(1);
        files.Count(f => f.Name.StartsWith("file7.")).Should().Be(1);
    }

    [Test]
    public async Task With_UpdateHelperServiceMock_ShouldNotAffectSubDirectories()
    {
        // Arrange
        RegisterType<DeleteUpdateBackupSnippetsService>();
        BuildMoqContainer();

        _testDirectoryService.CreateTestDirectory();

        var mockUpdateHelperService = Container.Resolve<Mock<IUpdateHelperService>>();
        var testDirectoryService = Container.Resolve<ITestDirectoryService>();
        var deleteUpdateBackupSnippetsService = Container.Resolve<DeleteUpdateBackupSnippetsService>();

        mockUpdateHelperService.Setup(u => u.GetApplicationBaseDirectory()).Returns(testDirectoryService.TestDirectory);
        
        // Create test files, subdirectories, and backup files
        testDirectoryService.CreateSubTestFile("file1.txt", "file1Content");
        testDirectoryService.CreateSubTestFile($"file_to_delete.{UpdateConstants.BAK_EXTENSION}0", "fileContent");
        
        var subDir = testDirectoryService.TestDirectory.CreateSubdirectory("subdir");
        await File.WriteAllTextAsync(Path.Combine(subDir.FullName, "subdir_file.txt"), "normalContent");
        await File.WriteAllTextAsync(Path.Combine(subDir.FullName, $"subdir_backup.{UpdateConstants.BAK_EXTENSION}0"), "backupContent");
        
        var backupDir = testDirectoryService.TestDirectory.CreateSubdirectory($"dir.{UpdateConstants.BAK_EXTENSION}1");
        await File.WriteAllTextAsync(Path.Combine(backupDir.FullName, "file_in_backup_dir.txt"), "fileContent");

        // Act
        await deleteUpdateBackupSnippetsService.DeleteBackupSnippetsAsync();

        // Assert
        testDirectoryService.TestDirectory.Refresh();
        
        File.Exists(Path.Combine(testDirectoryService.TestDirectory.FullName, "file1.txt")).Should().BeTrue();
        File.Exists(Path.Combine(testDirectoryService.TestDirectory.FullName, $"file_to_delete.{UpdateConstants.BAK_EXTENSION}0")).Should().BeFalse();
        Directory.Exists(Path.Combine(testDirectoryService.TestDirectory.FullName, $"dir.{UpdateConstants.BAK_EXTENSION}1")).Should().BeFalse();
        Directory.Exists(Path.Combine(testDirectoryService.TestDirectory.FullName, "subdir")).Should().BeTrue();
        File.Exists(Path.Combine(subDir.FullName, "subdir_file.txt")).Should().BeTrue();
        File.Exists(Path.Combine(subDir.FullName, $"subdir_backup.{UpdateConstants.BAK_EXTENSION}0")).Should().BeTrue();
    }

    [Test]
    public async Task With_UpdateHelperServiceMock_ShouldDeleteUpdateUnzipDirectoriesAtRootOnly()
    {
        // Arrange
        RegisterType<DeleteUpdateBackupSnippetsService>();
        BuildMoqContainer();

        _testDirectoryService.CreateTestDirectory();

        var mockUpdateHelperService = Container.Resolve<Mock<IUpdateHelperService>>();
        var testDirectoryService = Container.Resolve<ITestDirectoryService>();
        var deleteUpdateBackupSnippetsService = Container.Resolve<DeleteUpdateBackupSnippetsService>();

        mockUpdateHelperService.Setup(u => u.GetApplicationBaseDirectory()).Returns(testDirectoryService.TestDirectory);

        // Create test files and directories
        testDirectoryService.CreateSubTestFile("normal.txt", "normalContent");

        // Update extraction directory at the root (should be deleted)
        var rootExtractDir = testDirectoryService.TestDirectory.CreateSubdirectory($"{UpdateConstants.UPDATE_UNZIP_EXTRACT_START_NAME}_123");
        await File.WriteAllTextAsync(Path.Combine(rootExtractDir.FullName, "extract.txt"), "extractContent");

        // Normal directory at the root
        var normalDir = testDirectoryService.TestDirectory.CreateSubdirectory("normalDir");

        // Normal file in a subdirectory
        await File.WriteAllTextAsync(Path.Combine(normalDir.FullName, "subdir_file.txt"), "normalContent");

        // Update extraction directory in a subdirectory (should not be deleted)
        var subExtractDir = normalDir.CreateSubdirectory($"{UpdateConstants.UPDATE_UNZIP_EXTRACT_START_NAME}_456");
        await File.WriteAllTextAsync(Path.Combine(subExtractDir.FullName, "sub_extract.txt"), "subExtractContent");

        // File with extraction pattern in a subdirectory (should not be deleted)
        await File.WriteAllTextAsync(
            Path.Combine(normalDir.FullName, $"{UpdateConstants.UPDATE_UNZIP_EXTRACT_START_NAME}_file.txt"),
            "extractNamedContent"
        );

        // Act
        await deleteUpdateBackupSnippetsService.DeleteBackupSnippetsAsync();

        // Assert
        testDirectoryService.TestDirectory.Refresh();

        File.Exists(Path.Combine(testDirectoryService.TestDirectory.FullName, "normal.txt")).Should().BeTrue();
        Directory.Exists(Path.Combine(testDirectoryService.TestDirectory.FullName, $"{UpdateConstants.UPDATE_UNZIP_EXTRACT_START_NAME}_123")).Should().BeFalse();
        Directory.Exists(Path.Combine(testDirectoryService.TestDirectory.FullName, "normalDir")).Should().BeTrue();
        File.Exists(Path.Combine(normalDir.FullName, "subdir_file.txt")).Should().BeTrue();
        Directory.Exists(Path.Combine(normalDir.FullName, $"{UpdateConstants.UPDATE_UNZIP_EXTRACT_START_NAME}_456")).Should().BeTrue();
        File.Exists(Path.Combine(subExtractDir.FullName, "sub_extract.txt")).Should().BeTrue();
        File.Exists(Path.Combine(normalDir.FullName, $"{UpdateConstants.UPDATE_UNZIP_EXTRACT_START_NAME}_file.txt")).Should().BeTrue();
    }
}
