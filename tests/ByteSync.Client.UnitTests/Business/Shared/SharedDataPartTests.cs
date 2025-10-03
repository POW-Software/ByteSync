using System.Reflection;
using ByteSync.Business.Actions.Shared;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Controls.Json;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Business.Shared;

public class SharedDataPartTests
{
    [Test]
    public void Properties_Should_Have_Public_Setter_For_Serialization()
    {
        var type = typeof(SharedDataPart);
        
        string[] propertyNames =
        [
            nameof(SharedDataPart.NodeId),
            nameof(SharedDataPart.RelativePath),
            nameof(SharedDataPart.SignatureGuid),
            nameof(SharedDataPart.SignatureHash)
        ];
        
        foreach (var propertyName in propertyNames)
        {
            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            property.Should().NotBeNull("property {0} must exist", propertyName);
            
            var setter = property.SetMethod;
            setter.Should().NotBeNull("property {0} must have a setter for deserialization", propertyName);
            setter!.IsPublic.Should().BeTrue("property {0} setter must be public for deserialization", propertyName);
        }
    }
    
    [Test]
    public void Json_Deserialization_Should_Populate_Optional_Properties()
    {
        var json = "{\n" +
                   "  \"Name\": \"file.txt\",\n" +
                   "  \"InventoryPartType\": \"File\",\n" +
                   "  \"ClientInstanceId\": \"client-1\",\n" +
                   "  \"InventoryCodeAndId\": \"inv-123\",\n" +
                   "  \"NodeId\": \"node-xyz\",\n" +
                   "  \"RootPath\": \"C:/root\",\n" +
                   "  \"RelativePath\": \"sub/file.txt\",\n" +
                   "  \"SignatureGuid\": \"guid-abc\",\n" +
                   "  \"SignatureHash\": \"hash-123\",\n" +
                   "  \"HasAnalysisError\": false\n" +
                   "}";
        
        var result = JsonHelper.Deserialize<SharedDataPart>(json);
        
        result.NodeId.Should().Be("node-xyz");
        result.RelativePath.Should().Be("sub/file.txt");
        result.SignatureGuid.Should().Be("guid-abc");
        result.SignatureHash.Should().Be("hash-123");
        
        // Also ensure other required fields are bound, proving the model is deserializable
        result.Name.Should().Be("file.txt");
        result.InventoryPartType.Should().Be(FileSystemTypes.File);
        result.ClientInstanceId.Should().Be("client-1");
        result.InventoryCodeAndId.Should().Be("inv-123");
        result.RootPath.Should().Be("C:/root");
        result.HasAnalysisError.Should().BeFalse();
    }
}