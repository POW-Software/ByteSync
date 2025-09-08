using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
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
            httpClient.DefaultRequestHeaders.ExpectContinue = false;
            
            // Create a per-attempt copy so disposing the content won't close the original slice stream
            using var attemptStream = new MemoryStream(slice.MemoryStream.ToArray(), writable: false);
            using var content = new StreamContent(attemptStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Headers.ContentLength = attemptStream.Length;

            using var request = new HttpRequestMessage(HttpMethod.Put, storageLocation.Url)
            {
                Content = content
            };
            request.Headers.ExpectContinue = false;

            _logger.LogDebug("R2 PUT start: part {Part} sizeKB {SizeKb} host {Host}",
                slice.PartNumber, Math.Round(attemptStream.Length / 1024d), new Uri(storageLocation.Url).Host);

            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return UploadFileResponse.Success(
                    statusCode: (int)response.StatusCode
                );
            }
            else
            {
                string? body = null;
                try { body = await response.Content.ReadAsStringAsync(cancellationToken); } catch { }
                _logger.LogError("R2 PUT failed: status {Status} body: {BodySnippet}", (int)response.StatusCode, body != null ? body.Substring(0, Math.Min(body.Length, 256)) : "<empty>");
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
