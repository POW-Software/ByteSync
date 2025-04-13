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
        
        // Vérifier que les fichiers originaux n'existent plus
        File.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, "ByteSync.exe")).Should().BeFalse();
        File.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, "ByteSync.dll")).Should().BeFalse();
        
        // Vérifier que les fichiers de sauvegarde existent
        File.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, $"ByteSync.exe.{UpdateConstants.BAK_EXTENSION}0")).Should().BeTrue();
        File.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, $"ByteSync.dll.{UpdateConstants.BAK_EXTENSION}0")).Should().BeTrue();
        
        // Vérifier que le fichier régulier n'a pas été affecté
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
        
        // Vérifier que seul ByteSync.exe a été renommé
        File.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, "ByteSync.exe")).Should().BeFalse();
        File.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, $"ByteSync.exe.{UpdateConstants.BAK_EXTENSION}0")).Should().BeTrue();
        
        // Vérifier que les autres fichiers sont intacts
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
        
        // Vérifier que seul ByteSync.exe a été renommé
        File.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, "ByteSync.exe")).Should().BeFalse();
        File.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, $"ByteSync.exe.{UpdateConstants.BAK_EXTENSION}0")).Should().BeTrue();
        
        // Vérifier que le fichier unins000.exe est intact
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
        _backuper.BackedUpFileSystemInfos.Count.Should().Be(2); // Contents et ByteSync.app
        
        // Vérifier que les répertoires Contents et ByteSync.app ont été renommés
        Directory.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, "Contents")).Should().BeFalse();
        Directory.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, $"Contents.{UpdateConstants.BAK_EXTENSION}0")).Should().BeTrue();
        
        Directory.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, "ByteSync.app")).Should().BeFalse();
        Directory.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, $"ByteSync.app.{UpdateConstants.BAK_EXTENSION}0")).Should().BeTrue();
        
        // Vérifier que IgnoredDirectory est intact
        Directory.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, "IgnoredDirectory")).Should().BeTrue();
    }
    
    // [Test]
    // public async Task BackupExistingFilesAsync_ShouldIncrementBackupNumbers()
    // {
    //     // Arrange
    //     _testDirectoryService.CreateSubTestFile("ByteSync.exe", "exeContent");
    //     
    //     // Créer un fichier de sauvegarde existant
    //     _testDirectoryService.CreateSubTestFile($"ByteSync.exe.{UpdateConstants.BAK_EXTENSION}0", "oldBackup");
    //     
    //     // Act
    //     await _backuper.BackupExistingFilesAsync(CancellationToken.None);
    //     
    //     // Assert
    //     _backuper.BackedUpFileSystemInfos.Count.Should().Be(1);
    //     
    //     // Vérifier que le fichier original n'existe plus
    //     File.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, "ByteSync.exe")).Should().BeFalse();
    //     
    //     // Vérifier que la sauvegarde a été numérotée 1 (car 0 existe déjà)
    //     File.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, $"ByteSync.exe.{UpdateConstants.BAK_EXTENSION}1")).Should().BeTrue();
    //     
    //     // Vérifier que la première sauvegarde existe toujours
    //     File.Exists(Path.Combine(_testDirectoryService.TestDirectory.FullName, $"ByteSync.exe.{UpdateConstants.BAK_EXTENSION}0")).Should().BeTrue();
    // }
    
    [Test]
    public async Task BackupExistingFilesAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        for (int i = 0; i < 50; i++)
        {
            _testDirectoryService.CreateSubTestFile($"ByteSync.{i}.exe", $"content{i}");
        }
        
        // Act
        var cancellationTokenSource = new CancellationTokenSource();
        var task = _backuper.BackupExistingFilesAsync(cancellationTokenSource.Token);
        
        // Attendre un court délai pour que la tâche démarre
        await Task.Delay(50);
        
        // Annuler l'opération
        cancellationTokenSource.Cancel();
        
        // Attendre la fin de la tâche (ne devrait plus lever d'exception)
        await task;
        
        // Assert
        // On ne peut pas garantir exactement combien de fichiers ont été sauvegardés
        // avant l'annulation, mais il ne devrait pas y en avoir 50
        _backuper.BackedUpFileSystemInfos.Count.Should().BeLessThan(50);
    }
}

