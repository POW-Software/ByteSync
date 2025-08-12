namespace ByteSync.ServerCommon.Services.Storage.Factories;

using Amazon.S3;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Services.Storage.Factories;
using Microsoft.Extensions.Options;

public class CloudflareR2ClientFactory : ICloudflareR2ClientFactory
{
    private readonly CloudflareR2Settings _settings;
    private AmazonS3Client? _client;

    public CloudflareR2ClientFactory(IOptions<CloudflareR2Settings> settings)
    {
        _settings = settings.Value;
    }

    public AmazonS3Client GetClient()
    {
        if (_client == null)
        {
            _client = new AmazonS3Client(
                _settings.AccessKeyId,
                _settings.SecretAccessKey,
                new AmazonS3Config
                {
                    ServiceURL = _settings.Endpoint,
                    ForcePathStyle = true
                });
        }

        return _client;
    }
}