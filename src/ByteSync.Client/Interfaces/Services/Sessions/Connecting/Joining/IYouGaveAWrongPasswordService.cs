namespace ByteSync.Interfaces.Services.Sessions.Connecting.Joining;

public interface IYouGaveAWrongPasswordService
{
    Task Process(string sessionId);
}