using ByteSync.Common.Business.Versions;
using FluentAssertions;
using NUnit.Framework;

namespace TestingCommon.Business.Versions;

[TestFixture]
public class ProtocolVersionTests
{
    [Test]
    public void Current_ShouldBeV2()
    {
        ProtocolVersion.CURRENT.Should().Be(ProtocolVersion.V2);
    }
    
    [Test]
    public void MinSupported_ShouldBeV2()
    {
        ProtocolVersion.MIN_SUPPORTED.Should().Be(ProtocolVersion.V2);
    }
    
    [Test]
    public void IsCompatible_WithCurrentVersion_ShouldReturnTrue()
    {
        var result = ProtocolVersion.IsCompatible(ProtocolVersion.CURRENT);
        
        result.Should().BeTrue();
    }
    
    [Test]
    public void IsCompatible_WithV1_ShouldReturnFalse()
    {
        var result = ProtocolVersion.IsCompatible(ProtocolVersion.V1);
        
        result.Should().BeFalse();
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
        var result = ProtocolVersion.IsCompatible(3);
        
        result.Should().BeFalse();
    }
    
    [Test]
    public void IsCompatible_WithNegativeVersion_ShouldReturnFalse()
    {
        var result = ProtocolVersion.IsCompatible(-1);
        
        result.Should().BeFalse();
    }
}
