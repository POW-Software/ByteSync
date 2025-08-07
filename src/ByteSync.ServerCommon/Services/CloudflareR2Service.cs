using Amazon.S3;
using Amazon.S3.Model;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Services;

public class CloudflareR2Service : ICloudflareR2Service
{
    private readonly CloudflareR2Settings _cloudflareR2Settings;
    private AmazonS3Client? _s3Client;

    public CloudflareR2Service(IOptions<CloudflareR2Settings> cloudflareR2Settings)
    {
        _cloudflareR2Settings = cloudflareR2Settings.Value;
    }

    public async Task<ListObjectsV2Response> ListObjectsAsync(ListObjectsV2Request request, CancellationToken cancellationToken)
    {
        var s3Client = GetS3Client();
        return await s3Client.ListObjectsV2Async(request, cancellationToken);
    }

    public async Task<DeleteObjectResponse> DeleteObjectAsync(DeleteObjectRequest request, CancellationToken cancellationToken)
    {
        var s3Client = GetS3Client();
        return await s3Client.DeleteObjectAsync(request, cancellationToken);
    }

    private AmazonS3Client GetS3Client()
    {
        if (_s3Client == null)
        {
            _s3Client = new AmazonS3Client(
                _cloudflareR2Settings.AccessKeyId,
                _cloudflareR2Settings.SecretAccessKey,
                new AmazonS3Config
                {
                    ServiceURL = _cloudflareR2Settings.Endpoint,
                    ForcePathStyle = true
                });
        }

        return _s3Client;
    }
} 