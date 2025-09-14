using ByteSync.Business.Misc;
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
    [Test]
    public void ShellApplicationDataPath_Should_Map_To_MSIX_LocalCache_When_Installed_From_Store()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Ignore("Windows-only behavior");
        }

        var mockEnv = new Mock<IEnvironmentService>();
        mockEnv.SetupGet(e => e.IsPortableApplication).Returns(false);
        mockEnv.SetupGet(e => e.ExecutionMode).Returns(ExecutionMode.Regular);
        mockEnv.SetupProperty(e => e.Arguments, Array.Empty<string>());
        mockEnv.SetupGet(e => e.AssemblyFullName)
            .Returns(@"C:\\Program Files\\WindowsApps\\POWSoftware.ByteSync_2025.7.2.0_neutral__f852479tj7xda\\ByteSync.exe");

        var ladm = new LocalApplicationDataManager(mockEnv.Object);

        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var expectedShellRoot = IOUtils.Combine(local, "Packages", "POWSoftware.ByteSync_f852479tj7xda", "LocalCache", "Local");
        var expectedShellAppData = IOUtils.Combine(expectedShellRoot, "POW Software", "ByteSync");

        ladm.ShellApplicationDataPath.Should().Be(expectedShellAppData);

        var logicalLog = IOUtils.Combine(ladm.ApplicationDataPath, "Logs", "ByteSync_.log");
        var mapped = ladm.GetShellPath(logicalLog);
        var expectedMapped = IOUtils.Combine(expectedShellAppData, "Logs", "ByteSync_.log");
        mapped.Should().Be(expectedMapped);
    }

    [Test]
    public void ShellApplicationDataPath_Should_Equal_Logical_When_Not_Store()
    {
        var mockEnv = new Mock<IEnvironmentService>();
        mockEnv.SetupGet(e => e.IsPortableApplication).Returns(false);
        mockEnv.SetupGet(e => e.ExecutionMode).Returns(ExecutionMode.Regular);
        mockEnv.SetupProperty(e => e.Arguments, Array.Empty<string>());
        mockEnv.SetupGet(e => e.AssemblyFullName)
            .Returns(@"C:\\Program Files\\ByteSync\\ByteSync.exe");

        var ladm = new LocalApplicationDataManager(mockEnv.Object);

        ladm.ShellApplicationDataPath.Should().Be(ladm.ApplicationDataPath);

        var sample = IOUtils.Combine(ladm.ApplicationDataPath, "Some", "File.txt");
        ladm.GetShellPath(sample).Should().Be(sample);
    }
}