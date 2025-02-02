namespace ByteSync.Business.Sessions.Connecting;

public class CreateSessionError
{
    public Exception? Exception { get; set; }
    public CreateSessionStatus Status { get; set; }
}