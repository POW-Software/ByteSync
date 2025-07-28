using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;

namespace ByteSync.Services.Communications.Transfers.Strategies;

public class BlobStorageDownloadStrategy : IDownloadStrategy
{
    public async Task<Response> DownloadAsync(Stream memoryStream, FileStorageLocation storageLocation, CancellationToken cancellationToken)
    {
        var options = new BlobClientOptions();
        options.Retry.NetworkTimeout = TimeSpan.FromMinutes(20);
        var blob = new BlobClient(new Uri(storageLocation.Url), options);
        var response = await blob.DownloadToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;
        return response;
    }
} 