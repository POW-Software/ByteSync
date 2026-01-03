namespace ByteSync.Common.Business.Actions;

public enum ActionOperatorTypes
{
    // File only
    CopyContentOnly = 1,
    CopyDatesOnly = 2,
    Copy = 3,
    
    // Directory only
    Create = 5,
    
    // Common
    Delete = 10,
    DoNothing = 11
}