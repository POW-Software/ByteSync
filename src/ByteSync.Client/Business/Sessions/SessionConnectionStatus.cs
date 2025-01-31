namespace ByteSync.Business.Sessions;

public enum SessionConnectionStatus
{ 
    None = 0,
    JoiningSession = 1, 
    CreatingSession = 2,
    InSession = 3,
}