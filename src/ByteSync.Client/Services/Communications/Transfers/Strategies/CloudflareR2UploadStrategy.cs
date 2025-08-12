using System.Net.Http;
using System.Threading;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;

namespace ByteSync.Services.Communications.Transfers.Strategies;

public class CloudflareR2UploadStrategy : IUploadStrategy
{
    private readonly ILogger<CloudflareR2UploadStrategy> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public CloudflareR2UploadStrategy(ILogger<CloudflareR2UploadStrategy> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<UploadFileResponse> UploadAsync(FileUploaderSlice slice, FileStorageLocation storageLocation, CancellationToken cancellationToken)
    {
        try
        {
            
            slice.MemoryStream.Position = 0;
            
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromMinutes(10);
            
            using var content = new StreamContent(slice.MemoryStream);
            using var response = await httpClient.PutAsync(storageLocation.Url, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return UploadFileResponse.Success(
                    statusCode: (int)response.StatusCode
                );
            }
            else
            {
                return UploadFileResponse.Failure(
                    statusCode: (int)response.StatusCode,
                    errorMessage: $"Upload failed with status code: {response.StatusCode}"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload slice {number}", slice.PartNumber);
            return UploadFileResponse.Failure(500, ex);
        }
    }
}