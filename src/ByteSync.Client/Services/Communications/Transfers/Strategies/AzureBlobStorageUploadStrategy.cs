using System.Threading;
using Azure.Storage.Blobs;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;

namespace ByteSync.Services.Communications.Transfers.Strategies;

public class AzureBlobStorageUploadStrategy : IUploadStrategy
{
    private readonly ILogger<AzureBlobStorageUploadStrategy> _logger;

    public AzureBlobStorageUploadStrategy(ILogger<AzureBlobStorageUploadStrategy> logger)
    {
        _logger = logger;
    }

    public async Task<UploadFileResponse> UploadAsync(FileUploaderSlice slice, FileStorageLocation storageLocation, CancellationToken cancellationToken)
    {
        try
        {
            var options = new BlobClientOptions();
            options.Retry.NetworkTimeout = TimeSpan.FromMinutes(1);

            slice.MemoryStream.Position = 0;
            var blob = new BlobClient(new Uri(storageLocation.Url), options);
            var response = await blob.UploadAsync(slice.MemoryStream, cancellationToken);
                
            _logger.LogDebug("UploadAvailableSlice: slice {number} is uploaded", slice.PartNumber);

            var rawResponse = response.GetRawResponse();
            
            if (rawResponse.Status >= 200 && rawResponse.Status < 300)
            {
                return UploadFileResponse.Success(
                    statusCode: rawResponse.Status
                );
            }
            else
            {
                return UploadFileResponse.Failure(
                    statusCode: rawResponse.Status,
                    errorMessage: $"Upload failed with status code: {rawResponse.Status}"
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