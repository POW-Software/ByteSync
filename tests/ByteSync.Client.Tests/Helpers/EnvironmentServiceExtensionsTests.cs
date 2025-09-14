using ByteSync.Common.Business.Misc;
using ByteSync.Helpers;
using ByteSync.Interfaces.Controls.Applications;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ByteSync.Tests.Helpers;

[TestFixture]
public class EnvironmentServiceExtensionsTests
{
    [Test]
    public void IsInstalledFromWindowsStore_Should_Return_True_For_WindowsApps_Path()
    {
        var mockEnv = new Mock<IEnvironmentService>();
        mockEnv.SetupGet(e => e.DeploymentMode).Returns(DeploymentMode.MsixInstallation);

        var result = mockEnv.Object.IsInstalledFromWindowsStore();
        result.Should().BeTrue();
    }

    [Test]
    public void IsInstalledFromWindowsStore_Should_Return_False_For_Non_WindowsApps_Path()
    {
        var mockEnv = new Mock<IEnvironmentService>();
        mockEnv.SetupGet(e => e.DeploymentMode).Returns(DeploymentMode.SetupInstallation);

        mockEnv.Object.IsInstalledFromWindowsStore().Should().BeFalse();
    }
}