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
    public async Task On_WindowsOrLinux_ShouldWorkProperly()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Assert.Ignore("This test is only for Windows and Linux");
        }
        
        // Arrange
        RegisterType<UpdateHelperService, IUpdateHelperService>();
        RegisterType<DeleteUpdateBackupSnippetsService>();
        BuildMoqContainer();
        
        var contextHelper = new TestContextGenerator(Container);
        _testDirectoryService.CreateTestDirectory();
        
        var environmentService = Container.Resolve<Mock<IEnvironmentService>>();
        var testDirectoryService = Container.Resolve<ITestDirectoryService>();
        var deleteUpdateBackupSnippetsService = Container.Resolve<DeleteUpdateBackupSnippetsService>();
        
        environmentService.Setup(e => e.AssemblyFullName).Returns(testDirectoryService.TestDirectory.FullName);
        
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
}