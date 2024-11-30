namespace ByteSync.Common.Business.Sessions;

public enum SessionMemberGeneralStatus
{
    InventoryWaitingForStart = 1,
    InventoryRunningIdentification = 2,
    InventoryWaitingForAnalysis = 3,
    InventoryRunningAnalysis = 4,
    InventoryCancelled = 5,
    InventoryError = 6,
    InventoryFinished = 7,
    SynchronizationRunning = 8,
    SynchronizationSourceActionsInitiated = 9,
    SynchronizationError = 10,
    SynchronizationFinished = 11,
    Unassigned12 = 12,
    Unassigned13 = 13,
    Unassigned14 = 14,
}