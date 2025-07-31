using System.Threading;
using Azure.Storage.Blobs;
using ByteSync.Business.Communications.Transfers;
using ByteSync.Common.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;

namespace ByteSync.Services.Communications.Transfers.Strategies;

public class BlobStorageUploadStrategy : IUploadStrategy
{
    public async Task<UploadLocationResponse> UploadAsync(ILogger<FileUploadWorker> logger, FileUploaderSlice slice, FileStorageLocation storageLocation, CancellationToken cancellationToken)
    {
        try
        {
            var options = new BlobClientOptions();
            options.Retry.NetworkTimeout = TimeSpan.FromMinutes(60);

            slice.MemoryStream.Position = 0;
            var blob = new BlobClient(new Uri(storageLocation.Url), options);
            var response = await blob.UploadAsync(slice.MemoryStream, cancellationToken);
                
            logger.LogDebug("UploadAvailableSlice: slice {number} is uploaded", slice.PartNumber);
            
            return UploadLocationResponse.Success(
                statusCode: response.GetRawResponse().Status,
                response
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload slice {number}", slice.PartNumber);
            return UploadLocationResponse.Failure(500, ex.Message);
        }
    }
}