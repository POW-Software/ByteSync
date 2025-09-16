using ByteSync.Services.Applications;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.Tests.Services.Applications;

public class MsixPfnParserTests
{
    private IMsixPfnParser _parser = null!;
    
    [SetUp]
    public void Setup()
    {
        _parser = new MsixPfnParser();
    }
    
    [Test]
    public void TryParse_ReturnsTrue_And_ComposesPfn_On_ValidContainerName()
    {
        const string container = "POWSoftware.ByteSync_2025.7.2.0_neutral__f852479tj7xda";
        
        var ok = _parser.TryParse(container, out var pfn);
        
        ok.Should().BeTrue();
        pfn.Should().Be("POWSoftware.ByteSync_f852479tj7xda");
    }
    
    [Test]
    public void TryParse_ReturnsTrue_For_X86ContainerName()
    {
        const string container = "Vendor.Product_2.0.0.0_neutral__abc123xyz";
        
        var ok = _parser.TryParse(container, out var pfn);
        
        ok.Should().BeTrue();
        pfn.Should().Be("Vendor.Product_abc123xyz");
    }
    
    [Test]
    public void TryParse_ReturnsFalse_When_DoubleUnderscore_Is_Missing()
    {
        const string container = "SomeApp_1.0.0.0_neutral_x64";
        
        var ok = _parser.TryParse(container, out var pfn);
        
        ok.Should().BeFalse();
        pfn.Should().BeNull();
    }
    
    [Test]
    public void TryParse_ReturnsFalse_When_FirstUnderscore_Is_Missing()
    {
        const string container = "NoUnderscoreHere__publisher";
        
        var ok = _parser.TryParse(container, out var pfn);
        
        ok.Should().BeFalse();
        pfn.Should().BeNull();
    }
    
    [Test]
    public void TryParse_ReturnsFalse_On_Empty_Or_Whitespace()
    {
        _parser.TryParse(string.Empty, out var pfn1).Should().BeFalse();
        pfn1.Should().BeNull();
        
        _parser.TryParse("   ", out var pfn2).Should().BeFalse();
        pfn2.Should().BeNull();
    }
}