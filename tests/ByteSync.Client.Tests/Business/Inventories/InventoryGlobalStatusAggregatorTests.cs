using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Sessions;
using FluentAssertions;
using NUnit.Framework;

namespace ByteSync.Tests.Business.Inventories;

[TestFixture]
public class InventoryGlobalStatusAggregatorTests
{
    [Test]
    public void All_Pending_returns_Pending()
    {
        var statuses = new[]
        {
            SessionMemberGeneralStatus.InventoryWaitingForStart,
            SessionMemberGeneralStatus.InventoryWaitingForAnalysis
        };
        
        var result = InventoryGlobalStatusAggregator.Aggregate(statuses);
        result.Should().Be(InventoryTaskStatus.Pending);
    }
    
    [Test]
    public void Any_Running_returns_Running_even_with_errors()
    {
        var statuses = new[]
        {
            SessionMemberGeneralStatus.InventoryRunningIdentification,
            SessionMemberGeneralStatus.InventoryError
        };
        
        var result = InventoryGlobalStatusAggregator.Aggregate(statuses);
        result.Should().Be(InventoryTaskStatus.Running);
    }
    
    [Test]
    public void Pending_with_Cancelled_or_Error_without_Running_returns_Pending()
    {
        var statuses = new[]
        {
            SessionMemberGeneralStatus.InventoryWaitingForStart,
            SessionMemberGeneralStatus.InventoryCancelled,
            SessionMemberGeneralStatus.InventoryError
        };
        
        var result = InventoryGlobalStatusAggregator.Aggregate(statuses);
        result.Should().Be(InventoryTaskStatus.Pending);
    }
    
    [Test]
    public void No_Running_or_Pending_but_Error_returns_Error()
    {
        var statuses = new[]
        {
            SessionMemberGeneralStatus.InventoryError,
            SessionMemberGeneralStatus.InventoryCancelled
        };
        
        var result = InventoryGlobalStatusAggregator.Aggregate(statuses);
        result.Should().Be(InventoryTaskStatus.Error);
    }
    
    [Test]
    public void No_Running_Pending_Error_but_Cancelled_returns_Cancelled()
    {
        var statuses = new[]
        {
            SessionMemberGeneralStatus.InventoryCancelled,
            SessionMemberGeneralStatus.InventoryFinished
        };
        
        var result = InventoryGlobalStatusAggregator.Aggregate(statuses);
        result.Should().Be(InventoryTaskStatus.Cancelled);
    }
    
    [Test]
    public void All_Success_returns_Success()
    {
        var statuses = new[]
        {
            SessionMemberGeneralStatus.InventoryFinished,
            SessionMemberGeneralStatus.SynchronizationRunning
        };
        
        var result = InventoryGlobalStatusAggregator.Aggregate(statuses);
        result.Should().Be(InventoryTaskStatus.Success);
    }
}