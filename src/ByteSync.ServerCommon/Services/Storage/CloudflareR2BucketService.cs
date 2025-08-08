using Amazon.S3;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Services.Storage;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Services.Storage;

public class CloudflareR2BucketService : ICloudflareR2BucketService
{
    private readonly CloudflareR2Settings _cloudflareR2Settings;
    private AmazonS3Client? _s3Client;

    public CloudflareR2BucketService(IOptions<CloudflareR2Settings> cloudflareR2Settings)
    {
        _cloudflareR2Settings = cloudflareR2Settings.Value;
    }

    public AmazonS3Client BuildS3Client()
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