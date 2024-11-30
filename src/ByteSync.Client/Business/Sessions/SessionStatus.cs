namespace ByteSync.Business.Sessions;

public enum SessionStatus
{
    None = 0,
    CloudSessionCreation = 1,
    CloudSessionJunction = 2,
    Preparation = 3,
    Inventory = 4,
    Comparison = 5,
    Synchronization = 6,
    FatalError = 7,
    RegularEnd = 8,
}