using System.Reflection;
using ByteSync.Business.Filtering.Parsing;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Business.Filtering;

public class IdentifiersTest
{
    [Test]
    public void All_ShouldContainAllPublicConstStrings()
    {
        // Arrange
        var expectedConstants = typeof(Identifiers)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
            .Select(field => (string)field.GetValue(null))
            .ToList();
        
        // Act
        var actualConstants = Identifiers.All();
        
        // Assert
        actualConstants.Should().BeEquivalentTo(expectedConstants, options => options.WithStrictOrdering());
    }
    
    [Test]
    public void All_ShouldContain_ACTION_SYNCHRONIZE_CONTENT()
    {
        // Arrange
        var expectedConstant = Identifiers.ACTION_COPY_CONTENT;
        
        // Act
        var actualConstants = Identifiers.All();
        
        // Assert
        actualConstants.Should().Contain(expectedConstant);
    }
}