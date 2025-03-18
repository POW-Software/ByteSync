using ByteSync.Services.Communications.Api;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Tests.Controls.Communications.Api;

public class ApiResponseContentHelperTests
{
        [Test]
    public void IsEmptyContent_WithNullContent_ShouldReturnTrue()
    {
        // Act
        var result = ApiResponseContentHelper.IsEmptyContent(null);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void IsEmptyContent_WithEmptyString_ShouldReturnTrue()
    {
        // Act
        var result = ApiResponseContentHelper.IsEmptyContent("");
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void IsEmptyContent_WithWhitespaceOnly_ShouldReturnTrue()
    {
        // Act
        var result = ApiResponseContentHelper.IsEmptyContent("  \t\n");
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void IsEmptyContent_WithNullString_ShouldReturnTrue()
    {
        // Act
        var result = ApiResponseContentHelper.IsEmptyContent("null");
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void IsEmptyContent_WithEmptyJsonObject_ShouldReturnTrue()
    {
        // Act
        var result = ApiResponseContentHelper.IsEmptyContent("{}");
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void IsEmptyContent_WithIdOnlyJsonObject_ShouldReturnTrue()
    {
        // Act
        var result = ApiResponseContentHelper.IsEmptyContent("""{"$id":"1"}""");
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void IsEmptyContent_WithIdOnlyJsonObjectWithSpaces_ShouldReturnTrue()
    {
        // Act
        var result = ApiResponseContentHelper.IsEmptyContent("""{ "$id" : "1" }""");
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void IsEmptyContent_WithIdOnlyJsonObjectWithNewlines_ShouldReturnTrue()
    {
        // Act
        var result = ApiResponseContentHelper.IsEmptyContent("""
        {
            "$id": "1"
        }
        """);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void IsEmptyContent_WithNonEmptyJsonObject_ShouldReturnFalse()
    {
        // Act
        var result = ApiResponseContentHelper.IsEmptyContent("""{"$id":"1","name":"test"}""");
        
        // Assert
        result.Should().BeFalse();
    }
    
    [Test]
    public void IsEmptyContent_WithNonJsonContent_ShouldReturnFalse()
    {
        // Act
        var result = ApiResponseContentHelper.IsEmptyContent("This is not JSON");
        
        // Assert
        result.Should().BeFalse();
    }
}