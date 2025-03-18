using System.Net;

namespace ByteSync.Exceptions;

public class ApiException : Exception
{
    public ApiException(string message) : base(message)
    {
    }
    
    public ApiException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public ApiException(string message, HttpStatusCode httptatusCode) : base(message)
    {
        HttptatusCode = httptatusCode;
    }

    public HttpStatusCode? HttptatusCode { get; set; }
}