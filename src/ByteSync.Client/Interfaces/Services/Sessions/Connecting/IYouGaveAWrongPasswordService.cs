namespace ByteSync.Interfaces.Services.Sessions.Connecting;

public interface IYouGaveAWrongPasswordService
{
    Task Process(string sessionId);
}