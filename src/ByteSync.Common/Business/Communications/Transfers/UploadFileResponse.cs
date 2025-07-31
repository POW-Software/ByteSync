namespace ByteSync.Common.Business.Communications.Transfers;

public class UploadFileResponse
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public string? ErrorMessage { get; set; }

    public static UploadFileResponse Success(int statusCode, object? rawResponse = null)
    {
        return new UploadFileResponse
        {
            IsSuccess = true,
            StatusCode = statusCode,
        };
    }

    public static UploadFileResponse Failure(int statusCode, string errorMessage, object? rawResponse = null)
    {
        return new UploadFileResponse
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage,
        };
    }
}
