using System.IO;
using System.Net.Http;
using System.Threading;
using ByteSync.Common.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;

namespace ByteSync.Services.Communications.Transfers.Strategies;

public class CloudflareR2DownloadStrategy : IDownloadStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CloudflareR2DownloadStrategy> _logger;

    public CloudflareR2DownloadStrategy(IHttpClientFactory httpClientFactory, ILogger<CloudflareR2DownloadStrategy> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<DownloadFileResponse> DownloadAsync(Stream memoryStream, FileStorageLocation storageLocation, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromMinutes(10);
            
            using var response = await httpClient.GetAsync(storageLocation.Url, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                await response.Content.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;
                
                return DownloadFileResponse.Success(
                    statusCode: (int)response.StatusCode
                );
            }
            else
            {
                return DownloadFileResponse.Failure(
                    statusCode: (int)response.StatusCode,
                    errorMessage: $"Download failed with status code: {response.StatusCode}"
                );
            }
        }
        catch (Exception ex)
        {
            return DownloadFileResponse.Failure(500, ex);
        }
    }
} 