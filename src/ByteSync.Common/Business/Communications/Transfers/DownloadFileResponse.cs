namespace ByteSync.Common.Business.Communications.Transfers;

public class DownloadFileResponse
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public string? ErrorMessage { get; set; }

    public static DownloadFileResponse Success(int statusCode, object? rawResponse = null)
    {
        return new DownloadFileResponse
        {
            IsSuccess = true,
            StatusCode = statusCode,
        };
    }

    public static DownloadFileResponse Failure(int statusCode, string errorMessage, object? rawResponse = null)
    {
        return new DownloadFileResponse
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage,
        };
    }
}