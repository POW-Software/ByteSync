using Autofac;
using ByteSync.Business.Comparisons;
using ByteSync.Business.Filtering;
using ByteSync.Business.Filtering.Expressions;
using ByteSync.Business.Filtering.Parsing;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.Inventories;
using ByteSync.Services.Filtering;
using FluentAssertions;
using Moq;

namespace ByteSync.Client.IntegrationTests.Business.Filtering;

public class TestFiltering : BaseTestFiltering
{
    [SetUp]
    public void Setup()
    {
        SetupBase();
    }

    [Test]
    public void TestParse_Complete_Expression()
    {
        // Arrange
        var filterText = "A1.content==B1.content";
        
        ConfigureDataPartIndexer();
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeTrue();
        parseResult.Expression.Should().NotBeNull();
        parseResult.Expression.Should().BeOfType<PropertyComparisonExpression>();
    }
    
    [Test]
    public void TestParse_Incomplete_Expression()
    {
        // Arrange
        var filterText = "A1.content==";
        
        ConfigureDataPartIndexer();

        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeFalse();
        parseResult.ErrorMessage.Should().NotBeNull();
        parseResult.ErrorMessage.Should().Contain("Expected value after operator");
    }

    [Test]
    public void TestParse_Incomplete_DotExpression()
    {
        // Arrange
        var filterText = "A1.";
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeFalse();
        parseResult.ErrorMessage.Should().NotBeNull();
        parseResult.ErrorMessage.Should().Contain("Expected property name after dot");
    }
    
    [Test]
    public void TestParse_Incomplete_Operator()
    {
        // Arrange
        var filterText = "A1.content";
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeFalse();
        parseResult.ErrorMessage.Should().NotBeNull();
        parseResult.ErrorMessage.Should().Contain("Expected operator after property name");
    }
    
    [Test]
    public void TestParse_Incomplete_OnExpression()
    {
        // Arrange
        var filterText = "on:";
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeFalse();
        parseResult.ErrorMessage.Should().NotBeNull();
        parseResult.ErrorMessage.Should().Contain("Expected data source identifier after 'on:'");
    }
    
    [Test]
    public void TestParse_Incomplete_NotExpression()
    {
        // Arrange
        var filterText = "NOT";
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeFalse();
        parseResult.ErrorMessage.Should().NotBeNull();
        parseResult.ErrorMessage.Should().Contain("Incomplete expression after NOT");
    }
    
    [Test]
    public void TestParse_Incomplete_ParenthesesExpression()
    {
        // Arrange
        var filterText = "(A1.content==B1.content";
        
        ConfigureDataPartIndexer();
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeFalse();
        parseResult.ErrorMessage.Should().NotBeNull();
        parseResult.ErrorMessage.Should().Contain("Expected closing parenthesis");
    }
    
    [Test]
    public void TestFilterService_HandlesIncompleteExpression_WithoutException()
    {
        // Arrange
        var filterService = new FilterService(_filterParser, _evaluatorFactory);
        var comparisonItem = CreateBasicComparisonItem();
        
        // Act & Assert - Should not throw
        var filter = filterService.BuildFilter("A1.content==");
        
        // By default, incomplete filters should accept everything
        filter(comparisonItem).Should().BeTrue();
    }
    
    private void ConfigureDataPartIndexer()
    {
        var mockDataPartIndexer = Container.Resolve<Mock<IDataPartIndexer>>();
        var inventoryA = new Inventory { InventoryId = "Id_A", Letter = "A" };
        var inventoryB = new Inventory { InventoryId = "Id_B", Letter = "B" };
    
        var inventoryPartA = new InventoryPart(inventoryA, "/testRootA", FileSystemTypes.Directory);
        var inventoryPartB = new InventoryPart(inventoryB, "/testRootB", FileSystemTypes.Directory);
    
        var dataPartA = new DataPart("A1", inventoryPartA);
        var dataPartB = new DataPart("B1", inventoryPartB);
    
        mockDataPartIndexer.Setup(m => m.GetDataPart("A1")).Returns(dataPartA);
        mockDataPartIndexer.Setup(m => m.GetDataPart("B1")).Returns(dataPartB);
    }
}