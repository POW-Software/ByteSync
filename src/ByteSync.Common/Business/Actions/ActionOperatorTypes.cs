namespace ByteSync.Common.Business.Actions;

public enum ActionOperatorTypes
{
    // File only
    
    // Keep for backward compatibility
    // Delete when minimal supported version >= 2026.1
    SynchronizeContentOnly = 1,
    SynchronizeDate = 2,
    SynchronizeContentAndDate = 3, 
    
    CopyContentOnly = 1,
    CopyDatesOnly = 2,
    Copy = 3,
    
    // Directory only
    Create = 5,
    
    // Common
    Delete = 10,
    DoNothing = 11
}