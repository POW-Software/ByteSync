using System;

namespace ByteSync.Common.Business.Communications.Transfers;

public class UploadFileResponse
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }
    public UploadFailureKind FailureKind { get; set; } = UploadFailureKind.None;

    public static UploadFileResponse Success(int statusCode)
    {
        return new UploadFileResponse
        {
            IsSuccess = true,
            StatusCode = statusCode,
            FailureKind = UploadFailureKind.None,
        };
    }

    public static UploadFileResponse Failure(int statusCode, string errorMessage)
    {
        return new UploadFileResponse
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage,
            FailureKind = UploadFailureKind.ServerError,
        };
    }

    public static UploadFileResponse Failure(int statusCode, Exception exception)
    {
        return new UploadFileResponse
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = exception.Message,
            Exception = exception,
            FailureKind = UploadFailureKind.ServerError,
        };
    }

    public static UploadFileResponse ClientCancellation(Exception exception)
    {
        return new UploadFileResponse
        {
            IsSuccess = false,
            StatusCode = 0,
            ErrorMessage = exception.Message,
            Exception = exception,
            FailureKind = UploadFailureKind.ClientCancellation,
        };
    }

    public static UploadFileResponse ClientTimeout(Exception exception)
    {
        return new UploadFileResponse
        {
            IsSuccess = false,
            StatusCode = 0,
            ErrorMessage = exception.Message,
            Exception = exception,
            FailureKind = UploadFailureKind.ClientTimeout,
        };
    }
}
