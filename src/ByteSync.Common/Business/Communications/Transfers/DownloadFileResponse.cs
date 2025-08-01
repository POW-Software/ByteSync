using System;

namespace ByteSync.Common.Business.Communications.Transfers;

public class DownloadFileResponse
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }

    public static DownloadFileResponse Success(int statusCode)
    {
        return new DownloadFileResponse
        {
            IsSuccess = true,
            StatusCode = statusCode,
        };
    }

    public static DownloadFileResponse Failure(int statusCode, string errorMessage)
    {
        return new DownloadFileResponse
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage,
        };
    }

    public static DownloadFileResponse Failure(int statusCode, Exception exception)
    {
        return new DownloadFileResponse
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = exception.Message,
            Exception = exception,
        };
    }
}