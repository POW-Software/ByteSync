using System.Reflection;
using ByteSync.Business.Configurations;
using ByteSync.Business.Misc;
using ByteSync.Common.Business.Misc;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Services.Configurations;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Services.Configurations;

[TestFixture]
public class LocalApplicationDataManagerTests
{
    private Mock<IEnvironmentService> _environmentServiceMock = null!;
    
    [SetUp]
    public void SetUp()
    {
        _environmentServiceMock = new Mock<IEnvironmentService>();
        _environmentServiceMock.SetupGet(e => e.IsPortableApplication).Returns(false);
        _environmentServiceMock.SetupGet(e => e.ExecutionMode).Returns(ExecutionMode.Regular);
        _environmentServiceMock.SetupProperty(e => e.Arguments, []);
    }
    
    [Test]
    public void ApplicationDataPath_Should_Be_MSIX_LocalCache_When_Installed_From_Store()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Ignore("Windows-only behavior");
        }
        
        // Arrange
        _environmentServiceMock.SetupGet(e => e.AssemblyFullName)
            .Returns(@"C:\\Program Files\\WindowsApps\\POWSoftware.ByteSync_2025.7.2.0_neutral__f852479tj7xda\\ByteSync.exe");
        _environmentServiceMock.SetupGet(e => e.DeploymentMode).Returns(DeploymentModes.MsixInstallation);
        _environmentServiceMock.SetupGet(e => e.MsixPackageFamilyName).Returns("POWSoftware.ByteSync_f852479tj7xda");
        
        // Act
        var ladm = new LocalApplicationDataManager(_environmentServiceMock.Object);
        
        // Assert
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var expectedRoot = IOUtils.Combine(local, "Packages", "POWSoftware.ByteSync_f852479tj7xda", "LocalCache", "Local");
        var expectedAppData = IOUtils.Combine(expectedRoot, "POW Software", "ByteSync");
        ladm.ApplicationDataPath.Should().Be(expectedAppData);
    }
    
    [Test]
    public void ApplicationDataPath_Should_Be_Logical_When_Not_Store()
    {
        // Arrange
        _environmentServiceMock.SetupGet(e => e.AssemblyFullName)
            .Returns(@"C:\\Program Files\\ByteSync\\ByteSync.exe");
        _environmentServiceMock.SetupGet(e => e.DeploymentMode).Returns(DeploymentModes.SetupInstallation);
        
        // Act
        var ladm = new LocalApplicationDataManager(_environmentServiceMock.Object);
        
        // Assert: logical path under %LOCALAPPDATA% (or %APPDATA% if roaming)
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        ladm.ApplicationDataPath.Should().StartWith(IOUtils.Combine(local, "POW Software", "ByteSync"));
    }
    
    [Test]
    public void ApplicationDataPath_Should_Append_CustomSuffix_When_DebugArgumentProvided()
    {
        // Arrange
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var assemblyDirectory = Directory.CreateDirectory(Path.Combine(tempRoot, "Portable")).FullName;
        var assemblyPath = Path.Combine(assemblyDirectory, "ByteSync.exe");
        
        _environmentServiceMock.SetupGet(e => e.AssemblyFullName).Returns(assemblyPath);
        _environmentServiceMock.SetupGet(e => e.DeploymentMode).Returns(DeploymentModes.Portable);
        _environmentServiceMock.SetupGet(e => e.ExecutionMode).Returns(ExecutionMode.Debug);
        
        var argumentValue = "CustomSuffix";
        var overrideArg = $"--ladm-use-appdata={argumentValue}";
        _environmentServiceMock.Object.Arguments = new[] { overrideArg };
        
        using var commandLineOverride = OverrideCommandLineArgs(["ByteSync.exe", overrideArg]);
        
        try
        {
            // Act
            var ladm = new LocalApplicationDataManager(_environmentServiceMock.Object);
            
            // Assert
            var expectedBase = Path.Combine(assemblyDirectory, "ApplicationData");
            ladm.ApplicationDataPath.Should().Be(expectedBase + $" {argumentValue}");
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }
    
    [Test]
    public void ApplicationDataPath_Should_Create_DebugDirectory_When_DebugModeWithoutOverride()
    {
        // Arrange
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var assemblyDirectory = Directory.CreateDirectory(Path.Combine(tempRoot, "Portable")).FullName;
        var assemblyPath = Path.Combine(assemblyDirectory, "ByteSync.exe");
        
        _environmentServiceMock.SetupGet(e => e.AssemblyFullName).Returns(assemblyPath);
        _environmentServiceMock.SetupGet(e => e.DeploymentMode).Returns(DeploymentModes.Portable);
        _environmentServiceMock.SetupGet(e => e.ExecutionMode).Returns(ExecutionMode.Debug);
        
        _environmentServiceMock.Object.Arguments = Array.Empty<string>();
        
        try
        {
            // Act
            var ladm = new LocalApplicationDataManager(_environmentServiceMock.Object);
            
            // Assert
            var basePath = Path.Combine(assemblyDirectory, "ApplicationData");
            var debugRoot = basePath + " Debug";
            ladm.ApplicationDataPath.Should().StartWith(debugRoot);
            Path.GetDirectoryName(ladm.ApplicationDataPath).Should().Be(debugRoot);
            Directory.Exists(ladm.ApplicationDataPath).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }
    
    [Test]
    public void LogFilePath_Should_Return_MostRecent_Log_Excluding_Debug()
    {
        // Arrange
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var assemblyDirectory = Directory.CreateDirectory(Path.Combine(tempRoot, "Portable")).FullName;
        var assemblyPath = Path.Combine(assemblyDirectory, "ByteSync.exe");
        
        _environmentServiceMock.SetupGet(e => e.AssemblyFullName).Returns(assemblyPath);
        _environmentServiceMock.SetupGet(e => e.DeploymentMode).Returns(DeploymentModes.Portable);
        
        try
        {
            var ladm = new LocalApplicationDataManager(_environmentServiceMock.Object);
            
            var logsDirectory = Path.Combine(ladm.ApplicationDataPath, LocalApplicationDataConstants.LOGS_DIRECTORY);
            Directory.CreateDirectory(logsDirectory);
            
            var olderLog = Path.Combine(logsDirectory, "ByteSync_20240101.log");
            var latestLog = Path.Combine(logsDirectory, "ByteSync_20240102.log");
            var debugLog = Path.Combine(logsDirectory, "ByteSync_20240103_debug.log");
            
            File.WriteAllText(olderLog, "old");
            File.WriteAllText(latestLog, "latest");
            File.WriteAllText(debugLog, "debug");
            
            File.SetLastWriteTime(olderLog, DateTime.Now.AddMinutes(-10));
            File.SetLastWriteTime(latestLog, DateTime.Now.AddMinutes(-5));
            File.SetLastWriteTime(debugLog, DateTime.Now);
            
            // Act
            var result = ladm.LogFilePath;
            
            // Assert
            result.Should().Be(new FileInfo(latestLog).FullName);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }
    
    [Test]
    public void DebugLogFilePath_Should_Return_MostRecent_Debug_Log()
    {
        // Arrange
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var assemblyDirectory = Directory.CreateDirectory(Path.Combine(tempRoot, "Portable")).FullName;
        var assemblyPath = Path.Combine(assemblyDirectory, "ByteSync.exe");
        
        _environmentServiceMock.SetupGet(e => e.AssemblyFullName).Returns(assemblyPath);
        _environmentServiceMock.SetupGet(e => e.DeploymentMode).Returns(DeploymentModes.Portable);
        
        try
        {
            var ladm = new LocalApplicationDataManager(_environmentServiceMock.Object);
            
            var logsDirectory = Path.Combine(ladm.ApplicationDataPath, LocalApplicationDataConstants.LOGS_DIRECTORY);
            Directory.CreateDirectory(logsDirectory);
            
            var olderDebug = Path.Combine(logsDirectory, "ByteSync_20240101_debug.log");
            var latestDebug = Path.Combine(logsDirectory, "ByteSync_20240102_debug.log");
            var regularLog = Path.Combine(logsDirectory, "ByteSync_20240103.log");
            
            File.WriteAllText(olderDebug, "older");
            File.WriteAllText(latestDebug, "latest");
            File.WriteAllText(regularLog, "regular");
            
            File.SetLastWriteTime(olderDebug, DateTime.Now.AddMinutes(-10));
            File.SetLastWriteTime(latestDebug, DateTime.Now);
            File.SetLastWriteTime(regularLog, DateTime.Now.AddMinutes(5));
            
            // Act
            var result = ladm.DebugLogFilePath;
            
            // Assert
            result.Should().Be(new FileInfo(latestDebug).FullName);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }
    
    private static IDisposable OverrideCommandLineArgs(string[] args)
    {
        var field = typeof(Environment).GetField("s_commandLineArgs", BindingFlags.Static | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException("Unable to override command line arguments for tests.");
        }
        
        var original = (string[]?)field.GetValue(null);
        field.SetValue(null, args);
        
        return new DelegateDisposable(() => field.SetValue(null, original));
    }
    
    private sealed class DelegateDisposable : IDisposable
    {
        private readonly Action _dispose;
        
        public DelegateDisposable(Action dispose)
        {
            _dispose = dispose;
        }
        
        public void Dispose()
        {
            _dispose();
        }
    }
}