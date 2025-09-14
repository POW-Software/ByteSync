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
}