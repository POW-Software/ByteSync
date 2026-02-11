using System.Text.Json;
using ByteSync.Common.Business.Misc;
using ByteSync.Services.Inventories;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Services.Inventories;

public class NoiseFileDetectorTests
{
    private static readonly string[] KnownNoiseFileNames = LoadNoiseFileNamesFromEmbeddedResource();
    
    [TestCaseSource(nameof(KnownNoiseFileNames))]
    public void IsNoiseFileName_ShouldReturnTrue_ForKnownNoiseFiles_OnWindows(string fileName)
    {
        var result = NoiseFileDetector.IsNoiseFileName(fileName, OSPlatforms.Windows);
        
        result.Should().BeTrue();
    }
    
    [TestCaseSource(nameof(KnownNoiseFileNames))]
    public void IsNoiseFileName_ShouldReturnTrue_ForKnownNoiseFiles_OnLinux(string fileName)
    {
        var result = NoiseFileDetector.IsNoiseFileName(fileName, OSPlatforms.Linux);
        
        result.Should().BeTrue();
    }
    
    [TestCase("DESKTOP.INI")]
    [TestCase("THUMBS.DB")]
    [TestCase("EHTHUMBS.DB")]
    [TestCase("EHTHUMBS_VISTA.DB")]
    [TestCase("$recycle.bin")]
    [TestCase(".ds_store")]
    [TestCase(".appledouble")]
    [TestCase(".appledb")]
    [TestCase(".appledesktop")]
    [TestCase(".lsoverride")]
    [TestCase(".spotlight-v100")]
    [TestCase(".trashes")]
    [TestCase(".FSEVENTSD")]
    [TestCase(".temporaryitems")]
    [TestCase(".volumeicon.icns")]
    [TestCase(".DIRECTORY")]
    public void IsNoiseFileName_ShouldBeCaseInsensitive_OnNonLinuxPlatforms(string fileName)
    {
        var windowsResult = NoiseFileDetector.IsNoiseFileName(fileName, OSPlatforms.Windows);
        var macResult = NoiseFileDetector.IsNoiseFileName(fileName, OSPlatforms.MacOs);
        
        windowsResult.Should().BeTrue();
        macResult.Should().BeTrue();
    }
    
    [TestCase("DESKTOP.INI")]
    [TestCase("THUMBS.DB")]
    [TestCase("EHTHUMBS.DB")]
    [TestCase("EHTHUMBS_VISTA.DB")]
    [TestCase("$recycle.bin")]
    [TestCase(".ds_store")]
    [TestCase(".appledouble")]
    [TestCase(".appledb")]
    [TestCase(".appledesktop")]
    [TestCase(".lsoverride")]
    [TestCase(".spotlight-v100")]
    [TestCase(".trashes")]
    [TestCase(".FSEVENTSD")]
    [TestCase(".temporaryitems")]
    [TestCase(".volumeicon.icns")]
    [TestCase(".DIRECTORY")]
    public void IsNoiseFileName_ShouldBeCaseSensitive_OnLinux(string fileName)
    {
        var result = NoiseFileDetector.IsNoiseFileName(fileName, OSPlatforms.Linux);
        
        result.Should().BeFalse();
    }
    
    [TestCase("readme.md")]
    [TestCase("normal.txt")]
    [TestCase(".gitignore")]
    public void IsNoiseFileName_ShouldReturnFalse_ForUnknownFileNames(string fileName)
    {
        var windowsResult = NoiseFileDetector.IsNoiseFileName(fileName, OSPlatforms.Windows);
        var linuxResult = NoiseFileDetector.IsNoiseFileName(fileName, OSPlatforms.Linux);
        
        windowsResult.Should().BeFalse();
        linuxResult.Should().BeFalse();
    }
    
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void IsNoiseFileName_ShouldReturnFalse_ForEmptyValues(string? fileName)
    {
        var windowsResult = NoiseFileDetector.IsNoiseFileName(fileName, OSPlatforms.Windows);
        var linuxResult = NoiseFileDetector.IsNoiseFileName(fileName, OSPlatforms.Linux);
        
        windowsResult.Should().BeFalse();
        linuxResult.Should().BeFalse();
    }
    
    private static string[] LoadNoiseFileNamesFromEmbeddedResource()
    {
        var assembly = typeof(NoiseFileDetector).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .SingleOrDefault(rn => rn.EndsWith(".Services.Inventories.noise-files.json", StringComparison.Ordinal));
        
        resourceName.Should().NotBeNull();
        
        using var stream = assembly.GetManifestResourceStream(resourceName!);
        stream.Should().NotBeNull();
        
        var data = JsonSerializer.Deserialize<string[]>(stream!);
        data.Should().NotBeNull();
        
        return data!;
    }
}
