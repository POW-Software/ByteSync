namespace ByteSync.Common.Business.Communications.Transfers;

public class UploadFileResponse
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public string? ErrorMessage { get; set; }

    public static UploadFileResponse Success(int statusCode)
    {
        return new UploadFileResponse
        {
            IsSuccess = true,
            StatusCode = statusCode,
        };
    }

    public static UploadFileResponse Failure(int statusCode, string errorMessage)
    {
        return new UploadFileResponse
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage,
        };
    }
}
