using ByteSync.Common.Business.Misc;
using ByteSync.Common.Helpers;
using FluentAssertions;
using NUnit.Framework;

namespace TestingCommon.Helpers;

[TestFixture]
public class ProtectedPathsTests
{
    [TestCase("/dev", "/dev")]
    [TestCase("/dev/sda", "/dev")]
    [TestCase("/proc", "/proc")]
    [TestCase("/proc/1", "/proc")]
    [TestCase("/sys", "/sys")]
    [TestCase("/sys/kernel", "/sys")]
    [TestCase("/run", "/run")]
    [TestCase("/run/user/1000", "/run")]
    [TestCase("/var/run", "/var/run")]
    [TestCase("/var/run/daemon", "/var/run")]
    public void TryGetProtectedRoot_Linux_ReturnsTrue(string path, string expectedRoot)
    {
        var result = ProtectedPaths.TryGetProtectedRoot(path, OSPlatforms.Linux, out var root);
        
        result.Should().BeTrue();
        root.Should().Be(expectedRoot);
    }
    
    [TestCase("/private/var/run", "/private/var/run")]
    [TestCase("/private/var/run/daemon", "/private/var/run")]
    public void TryGetProtectedRoot_MacOs_ReturnsTrue(string path, string expectedRoot)
    {
        var result = ProtectedPaths.TryGetProtectedRoot(path, OSPlatforms.MacOs, out var root);
        
        result.Should().BeTrue();
        root.Should().Be(expectedRoot);
    }
    
    [Test]
    public void TryGetProtectedRoot_Linux_WithNonProtectedPath_ReturnsFalse()
    {
        var result = ProtectedPaths.TryGetProtectedRoot("/home/user", OSPlatforms.Linux, out var root);
        
        result.Should().BeFalse();
        root.Should().BeEmpty();
    }
    
    [Test]
    public void TryGetProtectedRoot_Windows_ReturnsFalse()
    {
        var result = ProtectedPaths.TryGetProtectedRoot("C:\\Windows", OSPlatforms.Windows, out var root);
        
        result.Should().BeFalse();
        root.Should().BeEmpty();
    }
}