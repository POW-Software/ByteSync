using Amazon.S3;

namespace ByteSync.ServerCommon.Interfaces.Services.Storage;

public interface ICloudflareR2BucketService
{
    AmazonS3Client BuildS3Client();
} 