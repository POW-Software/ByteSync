namespace ByteSync.Business.Sessions;

public enum SessionConnectionStatus
{ 
    NoSession = 0,
    JoiningSession = 1, 
    CreatingSession = 2,
    InSession = 3,
}