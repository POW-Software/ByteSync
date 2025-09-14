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
    [Test]
    public void ApplicationDataPath_Should_Be_MSIX_LocalCache_When_Installed_From_Store()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Ignore("Windows-only behavior");
        }

        var mockEnv = new Mock<IEnvironmentService>();
        mockEnv.SetupGet(e => e.IsPortableApplication).Returns(false);
        mockEnv.SetupGet(e => e.ExecutionMode).Returns(ExecutionMode.Regular);
        mockEnv.SetupProperty(e => e.Arguments, []);
        mockEnv.SetupGet(e => e.AssemblyFullName)
            .Returns(@"C:\\Program Files\\WindowsApps\\POWSoftware.ByteSync_2025.7.2.0_neutral__f852479tj7xda\\ByteSync.exe");
        mockEnv.SetupGet(e => e.DeploymentMode).Returns(DeploymentMode.MsixInstallation);
        mockEnv.SetupGet(e => e.MsixPackageFamilyName).Returns("POWSoftware.ByteSync_f852479tj7xda");

        var ladm = new LocalApplicationDataManager(mockEnv.Object);

        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var expectedRoot = IOUtils.Combine(local, "Packages", "POWSoftware.ByteSync_f852479tj7xda", "LocalCache", "Local");
        var expectedAppData = IOUtils.Combine(expectedRoot, "POW Software", "ByteSync");

        ladm.ApplicationDataPath.Should().Be(expectedAppData);
    }

    [Test]
    public void ApplicationDataPath_Should_Be_Logical_When_Not_Store()
    {
        var mockEnv = new Mock<IEnvironmentService>();
        mockEnv.SetupGet(e => e.IsPortableApplication).Returns(false);
        mockEnv.SetupGet(e => e.ExecutionMode).Returns(ExecutionMode.Regular);
        mockEnv.SetupProperty(e => e.Arguments, []);
        mockEnv.SetupGet(e => e.AssemblyFullName)
            .Returns(@"C:\\Program Files\\ByteSync\\ByteSync.exe");
        mockEnv.SetupGet(e => e.DeploymentMode).Returns(DeploymentMode.SetupInstallation);

        var ladm = new LocalApplicationDataManager(mockEnv.Object);

        // In setup mode, the logical path is used directly under %LOCALAPPDATA% (or %APPDATA% if roaming install)
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        ladm.ApplicationDataPath.Should().StartWith(IOUtils.Combine(local, "POW Software", "ByteSync"));
    }
}