using System.IO;
using System.Threading;
using Azure.Storage.Blobs;
using ByteSync.Common.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;

namespace ByteSync.Services.Communications.Transfers.Strategies;

public class AzureBlobStorageDownloadStrategy : IDownloadStrategy
{
    public async Task<DownloadFileResponse> DownloadAsync(Stream memoryStream, FileStorageLocation storageLocation, CancellationToken cancellationToken)
    {
        try
        {
            var options = new BlobClientOptions();
            options.Retry.NetworkTimeout = TimeSpan.FromMinutes(20);
            var blob = new BlobClient(new Uri(storageLocation.Url), options);
            var response = await blob.DownloadToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            if (response.Status >= 200 && response.Status < 300)
            {
                return DownloadFileResponse.Success(
                    statusCode: response.Status
                );
            }
            else
            {
                return DownloadFileResponse.Failure(
                    statusCode: response.Status,
                    errorMessage: $"Download failed with status code: {response.Status}"
                );
            }
        }
        catch (Exception ex)
        {
            // Encapsulate the full exception information
            var errorMessage = $"Download failed: {ex.GetType().Name}: {ex.Message}";
            if (ex.InnerException != null)
            {
                errorMessage += $" Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}";
            }
            
            return DownloadFileResponse.Failure(500, errorMessage);
        }
    }
} 