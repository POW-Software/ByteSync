using System.IO;
using System.Threading;
using Amazon.S3;
using Amazon.S3.Model;
using ByteSync.Common.Business.Communications.Transfers;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.Interfaces.Controls.Communications;

namespace ByteSync.Services.Communications.Transfers.Strategies;

public class CloudflareR2DownloadStrategy : IDownloadStrategy
{
    public async Task<DownloadFileResponse> DownloadAsync(Stream memoryStream, FileStorageLocation storageLocation, CancellationToken cancellationToken)
    {
        try
        {
            // Parse the Cloudflare R2 URL to extract bucket and key
            var uri = new Uri(storageLocation.Url);
            var pathSegments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            if (pathSegments.Length < 2)
            {
                return DownloadFileResponse.Failure(400, "Invalid Cloudflare R2 URL format");
            }
            
            var bucketName = pathSegments[0];
            var key = string.Join("/", pathSegments.Skip(1));
            
            // Extract account ID from the hostname
            var hostname = uri.Host;
            var accountId = hostname.Split('.')[0]; // e.g., "account-id.r2.cloudflarestorage.com"
            
            // For Cloudflare R2, we need to construct the S3 client with the proper endpoint
            var s3Client = new AmazonS3Client(new AmazonS3Config
            {
                ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
                ForcePathStyle = true // Required for Cloudflare R2
            });

            var request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = key
            };
            
            using var response = await s3Client.GetObjectAsync(request, cancellationToken);
            
            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;
                
                return DownloadFileResponse.Success(
                    statusCode: (int)response.HttpStatusCode
                );
            }
            else
            {
                return DownloadFileResponse.Failure(
                    statusCode: (int)response.HttpStatusCode,
                    errorMessage: $"Download failed with status code: {response.HttpStatusCode}"
                );
            }
        }
        catch (Exception ex)
        {
            return DownloadFileResponse.Failure(500, ex);
        }
    }
} 