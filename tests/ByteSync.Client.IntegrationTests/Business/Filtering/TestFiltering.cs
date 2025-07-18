﻿using Autofac;
using ByteSync.Business.Comparisons;
using ByteSync.Business.Filtering.Expressions;
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
        var filterText = "A1.contents==B1.contents";
        
        ConfigureDataPartIndexer();
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeTrue();
        parseResult.Expression.Should().NotBeNull();
        parseResult.Expression.Should().BeOfType<PropertyComparisonExpression>();
    }
    
    [Test]
    public void TestParse_CompleteLowerCase_Expression()
    {
        // Arrange
        var filterText = "a1.contents==b1.contents";
        
        ConfigureDataPartIndexer();
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeTrue();
        parseResult.Expression.Should().NotBeNull();
        parseResult.Expression.Should().BeOfType<PropertyComparisonExpression>();
    }
    
    [Test]
    public void TestParse_CompleteWithSpaces1_Expression()
    {
        // Arrange
        var filterText = "A1.contents == B1.contents";
        
        ConfigureDataPartIndexer();
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeTrue();
        parseResult.Expression.Should().NotBeNull();
        parseResult.Expression.Should().BeOfType<PropertyComparisonExpression>();
    }
    
    [Test]
    public void TestParse_CompleteWithSpaces2_Expression()
    {
        // Arrange
        var filterText = "A1.last-write-time >= 2024-01-01";
        
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
        var filterText = "A1.contents==";
        
        ConfigureDataPartIndexer();

        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeFalse();
        parseResult.ErrorMessage.Should().NotBeNull();
        parseResult.ErrorMessage.Should().Contain("Expected value after operator");
    }
    
    [Test]
    public void TestParse_UnknownIdentifier_Expression()
    {
        // Arrange
        var filterText = "A1.unknown==";
        
        ConfigureDataPartIndexer();

        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeFalse();
        parseResult.ErrorMessage.Should().NotBeNull();
        parseResult.ErrorMessage.Should().Contain("Expected property name after dot");
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
        var filterText = "A1.contents";
        
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
        var filterText = "(A1.contents==B1.contents";
        
        ConfigureDataPartIndexer();
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeFalse();
        parseResult.ErrorMessage.Should().NotBeNull();
        parseResult.ErrorMessage.Should().Contain("Expected closing parenthesis");
    }
    
    [Test]
    public void TestParse_Incomplete_WithAndOperator()
    {
        // Arrange
        var filterText = "A1.contents==B1.contents AND on:";
        
        ConfigureDataPartIndexer();
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeFalse();
        parseResult.ErrorMessage.Should().NotBeNull();
        parseResult.ErrorMessage.Should().Contain("Incomplete right operand for AND expression: Expected data source identifier after 'on:'");
    }
    
    [Test]
    public void TestParse_Complete_FullExpression()
    {
        // Arrange
        var filterText = "is:file AND name==\"report\" AND A1.size>1MB AND NOT on:B1";
        
        ConfigureDataPartIndexer();
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeTrue();
    }
    
    [Test]
    public void TestFilterService_HandlesIncompleteExpression_WithoutException()
    {
        // Arrange
        var filterService = new FilterService(_filterParser, _evaluatorFactory, _logger);
        var comparisonItem = CreateBasicComparisonItem();
        
        // Act & Assert - Should not throw
        var filter = filterService.BuildFilter("A1.content==");
        
        // By default, incomplete filters should accept everything
        filter(comparisonItem).Should().BeTrue();
    }
    
    [Test]
    public void TestFilterService_BuildFilter_ListWithIncompleteExpressions()
    {
        // Arrange
        var filterService = new FilterService(_filterParser, _evaluatorFactory, _logger);
        var comparisonItem = CreateBasicComparisonItem();

        // A complete expression and an incomplete one
        var filterTexts = new List<string>
        {
            "A1.content==B1.content", // complete
            "A1.content=="           // incomplete
        };

        ConfigureDataPartIndexer();

        // Act
        var filter = filterService.BuildFilter(filterTexts);

        // Assert
        // The filter should apply the complete expression and ignore the incomplete one
        // Here, we assume that CreateBasicComparisonItem() does not check content equality,
        // so the result depends on the item's configuration.
        // For the example, we simply verify that the filter does not throw an exception.
        filter.Should().NotBeNull();
        filter(comparisonItem); // Should not throw an exception

        // If no complete expression, the filter should accept everything
        var onlyIncomplete = new List<string> { "A1.content==" };
        var filter2 = filterService.BuildFilter(onlyIncomplete);
        filter2(comparisonItem).Should().BeTrue();
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
