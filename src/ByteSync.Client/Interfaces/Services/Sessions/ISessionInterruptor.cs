namespace ByteSync.Interfaces.Services.Sessions;

public interface ISessionInterruptor
{
    Task RequestQuitSession();
    
    Task RequestRestartSession();
}