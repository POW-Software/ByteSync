using ByteSync.Business.Inventories;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Client.UnitTests.Business.Inventories;

[TestFixture]
public class InventoryMonitorDataTests
{
    [Test]
    public void HasNonZeroProperty_WithAllZeros_ShouldReturnFalse()
    {
        // Arrange
        var data = new InventoryMonitorData();
        
        // Act
        var result = data.HasNonZeroProperty();
        
        // Assert
        result.Should().BeFalse();
    }
    
    [Test]
    public void HasNonZeroProperty_WithIdentifiedFiles_ShouldReturnTrue()
    {
        // Arrange
        var data = new InventoryMonitorData { IdentifiedFiles = 10 };
        
        // Act
        var result = data.HasNonZeroProperty();
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void HasNonZeroProperty_WithIdentifiedDirectories_ShouldReturnTrue()
    {
        // Arrange
        var data = new InventoryMonitorData { IdentifiedDirectories = 5 };
        
        // Act
        var result = data.HasNonZeroProperty();
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void HasNonZeroProperty_WithAnalyzedFiles_ShouldReturnTrue()
    {
        // Arrange
        var data = new InventoryMonitorData { AnalyzedFiles = 8 };
        
        // Act
        var result = data.HasNonZeroProperty();
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void HasNonZeroProperty_WithProcessedVolume_ShouldReturnTrue()
    {
        // Arrange
        var data = new InventoryMonitorData { ProcessedVolume = 1024 };
        
        // Act
        var result = data.HasNonZeroProperty();
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void HasNonZeroProperty_WithAnalyzeErrors_ShouldReturnTrue()
    {
        // Arrange
        var data = new InventoryMonitorData { AnalyzeErrors = 2 };
        
        // Act
        var result = data.HasNonZeroProperty();
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void HasNonZeroProperty_WithAnalyzableFiles_ShouldReturnTrue()
    {
        // Arrange
        var data = new InventoryMonitorData { AnalyzableFiles = 15 };
        
        // Act
        var result = data.HasNonZeroProperty();
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void HasNonZeroProperty_WithAnalyzableVolume_ShouldReturnTrue()
    {
        // Arrange
        var data = new InventoryMonitorData { AnalyzableVolume = 2048 };
        
        // Act
        var result = data.HasNonZeroProperty();
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void HasNonZeroProperty_WithIdentifiedVolume_ShouldReturnTrue()
    {
        // Arrange
        var data = new InventoryMonitorData { IdentifiedVolume = 4096 };
        
        // Act
        var result = data.HasNonZeroProperty();
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void HasNonZeroProperty_WithUploadTotalVolume_ShouldReturnTrue()
    {
        // Arrange
        var data = new InventoryMonitorData { UploadTotalVolume = 8192 };
        
        // Act
        var result = data.HasNonZeroProperty();
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void HasNonZeroProperty_WithUploadedVolume_ShouldReturnTrue()
    {
        // Arrange
        var data = new InventoryMonitorData { UploadedVolume = 1024 };
        
        // Act
        var result = data.HasNonZeroProperty();
        
        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void HasNonZeroProperty_WithSkippedEntriesCount_ShouldReturnTrue()
    {
        // Arrange
        var data = new InventoryMonitorData { SkippedEntriesCount = 2 };

        // Act
        var result = data.HasNonZeroProperty();

        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void HasNonZeroProperty_WithMultipleNonZeroProperties_ShouldReturnTrue()
    {
        // Arrange
        var data = new InventoryMonitorData
        {
            IdentifiedFiles = 10,
            AnalyzedFiles = 5,
            UploadTotalVolume = 2048,
            UploadedVolume = 1024
        };
        
        // Act
        var result = data.HasNonZeroProperty();
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void Record_WithClause_ShouldCreateNewInstanceWithModifiedProperty()
    {
        // Arrange
        var original = new InventoryMonitorData
        {
            IdentifiedFiles = 10,
            UploadTotalVolume = 5000
        };
        
        // Act
        var modified = original with { UploadedVolume = 2500 };
        
        // Assert
        modified.IdentifiedFiles.Should().Be(10);
        modified.UploadTotalVolume.Should().Be(5000);
        modified.UploadedVolume.Should().Be(2500);
        original.UploadedVolume.Should().Be(0);
    }
    
    [Test]
    public void UploadTotalVolume_ShouldBeSettableAndGettable()
    {
        // Arrange
        var data = new InventoryMonitorData();
        
        // Act
        data.UploadTotalVolume = 10240;
        
        // Assert
        data.UploadTotalVolume.Should().Be(10240);
    }
}
