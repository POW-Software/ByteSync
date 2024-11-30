namespace ByteSync.Common.Business.Actions;

public enum ActionOperatorTypes
{
    // File only
    SynchronizeContentOnly = 1, 
    SynchronizeDate = 2, 
    SynchronizeContentAndDate = 3, 
        
    // Directory only
    Create = 5,
        
    // Common
    Delete = 10,
    DoNothing = 11
}