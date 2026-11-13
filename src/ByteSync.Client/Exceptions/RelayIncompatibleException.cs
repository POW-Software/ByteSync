namespace ByteSync.Exceptions;

public sealed class RelayIncompatibleException : BuildConnectionException
{
    public RelayIncompatibleException(Exception innerException)
        : base("The relay returned HTTP 404 for auth/challenge, which is required for this client's secure authentication. " +
               "Check the relay address or update the relay server. Automatic reconnection has stopped.", innerException)
    {
    }
}
