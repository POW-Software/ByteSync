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
        mockEnv.SetupGet(e => e.OSPlatform).Returns(OSPlatforms.Windows);
        mockEnv.SetupGet(e => e.AssemblyFullName)
            .Returns(@"C:\\Program Files\\WindowsApps\\POWSoftware.ByteSync_2025.7.2.0_neutral__f852479tj7xda\\ByteSync.exe");

        var result = mockEnv.Object.IsInstalledFromWindowsStore();
        result.Should().BeTrue();
    }

    [Test]
    public void IsInstalledFromWindowsStore_Should_Return_False_For_Non_WindowsApps_Path()
    {
        var mockEnv = new Mock<IEnvironmentService>();
        mockEnv.SetupGet(e => e.OSPlatform).Returns(OSPlatforms.Windows);
        mockEnv.SetupGet(e => e.AssemblyFullName)
            .Returns(@"C:\\Program Files\\ByteSync\\ByteSync.exe");

        mockEnv.Object.IsInstalledFromWindowsStore().Should().BeFalse();
    }
}