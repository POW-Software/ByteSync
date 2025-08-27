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

    public ApiException(string message, HttpStatusCode httpStatusCode) : base(message)
    {
        HttpStatusCode = httpStatusCode;
    }

    public HttpStatusCode? HttpStatusCode { get; set; }
}