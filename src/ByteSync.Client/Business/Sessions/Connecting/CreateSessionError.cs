namespace ByteSync.Business.Sessions.Connecting;

public class CreateSessionError
{
    public Exception? Exception { get; init; }
    public CreateSessionStatus Status { get; init; }
}