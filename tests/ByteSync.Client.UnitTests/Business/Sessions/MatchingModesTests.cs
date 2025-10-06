using System.Text.Json;
using ByteSync.Business.Sessions;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Business.Sessions;

[TestFixture]
public class MatchingModesTests
{
    [Test]
    public void MatchingModesJsonConverter_Serializes_ToNewNames()
    {
        JsonSerializer.Serialize(MatchingModes.Flat).Should().Be("\"Flat\"");
        JsonSerializer.Serialize(MatchingModes.Tree).Should().Be("\"Tree\"");
    }
    
    [Test]
    public void MatchingModesJsonConverter_Deserializes_FromNewAndLegacyStrings()
    {
        JsonSerializer.Deserialize<MatchingModes>("\"Flat\"").Should().Be(MatchingModes.Flat);
        JsonSerializer.Deserialize<MatchingModes>("\"Tree\"").Should().Be(MatchingModes.Tree);
        
        JsonSerializer.Deserialize<MatchingModes>("\"Name\"").Should().Be(MatchingModes.Flat);
        JsonSerializer.Deserialize<MatchingModes>("\"RelativePath\"").Should().Be(MatchingModes.Tree);
    }
    
    [Test]
    public void MatchingModesJsonConverter_Deserializes_FromNumbers()
    {
        JsonSerializer.Deserialize<MatchingModes>("1").Should().Be(MatchingModes.Flat);
        JsonSerializer.Deserialize<MatchingModes>("2").Should().Be(MatchingModes.Tree);
    }
    
    [Test]
    public void LegacyLinkingKeyJsonConverter_Writes_Legacy_And_Reads_Legacy()
    {
        var settings = new SessionSettings { MatchingMode = MatchingModes.Flat };
        var json = JsonSerializer.Serialize(settings);
        
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.TryGetProperty("MatchingMode", out var newProp).Should().BeTrue();
        root.TryGetProperty("LinkingKey", out var legacyProp).Should().BeTrue();
        newProp.GetString().Should().Be("Flat");
        legacyProp.GetString().Should().Be("Name");
        
        var oldJson = "{\"LinkingKey\":\"RelativePath\"}";
        var data = JsonSerializer.Deserialize<SessionSettings>(oldJson)!;
        data.MatchingMode.Should().Be(MatchingModes.Tree);
    }
}