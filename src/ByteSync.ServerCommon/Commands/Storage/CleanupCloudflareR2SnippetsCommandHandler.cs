using Amazon.S3;
using Amazon.S3.Model;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Services.Storage;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Commands.Storage;

public class CleanupCloudflareR2SnippetsCommandHandler : IRequestHandler<CleanupCloudflareR2SnippetsRequest, int>
{
    private readonly ICloudflareR2Service _cloudflareR2Service;
    private readonly ILogger<CleanupCloudflareR2SnippetsCommandHandler> _logger;
    private readonly CloudflareR2Settings _cloudflareR2Settings;

    public CleanupCloudflareR2SnippetsCommandHandler(
        ICloudflareR2Service cloudflareR2Service,
        IOptions<CloudflareR2Settings> cloudflareR2Settings,
        ILogger<CleanupCloudflareR2SnippetsCommandHandler> logger)
    {
        _cloudflareR2Service = cloudflareR2Service;
        _cloudflareR2Settings = cloudflareR2Settings.Value;
        _logger = logger;
    }

    public async Task<int> Handle(CleanupCloudflareR2SnippetsRequest request, CancellationToken cancellationToken)
    {
        if (_cloudflareR2Settings.RetentionDurationInDays < 1)
        {
            _logger.LogWarning("RetentionDurationInDays is less than 1, no element deleted");
            return 0;
        }

        var deletedObjectsCount = 0;
        var cutoffDate = DateTime.UtcNow.AddDays(-_cloudflareR2Settings.RetentionDurationInDays);

        try
        {
            var s3Client = _cloudflareR2Service.BuildS3Client();

            var listObjectsRequest = new ListObjectsV2Request
            {
                BucketName = _cloudflareR2Settings.BucketName
            };

            var listObjectsResponse = await s3Client.ListObjectsV2Async(listObjectsRequest, cancellationToken);

            foreach (var s3Object in listObjectsResponse.S3Objects)
            {
                if (s3Object.LastModified <= cutoffDate)
                {
                    _logger.LogInformation("Deleting obsolete R2 object {ObjectKey} (LastModified:{LastModified})",
                        s3Object.Key, s3Object.LastModified);

                    var deleteRequest = new DeleteObjectRequest
                    {
                        BucketName = _cloudflareR2Settings.BucketName,
                        Key = s3Object.Key
                    };

                    await s3Client.DeleteObjectAsync(deleteRequest, cancellationToken);
                    deletedObjectsCount += 1;
                }
            }
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Error listing or deleting objects from R2 bucket {BucketName}", _cloudflareR2Settings.BucketName);
            return 0;
        }

        return deletedObjectsCount;
    }
} 