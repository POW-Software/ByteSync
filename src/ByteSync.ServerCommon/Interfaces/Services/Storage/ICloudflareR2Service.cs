namespace ByteSync.ServerCommon.Interfaces.Services.Storage;

using Amazon.S3;

public interface ICloudflareR2Service : IProviderService
{
    AmazonS3Client BuildS3Client();
}