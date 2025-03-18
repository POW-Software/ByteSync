using ByteSync.Common.Business.Auth;

namespace ByteSync.Exceptions;

public class BuildConnectionException : Exception
{
    public BuildConnectionException(string message) : base(message)
    {
    }
    
    public BuildConnectionException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public BuildConnectionException(string message, InitialConnectionStatus? resultAuthenticateResponseStatus) : base(message)
    {
        InitialConnectionStatus = resultAuthenticateResponseStatus;
    }

    public InitialConnectionStatus? InitialConnectionStatus { get; set; }
}