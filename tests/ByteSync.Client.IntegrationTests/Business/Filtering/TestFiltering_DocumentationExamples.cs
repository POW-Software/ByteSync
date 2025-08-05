using Autofac;
using ByteSync.Business.Actions.Local;
using ByteSync.Business.Comparisons;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Actions;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.Inventories;
using FluentAssertions;
using Moq;

namespace ByteSync.Client.IntegrationTests.Business.Filtering;

public class TestFiltering_DocumentationExamples : BaseTestFiltering
{
    private IAtomicActionRepository _mockActionRepository = null!;
    
    [SetUp]
    public void Setup()
    {
        SetupBase();
        _mockActionRepository = Container.Resolve<IAtomicActionRepository>();
    }
    
    [Test]
    public void TestParse_ComplexFileFilter_WithNameSizeAndExistence()
    {
        // Arrange - "is:file AND name==\"report*\" AND A1.size>1MB AND NOT on:B1"
        var filterText = "is:file AND name==\"report*\" AND A1.size>1MB AND NOT on:B1";
        
        ConfigureDataPartIndexer();
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeTrue();
        parseResult.Expression.Should().NotBeNull();
    }
    
    [Test]
    public void TestEvaluate_ComplexFileFilter_WithNameSizeAndExistence()
    {
        // Arrange - "is:file AND name==\"report*\" AND A1.size>1MB AND NOT on:B1" 
        var filterText = "is:file AND name==\"report*\" AND A1.size>1MB AND NOT on:B1";
        
        // Create a file that matches: is file, name starts with "report", size > 1MB, only on A
        var comparisonItem = PrepareComparisonWithOneContent("A1", "sameHash", DateTime.Now, 2 * 1024 * 1024, "reportA.txt");
        
        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void TestParse_LogFilesWithActions()
    {
        // Arrange - "name == \"*.log\" AND actions:copy"
        var filterText = "name == \"*.log\" AND actions:copy";
        
        ConfigureDataPartIndexer();
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeTrue();
        parseResult.Expression.Should().NotBeNull();
    }
    
    [Test]
    public void TestEvaluate_LogFilesWithActions()
    {
        // Arrange - "name == \"*.log\" AND actions:copy"
        var filterText = "name == \"*.log\" AND actions:copy";
        
        var comparisonItem = PrepareComparisonWithOneContent("A1", "sameHash", DateTime.Now, 1024, "app.log");
        
        // Add copy action
        var copyAction = CreateAtomicAction(comparisonItem, ActionOperatorTypes.SynchronizeContentOnly, false);
        _mockActionRepository.AddOrUpdate(new List<AtomicAction> { copyAction });
        
        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Test]
    public void TestParse_OnlyOnSourceAndWillBeCopied()
    {
        // Arrange - "only:A AND actions:copy"
        var filterText = "only:A AND actions:copy";
        
        ConfigureDataPartIndexer();
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeTrue();
        parseResult.Expression.Should().NotBeNull();
    }
    
    [Test]
    public void TestParse_ExcludeBackupPaths()
    {
        // Arrange - "NOT path:\"*backup*\""
        var filterText = "NOT path:\"*backup*\"";
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeTrue();
        parseResult.Expression.Should().NotBeNull();
    }
    
    [Test]
    public void TestEvaluate_ExcludeBackupPaths()
    {
        // Arrange - "NOT path:\"*backup*\""
        var filterText = "NOT path:\"*backup*\"";
        
        // Test with non-backup path (should match)
        var pathIdentity1 = new PathIdentity(FileSystemTypes.File, "/documents/file.txt", "file.txt", "/documents/file.txt");
        var comparisonItem1 = new ComparisonItem(pathIdentity1);
        
        // Test with backup path (should not match)
        var pathIdentity2 = new PathIdentity(FileSystemTypes.File, "/backup/file.txt", "file.txt", "/backup/file.txt");
        var comparisonItem2 = new ComparisonItem(pathIdentity2);
        
        // Act
        var result1 = EvaluateFilterExpression(filterText, comparisonItem1);
        var result2 = EvaluateFilterExpression(filterText, comparisonItem2);
        
        // Assert
        result1.Should().BeTrue(); // Non-backup path should match
        result2.Should().BeFalse(); // Backup path should be excluded
    }
    
    [Test]
    public void TestParse_FileAndDirectoryWithSize()
    {
        // Arrange - "(is:file OR is:directory) AND A1.size > 1MB"
        var filterText = "(is:file OR is:directory) AND A1.size > 1MB";
        
        ConfigureDataPartIndexer();
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeTrue();
        parseResult.Expression.Should().NotBeNull();
    }
    
    [Test]
    public void TestParse_DuplicatedFiles()
    {
        // Arrange - "A1.contents==A2.contents AND A1.size==A2.size AND is:file"
        var filterText = "A1.contents==A2.contents AND A1.size==A2.size AND is:file";
        
        ConfigureDataPartIndexer("A", "A"); // Need A1 and A2 from same inventory A
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeTrue();
        parseResult.Expression.Should().NotBeNull();
    }
    
    [Test]
    public void TestParse_OldTempFiles()
    {
        // Arrange - "(name=~\"\\\\.(tmp|bak|log)$\") AND A1.last-write-time < now-6M"
        var filterText = "(name=~\"\\\\.(tmp|bak|log)$\") AND A1.last-write-time < now-6M";
        
        ConfigureDataPartIndexer();
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeTrue();
        parseResult.Expression.Should().NotBeNull();
    }
    
    [Test]
    public void TestParse_ExecutablesInDocuments()
    {
        // Arrange - "path:\"*/Documents/*\" AND name=~\"\\\\.(exe|dll|bat)$\""
        var filterText = "path:\"*/Documents/*\" AND name=~\"\\\\.(exe|dll|bat)$\"";
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeTrue();
        parseResult.Expression.Should().NotBeNull();
    }
    
    [Test]
    public void TestParse_RecentProjectFiles()
    {
        // Arrange - "path:\"*MyProject*\" AND is:file AND A1.last-write-time >= now-7d"
        var filterText = "path:\"*MyProject*\" AND is:file AND A1.last-write-time >= now-7d";
        
        ConfigureDataPartIndexer();
        
        // Act
        var parseResult = _filterParser.TryParse(filterText);
        
        // Assert
        parseResult.IsComplete.Should().BeTrue();
        parseResult.Expression.Should().NotBeNull();
    }
    
    private void ConfigureDataPartIndexer(string inventoryACode = "A", string inventoryBCode = "B")
    {
        inventoryACode = inventoryACode.ToUpperInvariant();
        inventoryBCode = inventoryBCode.ToUpperInvariant();
        
        var mockDataPartIndexer = Container.Resolve<Mock<IDataPartIndexer>>();
        var inventoryA = new Inventory
        {
            InventoryId = $"Id_{inventoryACode}", 
            Code = inventoryACode
        };
        var inventoryB = new Inventory
        {
            InventoryId = $"Id_{inventoryBCode}", 
            Code = inventoryBCode
        };
    
        var inventoryPartA = new InventoryPart(inventoryA, $"/testRoot{inventoryACode}", 
            FileSystemTypes.Directory);
        var inventoryPartB = new InventoryPart(inventoryB, $"/testRoot{inventoryBCode}", 
            FileSystemTypes.Directory);
    
        var dataPartAName = $"{inventoryACode}1";
        var dataPartBName = $"{inventoryBCode}1";
        var dataPartA2Name = $"{inventoryACode}2"; // For A2 if needed
        
        var dataPartA = new DataPart(dataPartAName, inventoryPartA);
        var dataPartB = new DataPart(dataPartBName, inventoryPartB);
        var dataPartA2 = new DataPart(dataPartA2Name, inventoryPartA);
    
        mockDataPartIndexer.Setup(m => m.GetDataPart(dataPartAName)).Returns(dataPartA);
        mockDataPartIndexer.Setup(m => m.GetDataPart(dataPartBName)).Returns(dataPartB);
        mockDataPartIndexer.Setup(m => m.GetDataPart(dataPartA2Name)).Returns(dataPartA2);
    }
    
    private AtomicAction CreateAtomicAction(ComparisonItem comparisonItem, ActionOperatorTypes operatorType, bool isTargeted)
    {
        var action = new AtomicAction($"AAID_{Guid.NewGuid()}", comparisonItem);
        action.Operator = operatorType;

        if (!isTargeted)
        {
            action.SynchronizationRule = new SynchronizationRule(comparisonItem.FileSystemType, ConditionModes.All);
        }

        return action;
    }
} 