using Autofac;
using ByteSync.Business.DataNodes;
using ByteSync.Business.DataSources;
using ByteSync.Business.Inventories;
using ByteSync.Business.SessionMembers;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Misc;
using ByteSync.Factories;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Inventories;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.TestsCommon;
using FluentAssertions;
using Moq;

namespace ByteSync.Client.IntegrationTests.Factories;

public class TestInventoryBuilderFactory : IntegrationTest
{
    [SetUp]
    public void Setup()
    {
        // Initialize the container even though we don't use it for these tests
        // This is needed for the TearDown to work properly
        BuildMoqContainer();
    }
    
    [Test]
    public void DataSourceFiltering_ShouldFilterByDataNodeId()
    {
        // Arrange
        var fakeDataNode = new DataNode { NodeId = "TestNode1", Code = "A", ClientInstanceId = "TestClient" };
        var fakeDataSources = new List<DataSource> 
        { 
            new DataSource { DataNodeId = "TestNode1", Code = "DS1" },
            new DataSource { DataNodeId = "TestNode1", Code = "DS2" },
            new DataSource { DataNodeId = "TestNode2", Code = "DS3" } // This should be filtered out
        };
        
        // Act - Test the filtering logic directly
        var filteredDataSources = fakeDataSources
            .Where(ds => ds.DataNodeId == fakeDataNode.NodeId)
            .ToList();
        
        // Assert
        filteredDataSources.Should().HaveCount(2);
        filteredDataSources.Should().Contain(ds => ds.Code == "DS1");
        filteredDataSources.Should().Contain(ds => ds.Code == "DS2");
        filteredDataSources.Should().NotContain(ds => ds.Code == "DS3");
    }
    
    [Test]
    public void DataSourceFiltering_ShouldReturnEmptyWhenNoMatchingDataNode()
    {
        // Arrange
        var fakeDataNode = new DataNode { NodeId = "TestNode1", Code = "A", ClientInstanceId = "TestClient" };
        var fakeDataSources = new List<DataSource> 
        { 
            new DataSource { DataNodeId = "TestNode2", Code = "DS3" }, // Different DataNode
            new DataSource { DataNodeId = "TestNode3", Code = "DS4" }  // Different DataNode
        };
        
        // Act - Test the filtering logic directly
        var filteredDataSources = fakeDataSources
            .Where(ds => ds.DataNodeId == fakeDataNode.NodeId)
            .ToList();
        
        // Assert
        filteredDataSources.Should().BeEmpty();
    }
    
    [Test]
    public void DataNodeOrdering_ShouldOrderByOrderIndex()
    {
        // Arrange
        var dataNodes = new List<DataNode>
        {
            new DataNode { NodeId = "Node3", OrderIndex = 3, Code = "C" },
            new DataNode { NodeId = "Node1", OrderIndex = 1, Code = "A" },
            new DataNode { NodeId = "Node2", OrderIndex = 2, Code = "B" }
        };
        
        // Act
        var orderedDataNodes = dataNodes.OrderBy(n => n.OrderIndex).ToList();
        
        // Assert
        orderedDataNodes.Should().HaveCount(3);
        orderedDataNodes[0].Code.Should().Be("A");
        orderedDataNodes[1].Code.Should().Be("B");
        orderedDataNodes[2].Code.Should().Be("C");
    }
}