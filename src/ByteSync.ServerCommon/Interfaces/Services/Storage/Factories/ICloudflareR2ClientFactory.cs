namespace ByteSync.ServerCommon.Interfaces.Services.Storage.Factories;

using Amazon.S3;

public interface ICloudflareR2ClientFactory
{
    AmazonS3Client GetClient();
}