using ByteSync.Common.Business.Sessions;

namespace ByteSync.Business.Inventories;

public static class InventoryGlobalStatusAggregator
{
    public static InventoryTaskStatus Aggregate(IEnumerable<SessionMemberGeneralStatus> memberStatuses)
    {
        var list = memberStatuses as IList<SessionMemberGeneralStatus> ?? memberStatuses.ToList();
        
        if (list.Count == 0)
        {
            return InventoryTaskStatus.Pending;
        }
        
        // Map to task statuses for aggregation
        var taskStatuses = list.Select(MapMemberStatusToTaskStatus).ToList();
        
        // Pragmatic ordering with special rule: Pending dominates when mixed with Cancelled/Error and no Running
        if (taskStatuses.Any(s => s == InventoryTaskStatus.Running))
        {
            return InventoryTaskStatus.Running;
        }
        
        if (taskStatuses.Any(s => s == InventoryTaskStatus.Pending))
        {
            return InventoryTaskStatus.Pending;
        }
        
        if (taskStatuses.Any(s => s == InventoryTaskStatus.Error || s == InventoryTaskStatus.NotLaunched))
        {
            return InventoryTaskStatus.Error;
        }
        
        if (taskStatuses.Any(s => s == InventoryTaskStatus.Cancelled))
        {
            return InventoryTaskStatus.Cancelled;
        }
        
        return InventoryTaskStatus.Success;
    }
    
    private static InventoryTaskStatus MapMemberStatusToTaskStatus(SessionMemberGeneralStatus status)
    {
        switch (status)
        {
            case SessionMemberGeneralStatus.InventoryWaitingForStart:
            case SessionMemberGeneralStatus.InventoryWaitingForAnalysis:
                return InventoryTaskStatus.Pending;
            
            case SessionMemberGeneralStatus.InventoryRunningIdentification:
            case SessionMemberGeneralStatus.InventoryRunningAnalysis:
                return InventoryTaskStatus.Running;
            
            case SessionMemberGeneralStatus.InventoryCancelled:
                return InventoryTaskStatus.Cancelled;
            
            case SessionMemberGeneralStatus.InventoryError:
                return InventoryTaskStatus.Error;
            
            case SessionMemberGeneralStatus.InventoryFinished:
                return InventoryTaskStatus.Success;
            
            // All synchronization-related statuses are beyond inventory scope; treat as Success here
            case SessionMemberGeneralStatus.SynchronizationRunning:
            case SessionMemberGeneralStatus.SynchronizationSourceActionsInitiated:
            case SessionMemberGeneralStatus.SynchronizationError:
            case SessionMemberGeneralStatus.SynchronizationFinished:
            case SessionMemberGeneralStatus.Unassigned12:
            case SessionMemberGeneralStatus.Unassigned13:
            case SessionMemberGeneralStatus.Unassigned14:
                return InventoryTaskStatus.Success;
            
            default:
                return InventoryTaskStatus.Success;
        }
    }
}