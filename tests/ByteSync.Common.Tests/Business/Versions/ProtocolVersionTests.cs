using ByteSync.Common.Business.Versions;
using FluentAssertions;
using NUnit.Framework;

namespace TestingCommon.Business.Versions;

[TestFixture]
public class ProtocolVersionTests
{
    [Test]
    public void Current_ShouldBeV1()
    {
        ProtocolVersion.Current.Should().Be(ProtocolVersion.V1);
    }

    [Test]
    public void MinSupported_ShouldBeV1()
    {
        ProtocolVersion.MinSupported.Should().Be(ProtocolVersion.V1);
    }

    [Test]
    public void IsCompatible_WithCurrentVersion_ShouldReturnTrue()
    {
        var result = ProtocolVersion.IsCompatible(ProtocolVersion.Current);

        result.Should().BeTrue();
    }

    [Test]
    public void IsCompatible_WithV1_ShouldReturnTrue()
    {
        var result = ProtocolVersion.IsCompatible(ProtocolVersion.V1);

        result.Should().BeTrue();
    }

    [Test]
    public void IsCompatible_WithZero_ShouldReturnFalse()
    {
        var result = ProtocolVersion.IsCompatible(0);

        result.Should().BeFalse();
    }

    [Test]
    public void IsCompatible_WithDifferentVersion_ShouldReturnFalse()
    {
        var result = ProtocolVersion.IsCompatible(2);

        result.Should().BeFalse();
    }

    [Test]
    public void IsCompatible_WithNegativeVersion_ShouldReturnFalse()
    {
        var result = ProtocolVersion.IsCompatible(-1);

        result.Should().BeFalse();
    }
}

