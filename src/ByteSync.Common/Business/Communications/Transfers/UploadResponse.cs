namespace ByteSync.Common.Business.Communications.Transfers;

public class UploadLocationResponse
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public string? ErrorMessage { get; set; }

    // Optional: store the original response object if needed later
    public object? RawResponse { get; set; }

    public static UploadLocationResponse Success(int statusCode, object? rawResponse = null)
    {
        return new UploadLocationResponse
        {
            IsSuccess = true,
            StatusCode = statusCode,
            RawResponse = rawResponse
        };
    }

    public static UploadLocationResponse Failure(int statusCode, string errorMessage, object? rawResponse = null)
    {
        return new UploadLocationResponse
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage,
            RawResponse = rawResponse
        };
    }

    // Helper method to cast RawResponse to a known type
    public T? GetRawResponse<T>() where T : class
    {
        return RawResponse as T;
    }
}
